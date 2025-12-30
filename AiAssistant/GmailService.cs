using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace AiAssistant
{
    /// <summary>
    /// Gmail APIを使用したメールサービス実装
    /// OAuth2認証フローを使用してユーザーのGmailにアクセスします
    /// </summary>
    public sealed class GmailService : IGmailService, IDisposable
    {
        private static readonly string[] Scopes =
        {
            Google.Apis.Gmail.v1.GmailService.Scope.GmailReadonly,
            Google.Apis.Gmail.v1.GmailService.Scope.GmailModify
        };
        private const string ApplicationName = "AiAssistant";
        private const string UserId = "me";

        private Google.Apis.Gmail.v1.GmailService? _gmailService;
        private UserCredential? _credential;
        private bool _disposed;

        public bool IsAuthenticated => _credential != null && _gmailService != null;

        /// <summary>
        /// OAuth2認証を行います
        /// </summary>
        public async Task<bool> AuthenticateAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // client_secret.jsonまたはappsettings.jsonから認証情報を取得
                var clientSecrets = GoogleCredentialHelper.LoadClientSecrets();
                if (clientSecrets == null)
                {
                    Console.WriteLine("[Gmail] 認証情報が見つかりません。client_secret.jsonを配置してください。");
                    return false;
                }

                var credPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "tokens",
                    "gmail"
                );

                _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    clientSecrets,
                    Scopes,
                    "user",
                    cancellationToken,
                    new FileDataStore(credPath, true)
                );

                _gmailService = new Google.Apis.Gmail.v1.GmailService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = _credential,
                    ApplicationName = ApplicationName
                });

                Console.WriteLine("[Gmail] 認証成功");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Gmail] 認証エラー: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 未読メールの一覧を取得します
        /// </summary>
        public async Task<IReadOnlyList<EmailInfo>> GetUnreadEmailsAsync(
            int maxResults = 10,
            CancellationToken cancellationToken = default)
        {
            return await GetEmailsAsync("is:unread", maxResults, cancellationToken);
        }

        /// <summary>
        /// 最新のメール一覧を取得します
        /// </summary>
        public async Task<IReadOnlyList<EmailInfo>> GetRecentEmailsAsync(
            int maxResults = 10,
            CancellationToken cancellationToken = default)
        {
            return await GetEmailsAsync("in:inbox", maxResults, cancellationToken);
        }

        private async Task<IReadOnlyList<EmailInfo>> GetEmailsAsync(
            string query,
            int maxResults,
            CancellationToken cancellationToken)
        {
            if (_gmailService == null)
            {
                var authenticated = await AuthenticateAsync(cancellationToken);
                if (!authenticated || _gmailService == null)
                {
                    return Array.Empty<EmailInfo>();
                }
            }

            try
            {
                var request = _gmailService.Users.Messages.List(UserId);
                request.Q = query;
                request.MaxResults = maxResults;

                var response = await request.ExecuteAsync(cancellationToken);
                var emails = new List<EmailInfo>();

                if (response.Messages != null)
                {
                    foreach (var msg in response.Messages)
                    {
                        var email = await GetEmailInfoAsync(msg.Id, cancellationToken);
                        if (email != null)
                        {
                            emails.Add(email);
                        }
                    }
                }

                Console.WriteLine($"[Gmail] {emails.Count}件のメールを取得しました");
                return emails;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Gmail] メール取得エラー: {ex.Message}");
                return Array.Empty<EmailInfo>();
            }
        }

        /// <summary>
        /// 指定したメールの詳細を取得します
        /// </summary>
        public async Task<EmailInfo?> GetEmailDetailsAsync(
            string messageId,
            CancellationToken cancellationToken = default)
        {
            if (_gmailService == null)
            {
                var authenticated = await AuthenticateAsync(cancellationToken);
                if (!authenticated || _gmailService == null)
                {
                    return null;
                }
            }

            return await GetEmailInfoAsync(messageId, cancellationToken, includeBody: true);
        }

        private async Task<EmailInfo?> GetEmailInfoAsync(
            string messageId,
            CancellationToken cancellationToken,
            bool includeBody = false)
        {
            try
            {
                var request = _gmailService!.Users.Messages.Get(UserId, messageId);
                request.Format = includeBody
                    ? UsersResource.MessagesResource.GetRequest.FormatEnum.Full
                    : UsersResource.MessagesResource.GetRequest.FormatEnum.Metadata;
                request.MetadataHeaders = new[] { "From", "Subject", "Date" };

                var message = await request.ExecuteAsync(cancellationToken);

                var email = new EmailInfo
                {
                    Id = message.Id,
                    ThreadId = message.ThreadId,
                    Snippet = message.Snippet ?? "",
                    IsUnread = message.LabelIds?.Contains("UNREAD") ?? false,
                    IsStarred = message.LabelIds?.Contains("STARRED") ?? false,
                    Labels = message.LabelIds?.ToList() ?? new List<string>()
                };

                // ヘッダーを解析
                if (message.Payload?.Headers != null)
                {
                    foreach (var header in message.Payload.Headers)
                    {
                        switch (header.Name.ToLowerInvariant())
                        {
                            case "subject":
                                email.Subject = DecodeEncodedWord(header.Value ?? "(件名なし)");
                                break;
                            case "from":
                                var fromValue = DecodeEncodedWord(header.Value ?? "");
                                email.From = ExtractDisplayName(fromValue);
                                email.FromEmail = ExtractEmailAddress(fromValue);
                                break;
                            case "date":
                                if (DateTime.TryParse(header.Value, out var date))
                                {
                                    email.ReceivedAt = date.ToLocalTime();
                                }
                                break;
                        }
                    }
                }

                // 本文を取得
                if (includeBody && message.Payload != null)
                {
                    email.Body = ExtractBody(message.Payload);
                }

                // 受信日時が取れなかった場合、内部タイムスタンプを使用
                if (email.ReceivedAt == default && message.InternalDate.HasValue)
                {
                    email.ReceivedAt = DateTimeOffset.FromUnixTimeMilliseconds(message.InternalDate.Value).LocalDateTime;
                }

                return email;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Gmail] メール詳細取得エラー: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// メールを既読にします
        /// </summary>
        public async Task<bool> MarkAsReadAsync(string messageId, CancellationToken cancellationToken = default)
        {
            return await ModifyLabelsAsync(messageId, null, new[] { "UNREAD" }, cancellationToken);
        }

        /// <summary>
        /// メールを未読にします
        /// </summary>
        public async Task<bool> MarkAsUnreadAsync(string messageId, CancellationToken cancellationToken = default)
        {
            return await ModifyLabelsAsync(messageId, new[] { "UNREAD" }, null, cancellationToken);
        }

        private async Task<bool> ModifyLabelsAsync(
            string messageId,
            string[]? addLabels,
            string[]? removeLabels,
            CancellationToken cancellationToken)
        {
            if (_gmailService == null)
            {
                var authenticated = await AuthenticateAsync(cancellationToken);
                if (!authenticated || _gmailService == null)
                {
                    return false;
                }
            }

            try
            {
                var mods = new ModifyMessageRequest
                {
                    AddLabelIds = addLabels?.ToList(),
                    RemoveLabelIds = removeLabels?.ToList()
                };

                await _gmailService.Users.Messages.Modify(mods, UserId, messageId)
                    .ExecuteAsync(cancellationToken);

                Console.WriteLine($"[Gmail] ラベル変更成功: {messageId}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Gmail] ラベル変更エラー: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// メール一覧をサマリー文字列に変換します
        /// </summary>
        public string FormatEmailsSummary(IReadOnlyList<EmailInfo> emails, string label = "受信トレイ")
        {
            if (emails.Count == 0)
            {
                return $"📧 {label}\n\nメールはありません。";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"📧 {label} ({emails.Count}件)");
            sb.AppendLine();

            // 日付でグループ化
            var grouped = emails
                .GroupBy(e => e.ReceivedAt.Date)
                .OrderByDescending(g => g.Key);

            foreach (var group in grouped)
            {
                var dateStr = group.Key == DateTime.Today
                    ? "今日"
                    : group.Key == DateTime.Today.AddDays(-1)
                        ? "昨日"
                        : group.Key.ToString("M月d日 (ddd)");

                sb.AppendLine($"【{dateStr}】");

                foreach (var email in group.OrderByDescending(e => e.ReceivedAt))
                {
                    var unreadMark = email.IsUnread ? "●" : " ";
                    var starMark = email.IsStarred ? "⭐" : "";
                    var timeStr = email.ReceivedAt.ToString("HH:mm");
                    var fromDisplay = TruncateText(email.From, 15);
                    var subjectDisplay = TruncateText(email.Subject, 25);

                    sb.AppendLine($"  {unreadMark} {timeStr} {fromDisplay}");
                    sb.AppendLine($"     {starMark}{subjectDisplay}");
                }
                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }

        private static string ExtractBody(MessagePart payload)
        {
            // テキスト本文を探す
            if (payload.MimeType == "text/plain" && payload.Body?.Data != null)
            {
                return DecodeBase64Url(payload.Body.Data);
            }

            // マルチパートの場合、再帰的に探す
            if (payload.Parts != null)
            {
                foreach (var part in payload.Parts)
                {
                    if (part.MimeType == "text/plain" && part.Body?.Data != null)
                    {
                        return DecodeBase64Url(part.Body.Data);
                    }
                }

                // text/htmlしかない場合
                foreach (var part in payload.Parts)
                {
                    if (part.MimeType == "text/html" && part.Body?.Data != null)
                    {
                        var html = DecodeBase64Url(part.Body.Data);
                        return StripHtml(html);
                    }
                }

                // ネストされたパーツを探す
                foreach (var part in payload.Parts)
                {
                    if (part.Parts != null)
                    {
                        var body = ExtractBody(part);
                        if (!string.IsNullOrEmpty(body))
                        {
                            return body;
                        }
                    }
                }
            }

            return "";
        }

        private static string DecodeBase64Url(string base64Url)
        {
            var base64 = base64Url
                .Replace('-', '+')
                .Replace('_', '/');

            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }

            var bytes = Convert.FromBase64String(base64);
            return Encoding.UTF8.GetString(bytes);
        }

        private static string DecodeEncodedWord(string text)
        {
            // RFC 2047 エンコーディングをデコード
            var pattern = @"=\?([^?]+)\?([BQ])\?([^?]+)\?=";
            return Regex.Replace(text, pattern, match =>
            {
                var charset = match.Groups[1].Value;
                var encoding = match.Groups[2].Value.ToUpperInvariant();
                var encoded = match.Groups[3].Value;

                try
                {
                    byte[] bytes;
                    if (encoding == "B")
                    {
                        bytes = Convert.FromBase64String(encoded);
                    }
                    else // Q encoding
                    {
                        encoded = encoded.Replace("_", " ");
                        bytes = Regex.Replace(encoded, @"=([0-9A-Fa-f]{2})", m =>
                            ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString())
                            .Select(c => (byte)c).ToArray();
                    }

                    var enc = Encoding.GetEncoding(charset);
                    return enc.GetString(bytes);
                }
                catch
                {
                    return match.Value;
                }
            });
        }

        private static string ExtractDisplayName(string fromHeader)
        {
            // "Display Name <email@example.com>" から Display Name を抽出
            var match = Regex.Match(fromHeader, @"^""?([^""<]+)""?\s*<");
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }

            // メールアドレスのみの場合
            var emailMatch = Regex.Match(fromHeader, @"<([^>]+)>");
            if (emailMatch.Success)
            {
                return emailMatch.Groups[1].Value;
            }

            return fromHeader;
        }

        private static string ExtractEmailAddress(string fromHeader)
        {
            var match = Regex.Match(fromHeader, @"<([^>]+)>");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            // メールアドレスのみの場合
            if (fromHeader.Contains("@"))
            {
                return fromHeader.Trim();
            }

            return "";
        }

        private static string StripHtml(string html)
        {
            // 簡易的なHTMLタグ除去
            var text = Regex.Replace(html, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"<p>", "\n", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"<[^>]+>", "");
            text = System.Net.WebUtility.HtmlDecode(text);
            return text.Trim();
        }

        private static string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return "(なし)";
            if (text.Length <= maxLength) return text;
            return text[..(maxLength - 1)] + "…";
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _gmailService?.Dispose();
            _gmailService = null;
            _credential = null;
        }
    }
}
