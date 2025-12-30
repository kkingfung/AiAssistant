using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AiAssistant
{
    /// <summary>
    /// Claude使用量サービスの実装
    /// Anthropic Admin APIから組織の使用量情報を取得します
    /// </summary>
    public sealed class ClaudeUsageService : IClaudeUsageService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string? _adminApiKey;
        private readonly string? _organizationId;
        private bool _disposed;

        // Anthropic Console URLs
        private const string ConsoleBillingUrl = "https://console.anthropic.com/settings/billing";
        private const string ConsoleUsageUrl = "https://console.anthropic.com/settings/usage";
        private const string ApiBaseUrl = "https://api.anthropic.com";

        // 料金（USD per 1M tokens）- Claude 3.5 Sonnet
        private const decimal InputPricePerMillion = 3.00m;
        private const decimal OutputPricePerMillion = 15.00m;

        public bool IsAvailable => !string.IsNullOrEmpty(_adminApiKey) && !string.IsNullOrEmpty(_organizationId);

        public ClaudeUsageService()
        {
            var settings = AppSettings.Instance.Anthropic;
            _adminApiKey = settings.AdminApiKey;
            _organizationId = settings.OrganizationId;

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(ApiBaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };

            if (!string.IsNullOrEmpty(_adminApiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("x-api-key", _adminApiKey);
                _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            }
        }

        /// <summary>
        /// 今月の使用量を取得します（組織Admin API使用）
        /// </summary>
        public async Task<ClaudeUsageInfo?> GetCurrentMonthUsageAsync(CancellationToken cancellationToken = default)
        {
            if (!IsAvailable)
            {
                Console.WriteLine("[ClaudeUsage] Admin API設定がありません。appsettings.jsonにAnthropic設定を追加してください。");
                return null;
            }

            try
            {
                // 今月の開始日と終了日
                var now = DateTime.UtcNow;
                var startDate = new DateTime(now.Year, now.Month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                // Admin API エンドポイント
                // GET /v1/organizations/{organization_id}/usage
                var url = $"/v1/organizations/{_organizationId}/usage?" +
                         $"start_date={startDate:yyyy-MM-dd}&" +
                         $"end_date={endDate:yyyy-MM-dd}";

                Console.WriteLine($"[ClaudeUsage] API呼び出し: {url}");

                var response = await _httpClient.GetAsync(url, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    Console.WriteLine($"[ClaudeUsage] APIエラー: {response.StatusCode} - {errorContent}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                Console.WriteLine($"[ClaudeUsage] レスポンス: {content}");

                var usageData = JsonDocument.Parse(content);

                // レスポンスをパース
                var usage = new ClaudeUsageInfo
                {
                    PeriodStart = startDate,
                    PeriodEnd = endDate
                };

                // 使用量データを集計
                if (usageData.RootElement.TryGetProperty("data", out var dataArray))
                {
                    foreach (var item in dataArray.EnumerateArray())
                    {
                        if (item.TryGetProperty("input_tokens", out var inputTokens))
                        {
                            usage.InputTokens += inputTokens.GetInt64();
                        }
                        if (item.TryGetProperty("output_tokens", out var outputTokens))
                        {
                            usage.OutputTokens += outputTokens.GetInt64();
                        }
                        if (item.TryGetProperty("model", out var model))
                        {
                            usage.ModelName = model.GetString() ?? "";
                        }
                        usage.RequestCount++;
                    }
                }

                // コスト推定
                usage.EstimatedCostUsd = EstimateCost(usage.InputTokens, usage.OutputTokens);

                Console.WriteLine($"[ClaudeUsage] 使用量取得成功: {usage.TotalTokens:N0} tokens, ${usage.EstimatedCostUsd:N2}");
                return usage;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[ClaudeUsage] HTTP エラー: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClaudeUsage] エラー: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// コンソールのURLを取得します
        /// </summary>
        public string GetConsoleUrl() => ConsoleUsageUrl;

        /// <summary>
        /// コンソールをブラウザで開きます
        /// </summary>
        public void OpenConsoleInBrowser()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = ConsoleUsageUrl,
                    UseShellExecute = true
                });
                Console.WriteLine("[ClaudeUsage] ブラウザでコンソールを開きました");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClaudeUsage] ブラウザを開けませんでした: {ex.Message}");
            }
        }

        /// <summary>
        /// 使用量をサマリー文字列に変換します
        /// </summary>
        public string FormatUsageSummary(ClaudeUsageInfo? usage)
        {
            var sb = new StringBuilder();
            sb.AppendLine("📊 Claude使用量 (組織)");
            sb.AppendLine();

            if (usage != null)
            {
                sb.AppendLine($"📅 期間: {usage.PeriodStart:yyyy/MM/dd} ～ {usage.PeriodEnd:yyyy/MM/dd}");
                sb.AppendLine();

                sb.AppendLine("【トークン使用量】");
                sb.AppendLine($"   📥 入力: {usage.InputTokens:N0} tokens");
                sb.AppendLine($"   📤 出力: {usage.OutputTokens:N0} tokens");
                sb.AppendLine($"   📊 合計: {usage.TotalTokens:N0} tokens");
                sb.AppendLine();

                sb.AppendLine("【リクエスト数】");
                sb.AppendLine($"   🔄 {usage.RequestCount:N0} 回");
                sb.AppendLine();

                sb.AppendLine("【推定コスト】");
                sb.AppendLine($"   💰 ${usage.EstimatedCostUsd:N2} USD");

                // 日本円換算（概算: 1 USD = 150 JPY）
                var jpyEstimate = usage.EstimatedCostUsd * 150;
                sb.AppendLine($"   💴 約 ¥{jpyEstimate:N0}");

                if (!string.IsNullOrEmpty(usage.ModelName))
                {
                    sb.AppendLine();
                    sb.AppendLine($"📌 モデル: {usage.ModelName}");
                }
            }
            else if (!IsAvailable)
            {
                sb.AppendLine("⚠️ Admin API設定が必要です");
                sb.AppendLine();
                sb.AppendLine("appsettings.json に以下を追加してください:");
                sb.AppendLine();
                sb.AppendLine("\"Anthropic\": {");
                sb.AppendLine("  \"AdminApiKey\": \"your-admin-api-key\",");
                sb.AppendLine("  \"OrganizationId\": \"your-org-id\"");
                sb.AppendLine("}");
                sb.AppendLine();
                sb.AppendLine("【Admin APIキーの取得方法】");
                sb.AppendLine("1. console.anthropic.com にログイン");
                sb.AppendLine("2. Settings → Organization → API Keys");
                sb.AppendLine("3. Admin APIキーを作成");
                sb.AppendLine();
                sb.AppendLine("【Organization IDの確認】");
                sb.AppendLine("Settings → Organization で確認できます");
            }
            else
            {
                sb.AppendLine("❌ 使用量データを取得できませんでした");
                sb.AppendLine();
                sb.AppendLine("考えられる原因:");
                sb.AppendLine("・APIキーが無効または期限切れ");
                sb.AppendLine("・Organization IDが正しくない");
                sb.AppendLine("・ネットワーク接続の問題");
                sb.AppendLine();
                sb.AppendLine("コンソールで直接確認してください。");
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// トークン数からコストを推定します
        /// </summary>
        public static decimal EstimateCost(long inputTokens, long outputTokens)
        {
            var inputCost = (inputTokens / 1_000_000m) * InputPricePerMillion;
            var outputCost = (outputTokens / 1_000_000m) * OutputPricePerMillion;
            return inputCost + outputCost;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _httpClient.Dispose();
        }
    }
}
