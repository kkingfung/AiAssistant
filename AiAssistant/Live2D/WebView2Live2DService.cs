#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Controls;
using AiAssistant.Companionship;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace AiAssistant.Live2D
{
    /// <summary>
    /// WebView2を使用したLive2Dサービス実装
    /// pixi-live2d-displayを使用してLive2Dモデルを表示
    /// </summary>
    public sealed class WebView2Live2DService : ILive2DService
    {
        private readonly WebView2 _webView;
        private bool _isDisposed;
        private TaskCompletionSource<bool>? _initializationTcs;

        /// <inheritdoc/>
        public bool IsInitialized { get; private set; }

        /// <inheritdoc/>
        public bool IsModelLoaded { get; private set; }

        /// <inheritdoc/>
        public string? CurrentModelPath { get; private set; }

        /// <inheritdoc/>
        public event EventHandler? ModelLoaded;

        /// <inheritdoc/>
        public event EventHandler<MotionFinishedEventArgs>? MotionFinished;

        /// <inheritdoc/>
        public event EventHandler<Live2DErrorEventArgs>? ErrorOccurred;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="webView">Live2D表示用のWebView2コントロール</param>
        public WebView2Live2DService(WebView2 webView)
        {
            _webView = webView ?? throw new ArgumentNullException(nameof(webView));
        }

        /// <inheritdoc/>
        public async Task InitializeAsync()
        {
            if (IsInitialized) return;

            try
            {
                _initializationTcs = new TaskCompletionSource<bool>();

                // WebView2環境を初期化
                var userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "AiAssistant", "WebView2Cache");

                Directory.CreateDirectory(userDataFolder);

                var environment = await CoreWebView2Environment.CreateAsync(
                    userDataFolder: userDataFolder);

                await _webView.EnsureCoreWebView2Async(environment);

                // 設定
                _webView.CoreWebView2.Settings.IsScriptEnabled = true;
                _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                _webView.CoreWebView2.Settings.IsZoomControlEnabled = false;
                _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;

                // 透明背景を設定
                _webView.DefaultBackgroundColor = System.Drawing.Color.Transparent;

                // JavaScriptからのメッセージを受信
                _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

                // HTMLファイルを読み込み
                var htmlPath = GetRendererHtmlPath();
                if (File.Exists(htmlPath))
                {
                    _webView.CoreWebView2.Navigate(new Uri(htmlPath).AbsoluteUri);
                }
                else
                {
                    // 埋め込みHTMLを使用
                    var html = GetEmbeddedHtml();
                    _webView.NavigateToString(html);
                }

                // 初期化完了を待機（タイムアウト5秒）
                var timeoutTask = Task.Delay(5000);
                var completedTask = await Task.WhenAny(_initializationTcs.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    Debug.WriteLine("[Live2D] 初期化タイムアウト");
                    // タイムアウトしても続行
                }

                IsInitialized = true;
                Debug.WriteLine("[Live2D] WebView2初期化完了");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Live2D] 初期化エラー: {ex.Message}");
                ErrorOccurred?.Invoke(this, new Live2DErrorEventArgs("初期化に失敗しました", ex));
                throw;
            }
        }

        /// <summary>
        /// HTMLレンダラーのパスを取得
        /// </summary>
        private static string GetRendererHtmlPath()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, "Live2D", "Resources", "live2d-renderer.html");
        }

        /// <summary>
        /// 埋め込みHTML（フォールバック用）
        /// </summary>
        private static string GetEmbeddedHtml()
        {
            return @"<!DOCTYPE html>
<html><head><title>Live2D</title>
<style>body{margin:0;background:transparent;}</style>
</head><body>
<div id='message'>Live2Dレンダラーを読み込めませんでした</div>
</body></html>";
        }

        /// <summary>
        /// WebView2からのメッセージを処理
        /// </summary>
        private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var json = e.WebMessageAsJson;
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var type = root.GetProperty("type").GetString();
                var data = root.GetProperty("data");

                Debug.WriteLine($"[Live2D] Message: {type}");

                switch (type)
                {
                    case "initialized":
                        _initializationTcs?.TrySetResult(true);
                        break;

                    case "modelLoaded":
                        IsModelLoaded = true;
                        CurrentModelPath = data.GetProperty("path").GetString();
                        ModelLoaded?.Invoke(this, EventArgs.Empty);
                        break;

                    case "modelUnloaded":
                        IsModelLoaded = false;
                        CurrentModelPath = null;
                        break;

                    case "motionFinished":
                        MotionFinished?.Invoke(this, new MotionFinishedEventArgs("", 0));
                        break;

                    case "error":
                        var errorMsg = data.GetProperty("message").GetString() ?? "Unknown error";
                        ErrorOccurred?.Invoke(this, new Live2DErrorEventArgs(errorMsg));
                        break;

                    case "hit":
                        // ヒット判定（将来の拡張用）
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Live2D] メッセージ処理エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// JavaScriptを実行
        /// </summary>
        private async Task ExecuteScriptAsync(string script)
        {
            if (!IsInitialized || _webView.CoreWebView2 == null) return;

            try
            {
                await _webView.CoreWebView2.ExecuteScriptAsync(script);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Live2D] Script execution error: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task LoadModelAsync(string modelPath)
        {
            if (!IsInitialized)
            {
                await InitializeAsync();
            }

            // パスをJavaScript用にエスケープ
            var escapedPath = modelPath.Replace("\\", "/").Replace("'", "\\'");
            await ExecuteScriptAsync($"window.live2d.loadModel('{escapedPath}')");
        }

        /// <inheritdoc/>
        public async Task UnloadModelAsync()
        {
            await ExecuteScriptAsync("window.live2d.unloadModel()");
            IsModelLoaded = false;
            CurrentModelPath = null;
        }

        /// <inheritdoc/>
        public async Task SetExpressionAsync(EmotionType emotion)
        {
            var expressionName = Live2DExpressionMapper.GetExpression(emotion);
            await SetExpressionByNameAsync(expressionName);
        }

        /// <inheritdoc/>
        public async Task SetExpressionByNameAsync(string expressionName)
        {
            await ExecuteScriptAsync($"window.live2d.setExpression('{expressionName}')");
        }

        /// <inheritdoc/>
        public async Task PlayMotionAsync(string group, int index = 0, MotionPriority priority = MotionPriority.Normal)
        {
            await ExecuteScriptAsync($"window.live2d.playMotion('{group}', {index}, {(int)priority})");
        }

        /// <inheritdoc/>
        public async Task SetLipSyncAsync(float value)
        {
            var clampedValue = Math.Clamp(value, 0f, 1f);
            await ExecuteScriptAsync($"window.live2d.setLipSync({clampedValue:F3})");
        }

        /// <inheritdoc/>
        public async Task SetLookAtAsync(double x, double y)
        {
            var clampedX = Math.Clamp(x, -1.0, 1.0);
            var clampedY = Math.Clamp(y, -1.0, 1.0);
            await ExecuteScriptAsync($"window.live2d.setLookAt({clampedX:F3}, {clampedY:F3})");
        }

        /// <inheritdoc/>
        public async Task SetParameterAsync(string paramId, float value)
        {
            await ExecuteScriptAsync($"window.live2d.setParameter('{paramId}', {value:F3})");
        }

        /// <inheritdoc/>
        public async Task SetVisibilityAsync(bool visible)
        {
            await ExecuteScriptAsync($"window.live2d.setVisibility({visible.ToString().ToLower()})");
        }

        /// <inheritdoc/>
        public async Task SetScaleAsync(double scale)
        {
            await ExecuteScriptAsync($"window.live2d.setScale({scale:F3})");
        }

        /// <inheritdoc/>
        public async Task SetPositionAsync(double x, double y)
        {
            await ExecuteScriptAsync($"window.live2d.setPosition({x:F1}, {y:F1})");
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_isDisposed) return;

            if (_webView.CoreWebView2 != null)
            {
                _webView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
            }

            _isDisposed = true;
            Debug.WriteLine("[Live2D] Service disposed");
        }
    }
}
