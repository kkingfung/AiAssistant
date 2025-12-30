using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AiAssistant
{
    /// <summary>
    /// メール情報を表すクラス
    /// </summary>
    public sealed class EmailInfo
    {
        public string Id { get; set; } = string.Empty;
        public string ThreadId { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string Snippet { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime ReceivedAt { get; set; }
        public bool IsUnread { get; set; }
        public bool IsStarred { get; set; }
        public IReadOnlyList<string> Labels { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Gmailサービスのインターフェース
    /// </summary>
    public interface IGmailService
    {
        /// <summary>
        /// OAuth2認証が完了しているかどうか
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// OAuth2認証を行います
        /// </summary>
        Task<bool> AuthenticateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 未読メールの一覧を取得します
        /// </summary>
        Task<IReadOnlyList<EmailInfo>> GetUnreadEmailsAsync(int maxResults = 10, CancellationToken cancellationToken = default);

        /// <summary>
        /// 最新のメール一覧を取得します
        /// </summary>
        Task<IReadOnlyList<EmailInfo>> GetRecentEmailsAsync(int maxResults = 10, CancellationToken cancellationToken = default);

        /// <summary>
        /// 指定したメールの詳細を取得します
        /// </summary>
        Task<EmailInfo?> GetEmailDetailsAsync(string messageId, CancellationToken cancellationToken = default);

        /// <summary>
        /// メールを既読にします
        /// </summary>
        Task<bool> MarkAsReadAsync(string messageId, CancellationToken cancellationToken = default);

        /// <summary>
        /// メールを未読にします
        /// </summary>
        Task<bool> MarkAsUnreadAsync(string messageId, CancellationToken cancellationToken = default);

        /// <summary>
        /// メール一覧をサマリー文字列に変換します
        /// </summary>
        string FormatEmailsSummary(IReadOnlyList<EmailInfo> emails, string label = "受信トレイ");
    }
}
