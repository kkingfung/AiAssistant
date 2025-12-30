using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace AiAssistant
{
    /// <summary>
    /// MUFG銀行のファンド情報を取得するサービス
    /// </summary>
    public sealed class MufgFundService : IFundService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly List<string> _fundUrls;
        private bool _disposed;

        public IReadOnlyList<string> FundUrls => _fundUrls.AsReadOnly();

        public MufgFundService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            _httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

            _fundUrls = new List<string>();
        }

        public MufgFundService(IEnumerable<string> fundUrls) : this()
        {
            _fundUrls.AddRange(fundUrls);
        }

        public void AddFundUrl(string url)
        {
            if (!_fundUrls.Contains(url))
            {
                _fundUrls.Add(url);
            }
        }

        public void RemoveFundUrl(string url)
        {
            _fundUrls.Remove(url);
        }

        /// <summary>
        /// 指定URLのファンド情報を取得します
        /// </summary>
        public async Task<FundInfo?> GetFundInfoAsync(string url, CancellationToken cancellationToken = default)
        {
            try
            {
                Console.WriteLine($"[MufgFund] ファンド情報取得: {url}");

                var html = await _httpClient.GetStringAsync(url, cancellationToken);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var fund = new FundInfo
                {
                    Url = url,
                    FundId = ExtractFundId(url)
                };

                // ファンド名を取得
                var nameNode = doc.DocumentNode.SelectSingleNode("//h1");
                if (nameNode != null)
                {
                    fund.Name = CleanText(nameNode.InnerText);
                    fund.ShortName = TruncateName(fund.Name, 30);
                }

                // 基準価額を取得 (id="kijyunKagaku")
                var priceNode = doc.DocumentNode.SelectSingleNode("//*[@id='kijyunKagaku']");
                if (priceNode != null)
                {
                    fund.Price = ParseDecimal(CleanText(priceNode.InnerText));
                }

                // 前日比を取得 (id="dayChange")
                var changeNode = doc.DocumentNode.SelectSingleNode("//*[@id='dayChange']");
                if (changeNode != null)
                {
                    var changeText = CleanText(changeNode.InnerText);
                    // +3円 or -3円 の形式から数値を抽出
                    var match = Regex.Match(changeText, @"([+-]?\d+(?:,\d+)*(?:\.\d+)?)");
                    if (match.Success)
                    {
                        fund.PriceChange = ParseDecimal(match.Groups[1].Value);
                    }
                }

                // 純資産総額を取得 (id="jyunShisan")
                var assetsNode = doc.DocumentNode.SelectSingleNode("//*[@id='jyunShisan']");
                if (assetsNode != null)
                {
                    fund.NetAssets = ParseDecimal(CleanText(assetsNode.InnerText));
                }

                // 日付を取得 (id="kijyunYmd")
                var dateNode = doc.DocumentNode.SelectSingleNode("//*[@id='kijyunYmd']");
                if (dateNode != null)
                {
                    var dateText = CleanText(dateNode.InnerText);
                    var dateMatch = Regex.Match(dateText, @"(\d{4})年(\d{1,2})月(\d{1,2})日");
                    if (dateMatch.Success)
                    {
                        fund.AsOfDate = new DateTime(
                            int.Parse(dateMatch.Groups[1].Value),
                            int.Parse(dateMatch.Groups[2].Value),
                            int.Parse(dateMatch.Groups[3].Value)
                        );
                    }
                }

                // パフォーマンスを取得
                ExtractPerformance(doc, fund);

                // 分配金情報を取得
                ExtractDistribution(doc, fund);

                Console.WriteLine($"[MufgFund] 取得成功: {fund.ShortName} - {fund.Price}円 (前日比: {fund.PriceChange:+#;-#;0}円)");
                return fund;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MufgFund] ファンド情報取得エラー: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 登録されている全ファンドの情報を取得します
        /// </summary>
        public async Task<IReadOnlyList<FundInfo>> GetAllFundsInfoAsync(CancellationToken cancellationToken = default)
        {
            var results = new List<FundInfo>();

            foreach (var url in _fundUrls)
            {
                var fund = await GetFundInfoAsync(url, cancellationToken);
                if (fund != null)
                {
                    results.Add(fund);
                }
            }

            return results;
        }

        /// <summary>
        /// ファンド情報をサマリー文字列に変換します
        /// </summary>
        public string FormatFundsSummary(IReadOnlyList<FundInfo> funds)
        {
            if (funds.Count == 0)
            {
                return "💹 ファンド情報\n\n登録されているファンドがありません。";
            }

            var sb = new StringBuilder();
            sb.AppendLine("💹 ファンド情報");
            sb.AppendLine();

            foreach (var fund in funds)
            {
                var changeSymbol = fund.PriceChange >= 0 ? "+" : "";
                var changeColor = fund.PriceChange >= 0 ? "📈" : "📉";

                sb.AppendLine($"【{fund.ShortName}】");
                sb.AppendLine($"  基準価額: {fund.Price:N0}円 {changeColor} {changeSymbol}{fund.PriceChange:N0}円");
                sb.AppendLine($"  純資産: {fund.NetAssets:N2}{fund.NetAssetsUnit}");

                if (fund.AsOfDate != default)
                {
                    sb.AppendLine($"  更新日: {fund.AsOfDate:yyyy/MM/dd}");
                }

                // パフォーマンス表示
                var perfParts = new List<string>();
                if (fund.Return1Month.HasValue)
                    perfParts.Add($"1M:{fund.Return1Month:+0.00;-0.00}%");
                if (fund.Return3Months.HasValue)
                    perfParts.Add($"3M:{fund.Return3Months:+0.00;-0.00}%");
                if (fund.Return1Year.HasValue)
                    perfParts.Add($"1Y:{fund.Return1Year:+0.00;-0.00}%");

                if (perfParts.Count > 0)
                {
                    sb.AppendLine($"  騰落率: {string.Join(" ", perfParts)}");
                }

                if (fund.LatestDistribution.HasValue && fund.LatestDistribution > 0)
                {
                    sb.AppendLine($"  直近分配金: {fund.LatestDistribution:N0}円");
                }

                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }

        private static string ExtractFundId(string url)
        {
            var match = Regex.Match(url, @"/([a-zA-Z0-9]+)\.html");
            return match.Success ? match.Groups[1].Value : "";
        }

        private static string TruncateName(string name, int maxLength)
        {
            if (name.Length <= maxLength) return name;

            // 括弧以前の部分を取得
            var parenIndex = name.IndexOf('（');
            if (parenIndex > 0 && parenIndex <= maxLength)
            {
                return name[..parenIndex];
            }

            return name[..maxLength] + "...";
        }

        private static void ExtractPerformance(HtmlDocument doc, FundInfo fund)
        {
            try
            {
                // パフォーマンステーブルを探す (performanceTable内のm_table)
                var perfSection = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'performanceTable')]");
                if (perfSection == null) return;

                var tables = perfSection.SelectNodes(".//table[contains(@class, 'm_table')]");
                if (tables == null || tables.Count < 2) return;

                // 2つ目のテーブルがデータテーブル（1つ目はラベル用）
                var dataTable = tables[1];
                var rows = dataTable.SelectNodes(".//tr");
                if (rows == null || rows.Count < 2) return;

                // 1行目: ヘッダー (1ヵ月, 3ヵ月, 6ヵ月, 1年, 3年, 5年, 設定来)
                // 2行目: トータルリターン
                var headerRow = rows[0];
                var dataRow = rows[1];

                var headers = headerRow.SelectNodes(".//th");
                var values = dataRow.SelectNodes(".//td");

                if (headers == null || values == null) return;

                for (int i = 0; i < Math.Min(headers.Count, values.Count); i++)
                {
                    var header = CleanText(headers[i].InnerText);
                    var value = CleanText(values[i].InnerText);

                    if (value == "--" || string.IsNullOrWhiteSpace(value)) continue;

                    if (header.Contains("1ヵ月") || header.Contains("1カ月"))
                    {
                        fund.Return1Month = ExtractPercentage(value);
                    }
                    else if (header.Contains("3ヵ月") || header.Contains("3カ月"))
                    {
                        fund.Return3Months = ExtractPercentage(value);
                    }
                    else if (header.Contains("6ヵ月") || header.Contains("6カ月"))
                    {
                        fund.Return6Months = ExtractPercentage(value);
                    }
                    else if (header.Contains("1年"))
                    {
                        fund.Return1Year = ExtractPercentage(value);
                    }
                    else if (header.Contains("設定"))
                    {
                        fund.ReturnSinceInception = ExtractPercentage(value);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MufgFund] パフォーマンス抽出エラー: {ex.Message}");
            }
        }

        private static void ExtractDistribution(HtmlDocument doc, FundInfo fund)
        {
            try
            {
                var text = doc.DocumentNode.InnerText;

                // 直近分配金を探す
                var distMatch = Regex.Match(text, @"直近決算時分配金[^\d]*(\d+(?:,\d+)*)円");
                if (distMatch.Success)
                {
                    fund.LatestDistribution = ParseDecimal(distMatch.Groups[1].Value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MufgFund] 分配金抽出エラー: {ex.Message}");
            }
        }

        private static decimal? ExtractDecimal(string text)
        {
            var match = Regex.Match(text, @"([\d,]+(?:\.\d+)?)");
            if (match.Success)
            {
                return ParseDecimal(match.Groups[1].Value);
            }
            return null;
        }

        private static decimal? ExtractPercentage(string text)
        {
            var match = Regex.Match(text, @"([+-]?\d+(?:\.\d+)?)%?");
            if (match.Success)
            {
                return ParseDecimal(match.Groups[1].Value);
            }
            return null;
        }

        private static decimal ParseDecimal(string value)
        {
            var cleaned = value.Replace(",", "").Replace("円", "").Replace("%", "").Trim();
            if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }
            return 0;
        }

        private static string CleanText(string text)
        {
            return Regex.Replace(text, @"\s+", " ").Trim();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _httpClient.Dispose();
        }
    }
}
