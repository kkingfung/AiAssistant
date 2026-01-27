#nullable enable
using System;
using System.Threading.Tasks;

namespace AiAssistant.Audio
{
    /// <summary>
    /// YouTubeプレーヤーサービスのインターフェース
    /// </summary>
    public interface IYouTubePlayerService : IDisposable
    {
        /// <summary>初期化済みかどうか</summary>
        bool IsInitialized { get; }

        /// <summary>再生中かどうか</summary>
        bool IsPlaying { get; }

        /// <summary>現在の動画タイトル</summary>
        string? CurrentTitle { get; }

        /// <summary>
        /// WebView2を初期化
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// 動画IDで再生
        /// </summary>
        Task PlayAsync(string videoId);

        /// <summary>
        /// URLで再生
        /// </summary>
        Task PlayUrlAsync(string url);

        /// <summary>
        /// プリセットのLo-Fiストリームを再生
        /// </summary>
        Task PlayLofiAsync();

        /// <summary>
        /// 再生/一時停止をトグル
        /// </summary>
        Task TogglePlayPauseAsync();

        /// <summary>
        /// 停止
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// 音量設定（0-100）
        /// </summary>
        Task SetVolumeAsync(int volume);

        /// <summary>
        /// 再生状態変更イベント
        /// </summary>
        event EventHandler<bool>? PlayStateChanged;
    }

    /// <summary>
    /// プリセットのYouTubeストリーム
    /// </summary>
    public static class YouTubePresets
    {
        /// <summary>Lofi Girl - beats to relax/study to</summary>
        public const string LofiGirl = "jfKfPfyJRdk";

        /// <summary>Chillhop Music - 24/7 lofi hip hop</summary>
        public const string Chillhop = "5yx6BWlEVcY";

        /// <summary>College Music - lofi hip hop</summary>
        public const string CollegeMusic = "lP26UCnoH9s";
    }
}
