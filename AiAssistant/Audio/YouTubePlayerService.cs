#nullable enable
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Wpf;

namespace AiAssistant.Audio
{
    /// <summary>
    /// YouTubeプレーヤーサービス（WebView2使用）
    /// </summary>
    public sealed class YouTubePlayerService : IYouTubePlayerService
    {
        private readonly WebView2 _webView;
        private bool _isDisposed;
        private int _volume = 30;

        /// <inheritdoc/>
        public bool IsInitialized { get; private set; }

        /// <inheritdoc/>
        public bool IsPlaying { get; private set; }

        /// <inheritdoc/>
        public string? CurrentTitle { get; private set; }

        /// <inheritdoc/>
        public event EventHandler<bool>? PlayStateChanged;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="webView">WebView2コントロール</param>
        public YouTubePlayerService(WebView2 webView)
        {
            _webView = webView ?? throw new ArgumentNullException(nameof(webView));
        }

        /// <inheritdoc/>
        public async Task InitializeAsync()
        {
            if (IsInitialized) return;

            try
            {
                await _webView.EnsureCoreWebView2Async();

                // 最小限のHTMLページを作成
                var html = GetPlayerHtml();
                _webView.NavigateToString(html);

                IsInitialized = true;
                Debug.WriteLine("[YouTube] 初期化完了");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[YouTube] 初期化エラー: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task PlayAsync(string videoId)
        {
            if (!IsInitialized) return;

            try
            {
                // YouTube埋め込みURLに移動
                var embedUrl = $"https://www.youtube.com/embed/{videoId}?autoplay=1&loop=1&playlist={videoId}";
                _webView.Source = new Uri(embedUrl);

                IsPlaying = true;
                CurrentTitle = $"YouTube: {videoId}";
                PlayStateChanged?.Invoke(this, true);

                // 音量を設定（少し遅延させる）
                await Task.Delay(2000);
                await SetVolumeAsync(_volume);

                Debug.WriteLine($"[YouTube] 再生開始: {videoId}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[YouTube] 再生エラー: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task PlayUrlAsync(string url)
        {
            var videoId = ExtractVideoId(url);
            if (!string.IsNullOrEmpty(videoId))
            {
                await PlayAsync(videoId);
            }
        }

        /// <inheritdoc/>
        public async Task PlayLofiAsync()
        {
            // Lofi Girl のライブストリームを再生
            await PlayAsync(YouTubePresets.LofiGirl);
            CurrentTitle = "Lofi Girl - beats to relax/study to";
        }

        /// <inheritdoc/>
        public async Task TogglePlayPauseAsync()
        {
            if (!IsInitialized) return;

            try
            {
                // JavaScriptでプレーヤーを制御
                var script = IsPlaying
                    ? "document.querySelector('video')?.pause();"
                    : "document.querySelector('video')?.play();";

                await _webView.ExecuteScriptAsync(script);

                IsPlaying = !IsPlaying;
                PlayStateChanged?.Invoke(this, IsPlaying);

                Debug.WriteLine($"[YouTube] {(IsPlaying ? "再生" : "一時停止")}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[YouTube] トグルエラー: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public Task StopAsync()
        {
            if (!IsInitialized) return Task.CompletedTask;

            try
            {
                // 空のHTMLに戻す
                _webView.NavigateToString(GetPlayerHtml());

                IsPlaying = false;
                CurrentTitle = null;
                PlayStateChanged?.Invoke(this, false);

                Debug.WriteLine("[YouTube] 停止");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[YouTube] 停止エラー: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task SetVolumeAsync(int volume)
        {
            _volume = Math.Clamp(volume, 0, 100);

            if (!IsInitialized) return;

            try
            {
                var script = $"document.querySelector('video').volume = {_volume / 100.0};";
                await _webView.ExecuteScriptAsync(script);
                Debug.WriteLine($"[YouTube] 音量: {_volume}%");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[YouTube] 音量設定エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// URLから動画IDを抽出
        /// </summary>
        private static string? ExtractVideoId(string url)
        {
            // youtube.com/watch?v=VIDEO_ID
            var match = Regex.Match(url, @"[?&]v=([a-zA-Z0-9_-]{11})");
            if (match.Success) return match.Groups[1].Value;

            // youtu.be/VIDEO_ID
            match = Regex.Match(url, @"youtu\.be/([a-zA-Z0-9_-]{11})");
            if (match.Success) return match.Groups[1].Value;

            // youtube.com/embed/VIDEO_ID
            match = Regex.Match(url, @"youtube\.com/embed/([a-zA-Z0-9_-]{11})");
            if (match.Success) return match.Groups[1].Value;

            // 11文字のIDとして扱う
            if (Regex.IsMatch(url, @"^[a-zA-Z0-9_-]{11}$"))
                return url;

            return null;
        }

        /// <summary>
        /// プレーヤー用のHTML
        /// </summary>
        private static string GetPlayerHtml()
        {
            return @"
<!DOCTYPE html>
<html>
<head>
    <style>
        body { margin: 0; padding: 0; background: #1a1a2e; display: flex; align-items: center; justify-content: center; height: 100vh; }
        .placeholder { color: #666; font-family: sans-serif; text-align: center; }
    </style>
</head>
<body>
    <div class='placeholder'>Music Player Ready</div>
</body>
</html>";
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_isDisposed) return;

            IsPlaying = false;
            IsInitialized = false;
            _isDisposed = true;

            Debug.WriteLine("[YouTube] サービスを破棄");
        }
    }
}
