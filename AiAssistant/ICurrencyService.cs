using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AiAssistant
{
    /// <summary>
    /// 為替レート情報を表すクラス
    /// </summary>
    public sealed class CurrencyRate
    {
        public string BaseCurrency { get; set; } = "USD";
        public string TargetCurrency { get; set; } = string.Empty;
        public string CurrencyName { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// 為替レートサービスのインターフェース
    /// </summary>
    public interface ICurrencyService
    {
        /// <summary>
        /// 指定通貨の為替レートを取得します（USD基準）
        /// </summary>
        Task<CurrencyRate?> GetRateAsync(string currencyCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// 複数通貨の為替レートを取得します
        /// </summary>
        Task<IReadOnlyList<CurrencyRate>> GetRatesAsync(IEnumerable<string> currencyCodes, CancellationToken cancellationToken = default);

        /// <summary>
        /// 為替レートをサマリー文字列に変換します
        /// </summary>
        string FormatRatesSummary(IReadOnlyList<CurrencyRate> rates);
    }
}
