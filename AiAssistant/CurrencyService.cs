using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AiAssistant
{
    /// <summary>
    /// 為替レートサービスの実装
    /// Frankfurter API (https://frankfurter.app) を使用
    /// </summary>
    public sealed class CurrencyService : ICurrencyService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private bool _disposed;

        // 通貨コードと名前のマッピング
        private static readonly Dictionary<string, string> CurrencyNames = new()
        {
            { "JPY", "日本円" },
            { "HKD", "香港ドル" },
            { "KRW", "韓国ウォン" },
            { "USD", "米ドル" },
            { "EUR", "ユーロ" },
            { "GBP", "英ポンド" },
            { "CNY", "中国元" },
            { "TWD", "台湾ドル" },
            { "SGD", "シンガポールドル" },
            { "AUD", "豪ドル" },
            { "CAD", "カナダドル" },
            { "CHF", "スイスフラン" }
        };

        // 通貨の絵文字
        private static readonly Dictionary<string, string> CurrencyEmojis = new()
        {
            { "JPY", "🇯🇵" },
            { "HKD", "🇭🇰" },
            { "KRW", "🇰🇷" },
            { "USD", "🇺🇸" },
            { "EUR", "🇪🇺" },
            { "GBP", "🇬🇧" },
            { "CNY", "🇨🇳" },
            { "TWD", "🇹🇼" },
            { "SGD", "🇸🇬" },
            { "AUD", "🇦🇺" },
            { "CAD", "🇨🇦" },
            { "CHF", "🇨🇭" }
        };

        public CurrencyService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.frankfurter.app/"),
                Timeout = TimeSpan.FromSeconds(15)
            };
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiAssistant/1.0");
        }

        /// <summary>
        /// 指定通貨の為替レートを取得します（USD基準）
        /// </summary>
        public async Task<CurrencyRate?> GetRateAsync(string currencyCode, CancellationToken cancellationToken = default)
        {
            var rates = await GetRatesAsync(new[] { currencyCode }, cancellationToken);
            return rates.Count > 0 ? rates[0] : null;
        }

        /// <summary>
        /// 複数通貨の為替レートを取得します
        /// </summary>
        public async Task<IReadOnlyList<CurrencyRate>> GetRatesAsync(
            IEnumerable<string> currencyCodes,
            CancellationToken cancellationToken = default)
        {
            var result = new List<CurrencyRate>();

            try
            {
                var codes = string.Join(",", currencyCodes);
                var url = $"latest?from=USD&to={codes}";

                Console.WriteLine($"[Currency] 為替レートを取得: {url}");

                var response = await _httpClient.GetStringAsync(url, cancellationToken);
                var json = JsonDocument.Parse(response);

                var date = DateTime.Today;
                if (json.RootElement.TryGetProperty("date", out var dateElement))
                {
                    if (DateTime.TryParse(dateElement.GetString(), out var parsedDate))
                    {
                        date = parsedDate;
                    }
                }

                if (json.RootElement.TryGetProperty("rates", out var ratesElement))
                {
                    foreach (var rate in ratesElement.EnumerateObject())
                    {
                        var currencyRate = new CurrencyRate
                        {
                            BaseCurrency = "USD",
                            TargetCurrency = rate.Name,
                            CurrencyName = CurrencyNames.GetValueOrDefault(rate.Name, rate.Name),
                            Rate = rate.Value.GetDecimal(),
                            LastUpdated = date
                        };
                        result.Add(currencyRate);
                    }
                }

                Console.WriteLine($"[Currency] {result.Count}件の為替レートを取得しました");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Currency] 為替レート取得エラー: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 為替レートをサマリー文字列に変換します
        /// </summary>
        public string FormatRatesSummary(IReadOnlyList<CurrencyRate> rates)
        {
            if (rates.Count == 0)
            {
                return "💱 為替レート\n\n為替情報を取得できませんでした。";
            }

            var sb = new StringBuilder();
            sb.AppendLine("💱 為替レート");
            sb.AppendLine();

            // JPYレートを取得
            var jpyRate = rates.FirstOrDefault(r => r.TargetCurrency == "JPY");
            var hkdRate = rates.FirstOrDefault(r => r.TargetCurrency == "HKD");
            var krwRate = rates.FirstOrDefault(r => r.TargetCurrency == "KRW");

            // 100円換算を表示（メイン）
            if (jpyRate != null)
            {
                sb.AppendLine("【100円換算】");

                if (hkdRate != null)
                {
                    // 100 JPY = X HKD
                    var jpyToHkd = (100m / jpyRate.Rate) * hkdRate.Rate;
                    sb.AppendLine($"   🇯🇵 100 円 = 🇭🇰 {jpyToHkd:N2} HKD");
                }

                if (krwRate != null)
                {
                    // 100 JPY = X KRW
                    var jpyToKrw = (100m / jpyRate.Rate) * krwRate.Rate;
                    sb.AppendLine($"   🇯🇵 100 円 = 🇰🇷 {jpyToKrw:N2} KRW");
                }

                sb.AppendLine();
            }

            // 逆換算（外貨→円）
            sb.AppendLine("【円への換算】");
            foreach (var rate in rates)
            {
                if (rate.TargetCurrency == "JPY") continue;

                var emoji = CurrencyEmojis.GetValueOrDefault(rate.TargetCurrency, "💵");
                var name = rate.CurrencyName;

                if (jpyRate != null)
                {
                    // 1 外貨 = X 円
                    var crossRate = jpyRate.Rate / rate.Rate;
                    sb.AppendLine($"   {emoji} 1 {rate.TargetCurrency} ({name}) = {crossRate:N2} 円");
                }
            }
            sb.AppendLine();

            // USD基準レート（参考）
            sb.AppendLine("【USD基準レート】");
            foreach (var rate in rates)
            {
                var emoji = CurrencyEmojis.GetValueOrDefault(rate.TargetCurrency, "💵");
                sb.AppendLine($"   {emoji} 1 USD = {rate.Rate:N2} {rate.TargetCurrency}");
            }
            sb.AppendLine();

            if (rates.Count > 0)
            {
                sb.AppendLine($"更新日: {rates[0].LastUpdated:yyyy/MM/dd}");
            }

            return sb.ToString().TrimEnd();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _httpClient.Dispose();
        }
    }
}
