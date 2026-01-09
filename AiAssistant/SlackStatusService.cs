using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AiAssistant
{
    /// <summary>
    /// Slack ステータスサービスの実装
    /// Slack APIを使用してユーザーステータスを管理します
    /// 必要なスコープ: users.profile:read, users.profile:write, users:write
    /// </summary>
    public sealed class SlackStatusService : IStatusService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly SlackSettings _settings;
        private const string ApiBaseUrl = "https://slack.com/api";

        public string ServiceName => "Slack";

        public bool IsConfigured => _settings.IsConfigured;

        public SlackStatusService()
        {
            _settings = AppSettings.Instance.Slack;

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            if (IsConfigured)
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _settings.UserToken);
            }
        }

        /// <summary>
        /// 現在のステータスを取得します
        /// </summary>
        public async Task<UserStatus?> GetCurrentStatusAsync()
        {
            if (!IsConfigured)
            {
                return null;
            }

            try
            {
                var response = await _httpClient.GetAsync($"{ApiBaseUrl}/users.profile.get").ConfigureAwait(false);
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var result = JsonSerializer.Deserialize<SlackProfileResponse>(content);

                if (result?.Ok != true || result.Profile == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Slackプロファイル取得失敗: {result?.Error}");
                    return null;
                }

                var status = new UserStatus
                {
                    ServiceName = ServiceName,
                    StatusText = result.Profile.StatusText ?? string.Empty,
                    Emoji = result.Profile.StatusEmoji ?? string.Empty,
                    Presence = OnlineStatus.Online, // 別のAPIで取得必要
                    UpdatedAt = DateTime.Now
                };

                // ステータス有効期限を設定
                if (result.Profile.StatusExpiration > 0)
                {
                    status.ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(result.Profile.StatusExpiration).LocalDateTime;
                }

                return status;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Slackステータス取得エラー: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// ステータスを設定します
        /// </summary>
        public async Task<bool> SetStatusAsync(string statusText, string? emoji = null, int? expiresInMinutes = null)
        {
            if (!IsConfigured)
            {
                System.Diagnostics.Debug.WriteLine("Slack User Tokenが設定されていません");
                return false;
            }

            try
            {
                var profile = new Dictionary<string, object>
                {
                    ["status_text"] = statusText,
                    ["status_emoji"] = emoji ?? ":computer:"
                };

                // 有効期限を設定（Unix timestamp）
                if (expiresInMinutes.HasValue && expiresInMinutes.Value > 0)
                {
                    var expiresAt = DateTimeOffset.Now.AddMinutes(expiresInMinutes.Value).ToUnixTimeSeconds();
                    profile["status_expiration"] = expiresAt;
                }
                else
                {
                    profile["status_expiration"] = 0; // 無期限
                }

                var requestData = new Dictionary<string, object>
                {
                    ["profile"] = JsonSerializer.Serialize(profile)
                };

                var formContent = new FormUrlEncodedContent(
                    new[]
                    {
                        new KeyValuePair<string, string>("profile", JsonSerializer.Serialize(profile))
                    });

                var response = await _httpClient.PostAsync($"{ApiBaseUrl}/users.profile.set", formContent).ConfigureAwait(false);
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var result = JsonSerializer.Deserialize<SlackApiResponse>(content);

                if (result?.Ok == true)
                {
                    return true;
                }

                System.Diagnostics.Debug.WriteLine($"Slackステータス設定失敗: {result?.Error}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Slackステータス設定エラー: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ステータスをクリアします
        /// </summary>
        public async Task<bool> ClearStatusAsync()
        {
            return await SetStatusAsync(string.Empty, string.Empty).ConfigureAwait(false);
        }

        /// <summary>
        /// オンラインステータスを設定します
        /// </summary>
        public async Task<bool> SetPresenceAsync(OnlineStatus presence)
        {
            if (!IsConfigured)
            {
                return false;
            }

            try
            {
                // Slackは「auto」または「away」のみ設定可能
                var presenceValue = presence switch
                {
                    OnlineStatus.Online => "auto",
                    OnlineStatus.Idle => "away",
                    OnlineStatus.DoNotDisturb => "away", // DNDは別APIで設定
                    OnlineStatus.Invisible => "away",
                    OnlineStatus.Offline => "away",
                    _ => "auto"
                };

                var formContent = new FormUrlEncodedContent(
                    new[]
                    {
                        new KeyValuePair<string, string>("presence", presenceValue)
                    });

                var response = await _httpClient.PostAsync($"{ApiBaseUrl}/users.setPresence", formContent).ConfigureAwait(false);
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var result = JsonSerializer.Deserialize<SlackApiResponse>(content);

                if (result?.Ok == true)
                {
                    // DNDモードを設定
                    if (presence == OnlineStatus.DoNotDisturb)
                    {
                        await SetDndAsync(60).ConfigureAwait(false); // 1時間のDND
                    }

                    return true;
                }

                System.Diagnostics.Debug.WriteLine($"Slackプレゼンス設定失敗: {result?.Error}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Slackプレゼンス設定エラー: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// DND（おやすみモード）を設定します
        /// </summary>
        private async Task<bool> SetDndAsync(int durationMinutes)
        {
            try
            {
                var formContent = new FormUrlEncodedContent(
                    new[]
                    {
                        new KeyValuePair<string, string>("num_minutes", durationMinutes.ToString())
                    });

                var response = await _httpClient.PostAsync($"{ApiBaseUrl}/dnd.setSnooze", formContent).ConfigureAwait(false);
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var result = JsonSerializer.Deserialize<SlackApiResponse>(content);

                return result?.Ok == true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Slack APIが利用可能かチェックします
        /// </summary>
        public static async Task<bool> IsSlackAvailableAsync()
        {
            var settings = AppSettings.Instance.Slack;
            if (!settings.IsConfigured)
            {
                return false;
            }

            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", settings.UserToken);

                var response = await client.GetAsync($"{ApiBaseUrl}/auth.test").ConfigureAwait(false);
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var result = JsonSerializer.Deserialize<SlackApiResponse>(content);

                return result?.Ok == true;
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

    #region Slack API Models

    internal class SlackApiResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    internal class SlackProfileResponse : SlackApiResponse
    {
        [JsonPropertyName("profile")]
        public SlackProfile? Profile { get; set; }
    }

    internal class SlackProfile
    {
        [JsonPropertyName("status_text")]
        public string? StatusText { get; set; }

        [JsonPropertyName("status_emoji")]
        public string? StatusEmoji { get; set; }

        [JsonPropertyName("status_expiration")]
        public long StatusExpiration { get; set; }

        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("real_name")]
        public string? RealName { get; set; }
    }

    #endregion
}
