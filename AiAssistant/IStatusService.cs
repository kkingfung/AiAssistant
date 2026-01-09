using System;
using System.Threading.Tasks;

namespace AiAssistant
{
    /// <summary>
    /// ステータス情報を表すクラス
    /// </summary>
    public class UserStatus
    {
        /// <summary>ステータステキスト（例：「会議中」「作業中」など）</summary>
        public string StatusText { get; set; } = string.Empty;

        /// <summary>絵文字（例：:meeting:, :computer:）</summary>
        public string Emoji { get; set; } = string.Empty;

        /// <summary>オンラインステータス（Online, Away, DND, Offline）</summary>
        public OnlineStatus Presence { get; set; } = OnlineStatus.Offline;

        /// <summary>ステータスの有効期限</summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>サービス名（Discord, Slackなど）</summary>
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>最終更新日時</summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// オンラインステータスの列挙型
    /// </summary>
    public enum OnlineStatus
    {
        Online,     // オンライン
        Idle,       // 退席中
        DoNotDisturb, // 取り込み中
        Invisible,  // オフライン表示
        Offline     // オフライン
    }

    /// <summary>
    /// ステータスサービスのインターフェース
    /// </summary>
    public interface IStatusService
    {
        /// <summary>
        /// サービス名を取得します
        /// </summary>
        string ServiceName { get; }

        /// <summary>
        /// サービスが設定済みかどうか
        /// </summary>
        bool IsConfigured { get; }

        /// <summary>
        /// 現在のステータスを取得します
        /// </summary>
        Task<UserStatus?> GetCurrentStatusAsync();

        /// <summary>
        /// ステータスを設定します
        /// </summary>
        /// <param name="statusText">ステータステキスト</param>
        /// <param name="emoji">絵文字</param>
        /// <param name="expiresInMinutes">有効期限（分）、nullの場合は無期限</param>
        Task<bool> SetStatusAsync(string statusText, string? emoji = null, int? expiresInMinutes = null);

        /// <summary>
        /// ステータスをクリアします
        /// </summary>
        Task<bool> ClearStatusAsync();

        /// <summary>
        /// オンラインステータスを設定します
        /// </summary>
        Task<bool> SetPresenceAsync(OnlineStatus presence);
    }
}
