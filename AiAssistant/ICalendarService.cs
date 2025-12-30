using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AiAssistant
{
    /// <summary>
    /// カレンダーイベントを表すクラス
    /// </summary>
    public sealed class CalendarEvent
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsAllDay { get; set; }
        public string? Location { get; set; }
        public string? Description { get; set; }
        public string CalendarName { get; set; } = string.Empty;
        public string CalendarId { get; set; } = string.Empty;
    }

    /// <summary>
    /// カレンダーサービスのインターフェース
    /// </summary>
    public interface ICalendarService
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
        /// 今週のイベントを取得します
        /// </summary>
        Task<IReadOnlyList<CalendarEvent>> GetWeekEventsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 来週のイベントを取得します
        /// </summary>
        Task<IReadOnlyList<CalendarEvent>> GetNextWeekEventsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 今月のイベントを取得します
        /// </summary>
        Task<IReadOnlyList<CalendarEvent>> GetMonthEventsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 来月のイベントを取得します
        /// </summary>
        Task<IReadOnlyList<CalendarEvent>> GetNextMonthEventsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 指定期間のイベントを取得します
        /// </summary>
        Task<IReadOnlyList<CalendarEvent>> GetEventsAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default);

        /// <summary>
        /// イベントリストをサマリー文字列に変換します
        /// </summary>
        string FormatEventsSummary(IReadOnlyList<CalendarEvent> events, string periodLabel);
    }
}
