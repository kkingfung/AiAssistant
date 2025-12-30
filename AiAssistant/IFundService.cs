using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AiAssistant
{
    /// <summary>
    /// ファンド情報を表すクラス
    /// </summary>
    public sealed class FundInfo
    {
        public string FundId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal PriceChange { get; set; }
        public decimal PriceChangePercent { get; set; }
        public decimal NetAssets { get; set; }
        public string NetAssetsUnit { get; set; } = "億円";
        public DateTime AsOfDate { get; set; }
        public decimal? LatestDistribution { get; set; }
        public DateTime? LatestDistributionDate { get; set; }
        public string? Url { get; set; }

        // パフォーマンス
        public decimal? Return1Month { get; set; }
        public decimal? Return3Months { get; set; }
        public decimal? Return6Months { get; set; }
        public decimal? Return1Year { get; set; }
        public decimal? ReturnSinceInception { get; set; }
    }

    /// <summary>
    /// ファンドサービスのインターフェース
    /// </summary>
    public interface IFundService
    {
        /// <summary>
        /// 登録されているファンドURLのリスト
        /// </summary>
        IReadOnlyList<string> FundUrls { get; }

        /// <summary>
        /// ファンドURLを追加します
        /// </summary>
        void AddFundUrl(string url);

        /// <summary>
        /// ファンドURLを削除します
        /// </summary>
        void RemoveFundUrl(string url);

        /// <summary>
        /// 指定URLのファンド情報を取得します
        /// </summary>
        Task<FundInfo?> GetFundInfoAsync(string url, CancellationToken cancellationToken = default);

        /// <summary>
        /// 登録されている全ファンドの情報を取得します
        /// </summary>
        Task<IReadOnlyList<FundInfo>> GetAllFundsInfoAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// ファンド情報をサマリー文字列に変換します
        /// </summary>
        string FormatFundsSummary(IReadOnlyList<FundInfo> funds);
    }
}
