using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Documents;

namespace AiAssistant
{
    public partial class MainWindow : Window
    {
        private bool _isClickThrough;
        private bool _isChatOpen;
        private bool _isWaitingForResponse;

        private const int HOTKEY_ID = 0xB001;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const int WM_HOTKEY = 0x0312;
        private HwndSource? _hwndSource;

        private AssistantViewModel? _viewModel;
        private Button? _sendButton;

        public MainWindow()
        {
            InitializeComponent();

            SourceInitialized += OnSourceInitialized;
            Loaded += OnLoaded;
            Closed += OnClosed;
        }

        /// <summary>
        /// AIサービスを非同期で初期化します
        /// </summary>
        private async Task InitializeAiServiceAsync()
        {
            try
            {
                Console.WriteLine("[Init] AIサービス初期化開始...");
                var (aiService, serviceType) = await AiServiceFactory.CreateAsync();

                Console.WriteLine($"[Init] サービスタイプ: {serviceType}");
                Console.WriteLine($"[Init] サービスクラス: {aiService.GetType().Name}");

                _viewModel = new AssistantViewModel(aiService);
                DataContext = _viewModel;

                // サービスタイプに応じたメッセージを表示
                string message = serviceType switch
                {
                    var s when s.StartsWith("Ollama") => $"ローカルLLM ({serviceType}) を使用しています。",
                    "ChatGPT (Cloud)" => "ChatGPT (クラウド) を使用しています。",
                    "Mock (Demo)" => "MockAiService (デモ) を使用しています。設定を確認してください。",
                    _ => $"{serviceType} を使用しています。"
                };

                ShowTransientMessage(message, 3000);
                Console.WriteLine($"[Init] メッセージ: {message}");
                System.Diagnostics.Debug.WriteLine($"AIサービス初期化完了: {serviceType}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Init] エラー: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"AIサービス初期化エラー: {ex.Message}");

                // フォールバック
                _viewModel = new AssistantViewModel(new MockAiService());
                DataContext = _viewModel;
                ShowTransientMessage("AIサービスの初期化に失敗しました。MockAiServiceを使用します。", 3000);
            }
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            // ウィンドウを画面右下に配置
            var workArea = SystemParameters.WorkArea;
            Left = workArea.Right - Width - 10;
            Top = workArea.Bottom - Height - 10;

            // AIサービスを非同期で初期化
            await InitializeAiServiceAsync();
        }

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            var helper = new WindowInteropHelper(this);
            var hwnd = helper.Handle;
            if (hwnd == IntPtr.Zero) return;

            _hwndSource = HwndSource.FromHwnd(hwnd);
            if (_hwndSource != null)
                _hwndSource.AddHook(WndProc);

            // 初期状態：click-through無効（通常のウィンドウとして動作）
            _isClickThrough = false;
            ClickThroughHelper.SetClickThrough(this, _isClickThrough);

            // 註冊全域熱鍵 Ctrl+Alt+T 用來切換 click-through
            var vk = (uint)KeyInterop.VirtualKeyFromKey(Key.T);
            _ = RegisterHotKey(hwnd, HOTKEY_ID, MOD_CONTROL | MOD_ALT, vk);
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            if (_hwndSource != null)
                _hwndSource.RemoveHook(WndProc);

            var helper = new WindowInteropHelper(this);
            UnregisterHotKey(helper.Handle, HOTKEY_ID);
        }

        // 閉じるボタン
        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // チャットボタン
        private void OnChatButtonClick(object sender, RoutedEventArgs e)
        {
            ToggleChatBalloon();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_isClickThrough) return;
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                try { DragMove(); }
                catch { }
            }
        }

        private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ToggleClickThrough();
        }

        private void ToggleClickThrough()
        {
            _isClickThrough = !_isClickThrough;
            ClickThroughHelper.SetClickThrough(this, _isClickThrough);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                ToggleClickThrough();
                handled = true;
            }
            return IntPtr.Zero;
        }

        // チャットバルーンの表示/非表示を切り替え
        private void ToggleChatBalloon()
        {
            _isChatOpen = !_isChatOpen;
            ChatBalloon.Visibility = _isChatOpen ? Visibility.Visible : Visibility.Collapsed;

            if (_isChatOpen)
            {
                // チャットを開く時はクリックスルーを無効化
                if (_isClickThrough)
                {
                    _isClickThrough = false;
                    ClickThroughHelper.SetClickThrough(this, false);
                }

                // 送信ボタンの参照を取得（初回のみ）
                if (_sendButton == null)
                {
                    _sendButton = SendButton;
                }

                // ウィンドウをアクティブにしてフォーカスを設定
                this.Activate();
                ChatInputBox.Focus();
                Keyboard.Focus(ChatInputBox);
            }
        }

        // チャット閉じるボタン
        private void OnCloseChatClick(object sender, RoutedEventArgs e)
        {
            _isChatOpen = false;
            ChatBalloon.Visibility = Visibility.Collapsed;
        }

        // メッセージ送信ボタン
        private async void OnSendMessageClick(object sender, RoutedEventArgs e)
        {
            await SendChatMessageAsync();
        }

        // Enterキーでメッセージ送信
        private async void OnChatInputKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                e.Handled = true;
                await SendChatMessageAsync();
            }
        }

        // チャットメッセージを送信
        private async Task SendChatMessageAsync()
        {
            var message = ChatInputBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(message) || _viewModel == null || _isWaitingForResponse)
            {
                return;
            }

            // 送信中状態に設定
            _isWaitingForResponse = true;
            UpdateSendButtonState();

            // 入力フィールドをクリア
            ChatInputBox.Text = string.Empty;

            // ユーザーメッセージを表示
            AddChatMessage(message, isUser: true);

            // AIレスポンスを取得（ストリーミング）
            var responseTextBlock = AddChatMessage("入力中...", isUser: false);

            // ViewModelのResponseTextをリアルタイムで表示に反映（イベントハンドラを先に登録）
            PropertyChangedEventHandler handler = (s, e) =>
            {
                if (e.PropertyName == nameof(AssistantViewModel.ResponseText))
                {
                    Dispatcher.Invoke(() =>
                    {
                        responseTextBlock.Text = _viewModel.ResponseText;
                        ScrollChatToBottom();
                    });
                }
            };

            try
            {
                _viewModel.PropertyChanged += handler;
                await _viewModel.StreamPromptAsync(message);
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    responseTextBlock.Text = $"エラー: {ex.Message}";
                });
            }
            finally
            {
                _viewModel.PropertyChanged -= handler;

                // 送信完了状態に設定
                _isWaitingForResponse = false;
                UpdateSendButtonState();
            }
        }

        // 送信ボタンの状態を更新
        private void UpdateSendButtonState()
        {
            if (_sendButton != null)
            {
                _sendButton.IsEnabled = !_isWaitingForResponse;
                _sendButton.Opacity = _isWaitingForResponse ? 0.5 : 1.0;
            }
        }

        // チャットメッセージをUIに追加
        private TextBlock AddChatMessage(string message, bool isUser)
        {
            var messageContainer = new Border
            {
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(10, 6, 10, 6),
                CornerRadius = new CornerRadius(8),
                Background = isUser ? new SolidColorBrush(Color.FromRgb(30, 144, 255)) : new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                MaxWidth = 280
            };

            var textBlock = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                Foreground = isUser ? Brushes.White : Brushes.Black,
                FontSize = 13
            };

            messageContainer.Child = textBlock;
            ChatMessagesPanel.Children.Add(messageContainer);

            ScrollChatToBottom();

            return textBlock;
        }

        // チャットを最下部までスクロール
        private void ScrollChatToBottom()
        {
            ChatScrollViewer.ScrollToBottom();
        }

        // 一時的なメッセージを表示
        private async void ShowTransientMessage(string message, int durationMs)
        {
            try
            {
                TransientMessage.Text = message;
                TransientBorder.Visibility = Visibility.Visible;

                await Task.Delay(durationMs).ConfigureAwait(true);

                TransientBorder.Visibility = Visibility.Collapsed;
            }
            catch
            {
                TransientBorder.Visibility = Visibility.Collapsed;
            }
        }

        // 測試按鈕處理：顯示暫態文字 3 秒（UI 不阻塞）
        private async void OnTestButtonClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                TransientMessage.Text = "UI click detected";
                TransientBorder.Visibility = Visibility.Visible;

                // 顯示 3 秒
                await Task.Delay(3000).ConfigureAwait(true);

                TransientBorder.Visibility = Visibility.Collapsed;
            }
            catch
            {
                // 忽略取消或例外，保證 UI 清理
                TransientBorder.Visibility = Visibility.Collapsed;
            }
        }

        #region Win32 HotKey interop

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        #endregion
    }

    // ClickThroughHelper（Win32 interop, 保留既有功能）
    public static class ClickThroughHelper
    {
        private const int GWL_EXSTYLE = -20;
        private const long WS_EX_TRANSPARENT = 0x00000020L;
        private const long WS_EX_LAYERED = 0x00080000L;
        private const long WS_EX_NOACTIVATE = 0x08000000L;

        public static void SetClickThrough(Window window, bool enable)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));
            var helper = new WindowInteropHelper(window);
            var hwnd = helper.Handle;
            if (hwnd == IntPtr.Zero)
                throw new InvalidOperationException("Window handle not created yet. Call after SourceInitialized.");

            var exStylePtr = GetWindowLongPtr(hwnd, GWL_EXSTYLE);
            long exStyle = exStylePtr.ToInt64();

            if (enable)
                exStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT;
            else
                exStyle &= ~WS_EX_TRANSPARENT;

            SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(exStyle));
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
        }

        public static void SetNoActivate(Window window, bool enable)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));
            var helper = new WindowInteropHelper(window);
            var hwnd = helper.Handle;
            if (hwnd == IntPtr.Zero)
                throw new InvalidOperationException("Window handle not created yet. Call after SourceInitialized.");

            var exStylePtr = GetWindowLongPtr(hwnd, GWL_EXSTYLE);
            long exStyle = exStylePtr.ToInt64();

            if (enable)
                exStyle |= WS_EX_NOACTIVATE;
            else
                exStyle &= ~WS_EX_NOACTIVATE;

            SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(exStyle));
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);
        }

        #region Win32 interop (32/64-bit safe)

        private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
                return GetWindowLongPtr64(hWnd, nIndex);
            else
                return new IntPtr(GetWindowLong32(hWnd, nIndex));
        }

        private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            else
                return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
        private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_FRAMECHANGED = 0x0020;

        #endregion
    }
}