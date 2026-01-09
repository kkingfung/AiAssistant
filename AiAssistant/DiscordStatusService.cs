using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AiAssistant
{
    /// <summary>
    /// Discord ステータスサービスの実装
    /// Discord Webhookを使用してステータスを管理します
    /// 注意: Discord APIは直接ステータス変更をサポートしていないため、
    /// Webhookでカスタムステータスを通知として送信します
    /// </summary>
    public sealed class DiscordStatusService : IStatusService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly DiscordSettings _settings;

        public string ServiceName => "Discord";

        public bool IsConfigured => _settings.IsConfigured;

        public DiscordStatusService()
        {
            _settings = AppSettings.Instance.Discord;

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        /// <summary>
        /// 現在のステータスを取得します
        /// Discord APIではユーザー自身のステータス取得にBot Tokenが必要なため、
        /// 設定からキャッシュされた情報を返します
        /// </summary>
        public Task<UserStatus?> GetCurrentStatusAsync()
        {
            if (!IsConfigured)
            {
                return Task.FromResult<UserStatus?>(null);
            }

            // Discord APIは直接ステータス取得をサポートしていないため、
            // 設定に保存された最後のステータスを返す
            var status = new UserStatus
            {
                ServiceName = ServiceName,
                StatusText = _settings.LastStatusText,
                Emoji = _settings.LastEmoji,
                Presence = OnlineStatus.Online, // デフォルト
                UpdatedAt = DateTime.Now
            };

            return Task.FromResult<UserStatus?>(status);
        }

        /// <summary>
        /// ステータスを設定します（Webhookで通知を送信）
        /// </summary>
        public async Task<bool> SetStatusAsync(string statusText, string? emoji = null, int? expiresInMinutes = null)
        {
            if (!IsConfigured)
            {
                System.Diagnostics.Debug.WriteLine("Discord Webhookが設定されていません");
                return false;
            }

            try
            {
                var displayEmoji = emoji ?? ":computer:";
                var message = new
                {
                    content = $"{displayEmoji} **ステータス更新**: {statusText}",
                    username = "AI Assistant Status"
                };

                var json = JsonSerializer.Serialize(message);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_settings.WebhookUrl, content).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    // 設定に最後のステータスを保存
                    _settings.LastStatusText = statusText;
                    _settings.LastEmoji = emoji ?? string.Empty;
                    AppSettings.Instance.Save();
                    return true;
                }

                System.Diagnostics.Debug.WriteLine($"Discord Webhook送信失敗: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Discordステータス設定エラー: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ステータスをクリアします
        /// </summary>
        public async Task<bool> ClearStatusAsync()
        {
            if (!IsConfigured)
            {
                return false;
            }

            try
            {
                var message = new
                {
                    content = ":white_check_mark: **ステータスクリア**: ステータスが解除されました",
                    username = "AI Assistant Status"
                };

                var json = JsonSerializer.Serialize(message);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_settings.WebhookUrl, content).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    _settings.LastStatusText = string.Empty;
                    _settings.LastEmoji = string.Empty;
                    AppSettings.Instance.Save();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Discordステータスクリアエラー: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// オンラインステータスを設定します（Webhookでは直接設定不可）
        /// </summary>
        public async Task<bool> SetPresenceAsync(OnlineStatus presence)
        {
            // Webhookではプレゼンス設定は直接できないため、通知として送信
            var presenceText = presence switch
            {
                OnlineStatus.Online => "オンライン",
                OnlineStatus.Idle => "退席中",
                OnlineStatus.DoNotDisturb => "取り込み中",
                OnlineStatus.Invisible => "オフライン表示",
                OnlineStatus.Offline => "オフライン",
                _ => "不明"
            };

            return await SetStatusAsync($"プレゼンス: {presenceText}", ":bust_in_silhouette:").ConfigureAwait(false);
        }

        /// <summary>
        /// Discord APIが利用可能かチェックします
        /// </summary>
        public static async Task<bool> IsDiscordAvailableAsync()
        {
            var settings = AppSettings.Instance.Discord;
            if (!settings.IsConfigured)
            {
                return false;
            }

            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

                // Webhookの有効性をチェック（GETリクエストでWebhook情報取得）
                var response = await client.GetAsync(settings.WebhookUrl).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
