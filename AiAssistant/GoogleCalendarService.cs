using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace AiAssistant
{
    /// <summary>
    /// Google Calendar APIを使用したカレンダーサービス実装
    /// OAuth2認証フローを使用してユーザーのカレンダーにアクセスします
    /// </summary>
    public sealed class GoogleCalendarService : ICalendarService, IDisposable
    {
        private static readonly string[] Scopes = { CalendarService.Scope.CalendarReadonly };
        private const string ApplicationName = "AiAssistant";

        private CalendarService? _calendarService;
        private UserCredential? _credential;
        private bool _disposed;

        public bool IsAuthenticated => _credential != null && _calendarService != null;

        /// <summary>
        /// OAuth2認証を行います
        /// ブラウザが開き、ユーザーにGoogleアカウントへのアクセス許可を求めます
        /// </summary>
        public async Task<bool> AuthenticateAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // client_secret.jsonまたはappsettings.jsonから認証情報を取得
                var clientSecrets = GoogleCredentialHelper.LoadClientSecrets();
                if (clientSecrets == null)
                {
                    Console.WriteLine("[GoogleCalendar] 認証情報が見つかりません。client_secret.jsonを配置してください。");
                    return false;
                }

                // トークン保存先ディレクトリ
                var credPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "tokens",
                    "google-calendar"
                );

                // OAuth2認証フロー
                _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    clientSecrets,
                    Scopes,
                    "user",
                    cancellationToken,
                    new FileDataStore(credPath, true)
                );

                // Calendar APIサービスを作成
                _calendarService = new CalendarService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = _credential,
                    ApplicationName = ApplicationName
                });

                Console.WriteLine("[GoogleCalendar] 認証成功");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GoogleCalendar] 認証エラー: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 今週のイベントを取得します（月曜日から日曜日）
        /// </summary>
        public async Task<IReadOnlyList<CalendarEvent>> GetWeekEventsAsync(CancellationToken cancellationToken = default)
        {
            var today = DateTime.Today;
            var dayOfWeek = (int)today.DayOfWeek;
            // 月曜日を週の開始日とする
            var daysToMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
            var weekStart = today.AddDays(-daysToMonday);
            var weekEnd = weekStart.AddDays(7);

            return await GetEventsAsync(weekStart, weekEnd, cancellationToken);
        }

        /// <summary>
        /// 来週のイベントを取得します（月曜日から日曜日）
        /// </summary>
        public async Task<IReadOnlyList<CalendarEvent>> GetNextWeekEventsAsync(CancellationToken cancellationToken = default)
        {
            var today = DateTime.Today;
            var dayOfWeek = (int)today.DayOfWeek;
            // 月曜日を週の開始日とする
            var daysToMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
            var thisWeekStart = today.AddDays(-daysToMonday);
            var nextWeekStart = thisWeekStart.AddDays(7);
            var nextWeekEnd = nextWeekStart.AddDays(7);

            return await GetEventsAsync(nextWeekStart, nextWeekEnd, cancellationToken);
        }

        /// <summary>
        /// 今月のイベントを取得します
        /// </summary>
        public async Task<IReadOnlyList<CalendarEvent>> GetMonthEventsAsync(CancellationToken cancellationToken = default)
        {
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var monthEnd = monthStart.AddMonths(1);

            return await GetEventsAsync(monthStart, monthEnd, cancellationToken);
        }

        /// <summary>
        /// 来月のイベントを取得します
        /// </summary>
        public async Task<IReadOnlyList<CalendarEvent>> GetNextMonthEventsAsync(CancellationToken cancellationToken = default)
        {
            var today = DateTime.Today;
            var nextMonthStart = new DateTime(today.Year, today.Month, 1).AddMonths(1);
            var nextMonthEnd = nextMonthStart.AddMonths(1);

            return await GetEventsAsync(nextMonthStart, nextMonthEnd, cancellationToken);
        }

        /// <summary>
        /// 指定期間のイベントを取得します（全カレンダーから）
        /// </summary>
        public async Task<IReadOnlyList<CalendarEvent>> GetEventsAsync(
            DateTime start,
            DateTime end,
            CancellationToken cancellationToken = default)
        {
            if (_calendarService == null)
            {
                var authenticated = await AuthenticateAsync(cancellationToken);
                if (!authenticated || _calendarService == null)
                {
                    return Array.Empty<CalendarEvent>();
                }
            }

            try
            {
                var result = new List<CalendarEvent>();

                // 全カレンダーを取得
                var calendarListRequest = _calendarService.CalendarList.List();
                var calendarList = await calendarListRequest.ExecuteAsync(cancellationToken);

                if (calendarList.Items == null)
                {
                    Console.WriteLine("[GoogleCalendar] カレンダーリストが空です");
                    return Array.Empty<CalendarEvent>();
                }

                Console.WriteLine($"[GoogleCalendar] {calendarList.Items.Count}個のカレンダーを検出");

                // 各カレンダーからイベントを取得
                foreach (var calendar in calendarList.Items)
                {
                    try
                    {
                        var calendarName = calendar.Summary ?? calendar.Id;
                        Console.WriteLine($"[GoogleCalendar] カレンダー '{calendarName}' を取得中...");

                        var request = _calendarService.Events.List(calendar.Id);
                        request.TimeMinDateTimeOffset = new DateTimeOffset(start);
                        request.TimeMaxDateTimeOffset = new DateTimeOffset(end);
                        request.ShowDeleted = false;
                        request.SingleEvents = true;
                        request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

                        var events = await request.ExecuteAsync(cancellationToken);

                        if (events.Items != null)
                        {
                            foreach (var item in events.Items)
                            {
                                var calEvent = new CalendarEvent
                                {
                                    Id = item.Id,
                                    Title = item.Summary ?? "(無題)",
                                    Location = item.Location,
                                    Description = item.Description,
                                    CalendarName = calendarName,
                                    CalendarId = calendar.Id
                                };

                                // 終日イベントかどうかを判定
                                if (item.Start.Date != null)
                                {
                                    calEvent.IsAllDay = true;
                                    calEvent.StartTime = DateTime.Parse(item.Start.Date);
                                    calEvent.EndTime = DateTime.Parse(item.End?.Date ?? item.Start.Date);
                                }
                                else if (item.Start.DateTimeDateTimeOffset.HasValue)
                                {
                                    calEvent.IsAllDay = false;
                                    calEvent.StartTime = item.Start.DateTimeDateTimeOffset.Value.LocalDateTime;
                                    calEvent.EndTime = item.End?.DateTimeDateTimeOffset?.LocalDateTime ?? item.Start.DateTimeDateTimeOffset.Value.LocalDateTime;
                                }

                                result.Add(calEvent);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[GoogleCalendar] カレンダー '{calendar.Summary}' の取得エラー: {ex.Message}");
                        // 個別のカレンダーエラーは無視して続行
                    }
                }

                // 開始時刻でソート
                result.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));

                Console.WriteLine($"[GoogleCalendar] 合計 {result.Count}件のイベントを取得しました");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GoogleCalendar] イベント取得エラー: {ex.Message}");
                return Array.Empty<CalendarEvent>();
            }
        }

        /// <summary>
        /// イベントリストをサマリー文字列に変換します
        /// </summary>
        public string FormatEventsSummary(IReadOnlyList<CalendarEvent> events, string periodLabel)
        {
            if (events.Count == 0)
            {
                return $"📅 {periodLabel}の予定\n\n予定はありません。";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"📅 {periodLabel}の予定 ({events.Count}件)");
            sb.AppendLine();

            // 日付でグループ化
            var grouped = events
                .GroupBy(e => e.StartTime.Date)
                .OrderBy(g => g.Key);

            foreach (var group in grouped)
            {
                var dateStr = group.Key.ToString("M月d日 (ddd)");
                sb.AppendLine($"【{dateStr}】");

                foreach (var evt in group.OrderBy(e => e.StartTime))
                {
                    // カレンダー名の短縮表示を作成
                    var calTag = GetCalendarTag(evt.CalendarName);

                    if (evt.IsAllDay)
                    {
                        sb.AppendLine($"  {calTag} {evt.Title} (終日)");
                    }
                    else
                    {
                        var timeStr = $"{evt.StartTime:HH:mm}～{evt.EndTime:HH:mm}";
                        sb.AppendLine($"  {calTag} {timeStr} {evt.Title}");
                    }

                    if (!string.IsNullOrWhiteSpace(evt.Location))
                    {
                        sb.AppendLine($"      📍 {evt.Location}");
                    }
                }
                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// カレンダー名からタグ/絵文字を取得します
        /// </summary>
        private static string GetCalendarTag(string calendarName)
        {
            if (string.IsNullOrEmpty(calendarName))
                return "・";

            var lower = calendarName.ToLowerInvariant();

            // 祝日カレンダー
            if (lower.Contains("祝日") || lower.Contains("holiday"))
                return "🎌";

            // 誕生日カレンダー
            if (lower.Contains("birthday") || lower.Contains("誕生"))
                return "🎂";

            // 仕事/業務
            if (lower.Contains("work") || lower.Contains("仕事") || lower.Contains("業務"))
                return "💼";

            // プライベート/個人
            if (lower.Contains("personal") || lower.Contains("private") || lower.Contains("個人"))
                return "🏠";

            // 家族
            if (lower.Contains("family") || lower.Contains("家族"))
                return "👨‍👩‍👧‍👦";

            // リマインダー
            if (lower.Contains("reminder") || lower.Contains("リマインダー") || lower.Contains("task"))
                return "⏰";

            // デフォルト
            return "📌";
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _calendarService?.Dispose();
            _calendarService = null;
            _credential = null;
        }
    }
}
