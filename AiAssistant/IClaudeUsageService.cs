using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AiAssistant
{
    /// <summary>
    /// Claude API使用量情報を表すクラス
    /// </summary>
    public sealed class ClaudeUsageInfo
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public long InputTokens { get; set; }
        public long OutputTokens { get; set; }
        public long TotalTokens => InputTokens + OutputTokens;
        public decimal EstimatedCostUsd { get; set; }
        public string ModelName { get; set; } = string.Empty;
        public int RequestCount { get; set; }
    }

    /// <summary>
    /// Claude使用量サービスのインターフェース
    /// </summary>
    public interface IClaudeUsageService
    {
        /// <summary>
        /// 使用量情報を取得できるかどうか
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// 今月の使用量を取得します
        /// </summary>
        Task<ClaudeUsageInfo?> GetCurrentMonthUsageAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// コンソールのURLを取得します
        /// </summary>
        string GetConsoleUrl();

        /// <summary>
        /// 使用量をサマリー文字列に変換します
        /// </summary>
        string FormatUsageSummary(ClaudeUsageInfo? usage);
    }
}
