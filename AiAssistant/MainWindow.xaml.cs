using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Documents;
using WpfAnimatedGif;

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
        private CharacterAnimationController? _animationController;

        // クイックアクションサービス
        private ICalendarService? _calendarService;
        private IWeatherService? _weatherService;
        private IFundService? _fundService;
        private IGmailService? _gmailService;
        private ICurrencyService? _currencyService;
        private ClaudeUsageService? _claudeUsageService;
        private IChatHistoryService? _chatHistoryService;
        private ITranslationService? _translationService;
        private IGitHubService? _gitHubService;

        // 翻訳用ホットキー
        private const int TRANSLATE_HOTKEY_ID = 0xB002;
        private const uint MOD_SHIFT = 0x0004;

        // 現在表示中のメール一覧（インタラクション用）
        private IReadOnlyList<EmailInfo>? _currentEmails;

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

                // 翻訳サービスを初期化
                InitializeTranslationService();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Init] エラー: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"AIサービス初期化エラー: {ex.Message}");

                // フォールバック
                _viewModel = new AssistantViewModel(new MockAiService());
                DataContext = _viewModel;
                ShowTransientMessage("AIサービスの初期化に失敗しました。MockAiServiceを使用します。", 3000);

                // 翻訳サービスを初期化（Mock使用時も）
                InitializeTranslationService();
            }
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            // 保存されたウィンドウサイズを適用
            ApplySavedWindowSize();

            // ウィンドウを画面右下に配置
            var workArea = SystemParameters.WorkArea;
            Left = workArea.Right - Width - 10;
            Top = workArea.Bottom - Height - 10;

            // アバター画像を読み込み
            LoadAvatar();

            // キャラクターアニメーションを初期化
            InitializeCharacterAnimation();

            // テーマを適用
            ApplyTheme();

            // AIサービスを非同期で初期化
            await InitializeAiServiceAsync();

            // クイックアクションサービスを初期化
            InitializeQuickActionServices();
        }

        /// <summary>
        /// 保存されたウィンドウサイズを適用します
        /// </summary>
        private void ApplySavedWindowSize()
        {
            var settings = AppSettings.Instance.Assistant;
            var sizeKey = settings.WindowSize;

            // サイズプリセットを取得 (width, height, charSize, chatWidth, chatHeight)
            var (width, height, charSize, chatWidth, chatHeight) = sizeKey switch
            {
                "Small" => (180.0, 200.0, 160.0, 170.0, 190.0),
                "Medium" => (240.0, 267.0, 220.0, 230.0, 257.0),
                "Large" => (360.0, 400.0, 320.0, 340.0, 380.0),
                _ => (360.0, 400.0, 320.0, 340.0, 380.0)
            };

            // ウィンドウサイズを適用
            this.Width = width;
            this.Height = height;

            // キャラクター表示エリアをリサイズ
            CharacterBorder.Width = charSize;
            CharacterBorder.Height = charSize;

            // チャットボックスをリサイズ
            ChatBalloon.Width = chatWidth;
            ChatBalloon.Height = chatHeight;

            Console.WriteLine($"[WindowSize] 保存されたサイズを適用: {sizeKey} ({width}×{height})");
        }

        /// <summary>
        /// クイックアクションサービスを初期化します
        /// </summary>
        private void InitializeQuickActionServices()
        {
            var settings = AppSettings.Instance;

            // 天気サービス
            _weatherService = new WeatherService(
                settings.Weather.City,
                settings.Weather.Latitude,
                settings.Weather.Longitude
            );

            // ファンドサービス
            _fundService = new MufgFundService(settings.Fund.FundUrls);

            // Google関連サービスは認証時に初期化
            _calendarService = new GoogleCalendarService();
            _gmailService = new GmailService();

            // 為替レートサービス
            _currencyService = new CurrencyService();

            // Claude使用量サービス
            _claudeUsageService = new ClaudeUsageService();

            // GitHubサービス
            try
            {
                if (AppSettings.Instance.GitHub.IsConfigured)
                {
                    _gitHubService = new GitHubService();
                    Console.WriteLine("[GitHub] GitHubサービスを初期化しました");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GitHub] 初期化エラー: {ex.Message}");
            }

            // 会話履歴サービス
            _chatHistoryService = new ChatHistoryService();

            // 前回のセッションを復元
            RestoreChatHistory();

            Console.WriteLine("[QuickAction] クイックアクションサービスを初期化しました");
        }

        /// <summary>
        /// 翻訳サービスを初期化します（AIサービス初期化後に呼び出す）
        /// </summary>
        private void InitializeTranslationService()
        {
            if (_viewModel?.AiService != null)
            {
                _translationService = new TranslationService(_viewModel.AiService);
                Console.WriteLine("[Translation] 翻訳サービスを初期化しました");
            }
        }

        /// <summary>
        /// 前回の会話履歴を復元します
        /// </summary>
        private void RestoreChatHistory()
        {
            if (_chatHistoryService == null || !_chatHistoryService.IsEnabled) return;

            try
            {
                var lastSession = _chatHistoryService.RestoreLastSession();
                if (lastSession != null && lastSession.Messages.Count > 0)
                {
                    // チャットパネルに履歴を表示
                    foreach (var message in lastSession.Messages)
                    {
                        if (message.Role == "user")
                        {
                            AddChatMessage(message.Content, isUser: true);
                        }
                        else if (message.Role == "assistant")
                        {
                            AddChatMessage(message.Content, isUser: false);
                        }
                    }

                    Console.WriteLine($"[ChatHistory] {lastSession.Messages.Count}件の履歴を復元しました");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChatHistory] 履歴復元エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// キャラクターアニメーションを初期化します
        /// </summary>
        private void InitializeCharacterAnimation()
        {
            var settings = AppSettings.Instance.Assistant;
            var animationsFolder = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                settings.CharacterAnimationsFolder
            );

            try
            {
                // CharacterAnimationControllerを作成
                _animationController = new CharacterAnimationController(
                    AvatarImage,
                    animationsFolder,
                    settings.SelectedPet
                );

                // 切り替え間隔を設定
                _animationController.SwitchIntervalMs = settings.AnimationSwitchIntervalSeconds * 1000;

                // アニメーション再生を開始
                _animationController.Start();

                // AvatarImageを表示、プレースホルダーを非表示
                AvatarImage.Visibility = Visibility.Visible;
                PlaceholderViewbox.Visibility = Visibility.Collapsed;

                Console.WriteLine($"[CharacterAnimation] {settings.SelectedPet} アニメーションシステムを初期化しました");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CharacterAnimation] 初期化エラー: {ex.Message}");
                // エラー時はプレースホルダーを表示
                AvatarImage.Visibility = Visibility.Collapsed;
                PlaceholderViewbox.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// アバター画像を読み込みます（静止画用・廃止予定）
        /// </summary>
        private void LoadAvatar()
        {
            // キャラクターアニメーションシステムを使用するため、この機能は非推奨
            // 必要に応じて静止画を表示する場合のみ使用
            var settings = AppSettings.Instance.Assistant;

            if (settings.HasAvatar && _animationController == null)
            {
                try
                {
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(settings.AvatarImagePath, UriKind.Absolute);
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    AvatarImage.Source = bitmap;
                    AvatarImage.Visibility = Visibility.Visible;
                    PlaceholderViewbox.Visibility = Visibility.Collapsed;

                    Console.WriteLine($"[Avatar] アバター画像を読み込みました: {settings.AvatarImagePath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Avatar] アバター画像の読み込みに失敗: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// テーマを適用します
        /// </summary>
        private void ApplyTheme()
        {
            var settings = AppSettings.Instance.Assistant;

            if (settings.IsDarkTheme)
            {
                // ダークテーマの色
                ChatBalloon.Background = new SolidColorBrush(Color.FromArgb(245, 30, 30, 30));
                TransientBorder.Background = new SolidColorBrush(Color.FromArgb(220, 50, 50, 50));
            }
            else
            {
                // ライトテーマの色（デフォルト）
                ChatBalloon.Background = new SolidColorBrush(Color.FromArgb(240, 255, 255, 255));
                TransientBorder.Background = new SolidColorBrush(Color.FromArgb(220, 0, 0, 0));
            }
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

            // 翻訳用ホットキー Ctrl+Shift+T を登録
            _ = RegisterHotKey(hwnd, TRANSLATE_HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, vk);
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            if (_hwndSource != null)
                _hwndSource.RemoveHook(WndProc);

            var helper = new WindowInteropHelper(this);
            UnregisterHotKey(helper.Handle, HOTKEY_ID);
            UnregisterHotKey(helper.Handle, TRANSLATE_HOTKEY_ID);

            // アニメーションコントローラーをクリーンアップ
            _animationController?.Dispose();

            // クイックアクションサービスをクリーンアップ
            (_calendarService as IDisposable)?.Dispose();
            (_weatherService as IDisposable)?.Dispose();
            (_fundService as IDisposable)?.Dispose();
            (_gmailService as IDisposable)?.Dispose();
            (_currencyService as IDisposable)?.Dispose();
            _claudeUsageService?.Dispose();
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

        // ペット選択ボタン
        private void OnPetSelectorButtonClick(object sender, RoutedEventArgs e)
        {
            TogglePetSelector();
        }

        /// <summary>
        /// 設定ウィンドウを開きます
        /// </summary>
        private void OpenSettingsWindow()
        {
            try
            {
                var settingsWindow = new SettingsWindow(_chatHistoryService);
                settingsWindow.Owner = this;

                var result = settingsWindow.ShowDialog();

                if (result == true && settingsWindow.SettingsSaved)
                {
                    // 設定をリロード
                    AppSettings.Reload();

                    // テーマを再適用
                    ApplyTheme();

                    ShowTransientMessage("設定を保存しました。一部の変更は再起動後に反映されます。", 3000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Settings] 設定ウィンドウエラー: {ex.Message}");
                ShowTransientMessage("設定ウィンドウを開けませんでした", 2000);
            }
        }

        // ペット選択ポップアップの表示/非表示を切り替え
        private void TogglePetSelector()
        {
            bool isOpen = PetSelectorPopup.Visibility == Visibility.Visible;
            PetSelectorPopup.Visibility = isOpen ? Visibility.Collapsed : Visibility.Visible;

            if (!isOpen)
            {
                // 開く時はチャットを閉じる
                ChatBalloon.Visibility = Visibility.Collapsed;
                _isChatOpen = false;

                // クリックスルーを無効化
                if (_isClickThrough)
                {
                    _isClickThrough = false;
                    ClickThroughHelper.SetClickThrough(this, false);
                }

                // 現在選択されているペットをハイライト
                HighlightSelectedPet();
            }
        }

        // ペット選択ポップアップを閉じる
        private void OnClosePetSelectorClick(object sender, RoutedEventArgs e)
        {
            PetSelectorPopup.Visibility = Visibility.Collapsed;
        }

        // ペットが選択された時の処理
        private void OnPetSelected(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string petType)
            {
                ChangePet(petType);
                PetSelectorPopup.Visibility = Visibility.Collapsed;
            }
        }

        // ペットを変更する
        private void ChangePet(string petType)
        {
            try
            {
                // 設定を更新
                var settings = AppSettings.Instance.Assistant;
                settings.SelectedPet = petType;
                AppSettings.Instance.Save();

                // 古いアニメーションコントローラーを完全に停止・破棄
                if (_animationController != null)
                {
                    _animationController.Stop();
                    _animationController.Dispose();
                    _animationController = null;
                }

                // Image コントロールをクリア
                WpfAnimatedGif.ImageBehavior.SetAnimatedSource(AvatarImage, null);
                AvatarImage.Source = null;
                AvatarImage.Visibility = Visibility.Collapsed;

                // 強制的にUIを更新
                AvatarImage.UpdateLayout();

                // 新しいペットでアニメーションを再初期化
                InitializeCharacterAnimation();

                // メッセージを表示
                string petName = petType switch
                {
                    "Cat" => "🐱 Cat",
                    "Crab" => "🦀 Crab",
                    "Dragon" => "🐉 Dragon",
                    "Frog" => "🐸 Frog",
                    "Shark" => "🦈 Shark",
                    "Snake" => "🐍 Snake",
                    "Random" => "🎲 Random",
                    _ => petType
                };

                ShowTransientMessage($"{petName} に変更しました！", 2000);
                Console.WriteLine($"[PetSelector] ペットを変更: {petType}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PetSelector] ペット変更エラー: {ex.Message}");
                ShowTransientMessage("ペットの変更に失敗しました", 2000);
            }
        }

        // 現在選択されているペットをハイライト
        private void HighlightSelectedPet()
        {
            var settings = AppSettings.Instance.Assistant;
            var selectedPet = settings.SelectedPet;

            // すべてのボタンをリセット
            ResetPetButtonStyles();

            // 選択されているペットのボタンをハイライト
            Button? selectedButton = selectedPet switch
            {
                "Cat" => PetCatButton,
                "Crab" => PetCrabButton,
                "Dragon" => PetDragonButton,
                "Frog" => PetFrogButton,
                "Shark" => PetSharkButton,
                "Snake" => PetSnakeButton,
                "Random" => PetRandomButton,
                _ => null
            };

            if (selectedButton != null && selectedButton.Parent is Border border)
            {
                border.Background = new SolidColorBrush(Color.FromRgb(147, 112, 219)); // Purple
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(138, 43, 226));
                border.BorderThickness = new Thickness(2);

                if (selectedButton.Content is TextBlock textBlock)
                {
                    textBlock.Foreground = Brushes.White;
                }
            }
        }

        // ペットボタンのスタイルをリセット
        private void ResetPetButtonStyles()
        {
            var buttons = new[] { PetCatButton, PetCrabButton, PetDragonButton, PetFrogButton, PetSharkButton, PetSnakeButton };

            foreach (var button in buttons)
            {
                if (button.Parent is Border border)
                {
                    border.Background = Brushes.White;
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(221, 221, 221));
                    border.BorderThickness = new Thickness(1);
                }

                if (button.Content is TextBlock textBlock)
                {
                    textBlock.Foreground = Brushes.Black;
                }
            }

            // Random button has different default style
            if (PetRandomButton.Parent is Border randomBorder)
            {
                randomBorder.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
            }
            if (PetRandomButton.Content is TextBlock randomTextBlock)
            {
                randomTextBlock.Foreground = Brushes.Black;
            }
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
            if (msg == WM_HOTKEY)
            {
                var hotkeyId = wParam.ToInt32();
                if (hotkeyId == HOTKEY_ID)
                {
                    ToggleClickThrough();
                    handled = true;
                }
                else if (hotkeyId == TRANSLATE_HOTKEY_ID)
                {
                    _ = TranslateClipboardTextAsync();
                    handled = true;
                }
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

            // 履歴に保存
            _chatHistoryService?.AddMessage("user", message);

            // AIレスポンスを取得（ストリーミング）
            var responseTextBlock = AddChatMessage("入力中...", isUser: false);

            // ViewModelのResponseTextをリアルタイムで表示に反映（イベントハンドラを先に登録）
            Border? responseBorder = null;
            PropertyChangedEventHandler handler = (s, e) =>
            {
                if (e.PropertyName == nameof(AssistantViewModel.ResponseText))
                {
                    Dispatcher.Invoke(() =>
                    {
                        // Markdownフォーマットでテキストを更新
                        var newTextBlock = MarkdownTextBlockHelper.CreateFormattedTextBlock(_viewModel.ResponseText, false);

                        // 親Borderを見つけて子を置き換え
                        if (responseBorder == null)
                        {
                            // 初回：responseTextBlockの親Borderを見つける
                            var parent = VisualTreeHelper.GetParent(responseTextBlock);
                            if (parent is Border border)
                            {
                                responseBorder = border;
                            }
                        }

                        if (responseBorder != null)
                        {
                            responseBorder.Child = newTextBlock;
                        }
                        else
                        {
                            // フォールバック：プレーンテキストとして更新
                            responseTextBlock.Text = _viewModel.ResponseText;
                        }

                        ScrollChatToBottom();
                    });
                }
            };

            try
            {
                _viewModel.PropertyChanged += handler;
                await _viewModel.StreamPromptAsync(message);

                // AIの応答を履歴に保存
                if (!string.IsNullOrEmpty(_viewModel.ResponseText))
                {
                    _chatHistoryService?.AddMessage("assistant", _viewModel.ResponseText);
                }
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
            var assistantSettings = AppSettings.Instance.Assistant;
            var isDark = assistantSettings.IsDarkTheme;
            var bubbleStyle = AppSettings.Instance.ChatBubble.GetCurrentStyle();

            // スタイルから色を取得
            var backgroundColor = isUser
                ? ChatBubbleStyle.ParseColor(bubbleStyle.UserBubbleColor)
                : ChatBubbleStyle.ParseColor(isDark ? bubbleStyle.AiBubbleColorDark : bubbleStyle.AiBubbleColorLight);

            var textColor = isUser
                ? ChatBubbleStyle.ParseColor(bubbleStyle.UserTextColor)
                : ChatBubbleStyle.ParseColor(isDark ? bubbleStyle.AiTextColorDark : bubbleStyle.AiTextColorLight);

            var messageContainer = new Border
            {
                Margin = new Thickness(0, 0, 0, bubbleStyle.Margin),
                Padding = new Thickness(bubbleStyle.Padding, bubbleStyle.Padding * 0.6, bubbleStyle.Padding, bubbleStyle.Padding * 0.6),
                CornerRadius = new CornerRadius(bubbleStyle.CornerRadius),
                Background = new SolidColorBrush(backgroundColor),
                HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                MaxWidth = 280,
                Opacity = bubbleStyle.Opacity
            };

            // 境界線スタイルを適用
            if (bubbleStyle.BorderThickness > 0)
            {
                messageContainer.BorderThickness = new Thickness(bubbleStyle.BorderThickness);
                messageContainer.BorderBrush = ChatBubbleStyle.ToBrush(bubbleStyle.BorderColor);
            }

            // シャドウを適用
            if (bubbleStyle.EnableShadow)
            {
                messageContainer.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = ChatBubbleStyle.ParseColor(bubbleStyle.ShadowColor),
                    BlurRadius = bubbleStyle.ShadowBlurRadius,
                    ShadowDepth = 2,
                    Opacity = 0.5
                };
            }

            // Markdownサポートを使用してTextBlockを作成
            var textBlock = isUser
                ? new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(textColor),
                    FontSize = bubbleStyle.FontSize,
                    FontFamily = new FontFamily(bubbleStyle.FontFamily)
                }
                : MarkdownTextBlockHelper.CreateFormattedTextBlock(message, isUser, bubbleStyle);

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

        #region Style Selector Handlers

        /// <summary>
        /// スタイル選択ボタンクリック - コンテキストメニューを表示
        /// </summary>
        private void OnStyleSelectorButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            var contextMenu = new ContextMenu();
            var currentPreset = AppSettings.Instance.ChatBubble.Preset;

            // 各プリセットをメニューに追加
            foreach (ChatBubblePreset preset in Enum.GetValues(typeof(ChatBubblePreset)))
            {
                if (preset == ChatBubblePreset.Custom) continue; // カスタムは別途処理

                var presetName = preset.ToString();
                var emoji = GetPresetEmoji(preset);
                var menuItem = new MenuItem
                {
                    Header = $"{emoji} {GetPresetDisplayName(preset)}",
                    IsChecked = currentPreset == presetName,
                    Tag = presetName
                };
                menuItem.Click += OnStylePresetSelected;
                contextMenu.Items.Add(menuItem);
            }

            contextMenu.PlacementTarget = button;
            contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            contextMenu.IsOpen = true;
        }

        /// <summary>
        /// プリセットの絵文字を取得します
        /// </summary>
        private static string GetPresetEmoji(ChatBubblePreset preset)
        {
            return preset switch
            {
                ChatBubblePreset.Modern => "💬",
                ChatBubblePreset.Minimal => "⬜",
                ChatBubblePreset.Glassmorphism => "🔮",
                ChatBubblePreset.Retro => "📺",
                ChatBubblePreset.PixelArt => "🎮",
                ChatBubblePreset.Neon => "💡",
                ChatBubblePreset.Pastel => "🌸",
                ChatBubblePreset.Dark => "🌙",
                _ => "🎨"
            };
        }

        /// <summary>
        /// プリセットの表示名を取得します
        /// </summary>
        private static string GetPresetDisplayName(ChatBubblePreset preset)
        {
            return preset switch
            {
                ChatBubblePreset.Modern => "Modern (モダン)",
                ChatBubblePreset.Minimal => "Minimal (ミニマル)",
                ChatBubblePreset.Glassmorphism => "Glass (ガラス風)",
                ChatBubblePreset.Retro => "Retro (レトロ)",
                ChatBubblePreset.PixelArt => "Pixel Art (ピクセル)",
                ChatBubblePreset.Neon => "Neon (ネオン)",
                ChatBubblePreset.Pastel => "Pastel (パステル)",
                ChatBubblePreset.Dark => "Dark (ダーク)",
                _ => preset.ToString()
            };
        }

        /// <summary>
        /// スタイルプリセットが選択されたときの処理
        /// </summary>
        private void OnStylePresetSelected(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem || menuItem.Tag is not string presetName) return;

            try
            {
                // 設定を更新
                AppSettings.Instance.ChatBubble.Preset = presetName;
                AppSettings.Instance.Save();

                // 通知
                var emoji = Enum.TryParse<ChatBubblePreset>(presetName, out var preset) ? GetPresetEmoji(preset) : "🎨";
                ShowTransientMessage($"{emoji} スタイルを {presetName} に変更しました", 2000);

                Console.WriteLine($"[StyleSelector] スタイルを変更: {presetName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StyleSelector] エラー: {ex.Message}");
                ShowTransientMessage("スタイルの変更に失敗しました", 2000);
            }
        }

        #endregion

        #region GitHub Handlers

        /// <summary>
        /// GitHubボタンクリック - コンテキストメニューを表示
        /// </summary>
        private void OnGitHubButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            var contextMenu = new ContextMenu();

            var notificationsItem = new MenuItem { Header = "🔔 通知を表示" };
            notificationsItem.Click += async (s, args) => await ShowGitHubNotificationsAsync();

            var prsItem = new MenuItem { Header = "📝 レビュー待ちPR" };
            prsItem.Click += async (s, args) => await ShowPullRequestsAwaitingReviewAsync();

            var markAllReadItem = new MenuItem { Header = "✅ すべて既読にする" };
            markAllReadItem.Click += async (s, args) => await MarkAllGitHubNotificationsReadAsync();

            contextMenu.Items.Add(notificationsItem);
            contextMenu.Items.Add(prsItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(markAllReadItem);

            contextMenu.PlacementTarget = button;
            contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            contextMenu.IsOpen = true;
        }

        /// <summary>
        /// GitHub通知を表示します
        /// </summary>
        private async Task ShowGitHubNotificationsAsync()
        {
            if (_gitHubService == null)
            {
                ShowTransientMessage("GitHubが設定されていません", 2000);
                return;
            }

            try
            {
                ShowTransientMessage("GitHub通知を取得中...", 30000);

                var notifications = await _gitHubService.GetNotificationsAsync();

                // チャットを開く
                if (!_isChatOpen)
                {
                    _isChatOpen = true;
                    ChatBalloon.Visibility = Visibility.Visible;
                }

                AddSystemMessage("🐙 GitHub通知");

                if (notifications.Count == 0)
                {
                    AddSystemMessage("未読の通知はありません");
                }
                else
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"**{notifications.Count}件の未読通知**\n");

                    foreach (var n in notifications.Take(10))
                    {
                        var icon = n.Type switch
                        {
                            "PullRequest" => "📝",
                            "Issue" => "🔵",
                            "Release" => "📦",
                            _ => "🔔"
                        };

                        sb.AppendLine($"{icon} **{n.Repository}**");
                        sb.AppendLine($"  {n.Title}");
                        sb.AppendLine($"  _{n.Reason}_ - {n.UpdatedAt:MM/dd HH:mm}");
                        sb.AppendLine();
                    }

                    if (notifications.Count > 10)
                    {
                        sb.AppendLine($"... 他 {notifications.Count - 10} 件");
                    }

                    AddChatMessage(sb.ToString(), false);
                }

                TransientBorder.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GitHub] エラー: {ex.Message}");
                ShowTransientMessage("GitHub通知の取得に失敗しました", 3000);
            }
        }

        /// <summary>
        /// レビュー待ちPRを表示します
        /// </summary>
        private async Task ShowPullRequestsAwaitingReviewAsync()
        {
            if (_gitHubService == null)
            {
                ShowTransientMessage("GitHubが設定されていません", 2000);
                return;
            }

            try
            {
                ShowTransientMessage("レビュー待ちPRを取得中...", 30000);

                var prs = await _gitHubService.GetPullRequestsAwaitingReviewAsync();

                // チャットを開く
                if (!_isChatOpen)
                {
                    _isChatOpen = true;
                    ChatBalloon.Visibility = Visibility.Visible;
                }

                AddSystemMessage("📝 レビュー待ちPR");

                if (prs.Count == 0)
                {
                    AddSystemMessage("レビュー待ちのPRはありません");
                }
                else
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"**{prs.Count}件のレビュー待ちPR**\n");

                    foreach (var pr in prs.Take(10))
                    {
                        var draftIcon = pr.IsDraft ? "📝" : "✨";
                        sb.AppendLine($"{draftIcon} **#{pr.Number}** {pr.Title}");
                        sb.AppendLine($"  {pr.Repository} by @{pr.Author}");
                        sb.AppendLine($"  📅 {pr.CreatedAt:MM/dd} | 💬 {pr.Comments}");
                        sb.AppendLine();
                    }

                    AddChatMessage(sb.ToString(), false);
                }

                TransientBorder.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GitHub] エラー: {ex.Message}");
                ShowTransientMessage("レビュー待ちPRの取得に失敗しました", 3000);
            }
        }

        /// <summary>
        /// すべてのGitHub通知を既読にします
        /// </summary>
        private async Task MarkAllGitHubNotificationsReadAsync()
        {
            if (_gitHubService == null)
            {
                ShowTransientMessage("GitHubが設定されていません", 2000);
                return;
            }

            try
            {
                await _gitHubService.MarkAllNotificationsAsReadAsync();
                ShowTransientMessage("すべての通知を既読にしました", 2000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GitHub] エラー: {ex.Message}");
                ShowTransientMessage("既読化に失敗しました", 3000);
            }
        }

        #endregion

        #region Status Handlers (Discord/Slack)

        private DiscordStatusService? _discordService;
        private SlackStatusService? _slackService;

        /// <summary>
        /// ステータスボタンクリック - コンテキストメニューを表示
        /// </summary>
        private void OnStatusButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            var contextMenu = new ContextMenu();

            // ステータス表示
            var viewStatusItem = new MenuItem { Header = "👀 現在のステータスを表示" };
            viewStatusItem.Click += async (s, args) => await ShowCurrentStatusAsync();
            contextMenu.Items.Add(viewStatusItem);

            contextMenu.Items.Add(new Separator());

            // ステータス設定メニュー
            var setStatusMenu = new MenuItem { Header = "📝 ステータスを設定" };

            var workingItem = new MenuItem { Header = "💻 作業中" };
            workingItem.Click += async (s, args) => await SetStatusAsync("作業中", ":computer:");

            var meetingItem = new MenuItem { Header = "🗓️ 会議中" };
            meetingItem.Click += async (s, args) => await SetStatusAsync("会議中", ":calendar:");

            var breakItem = new MenuItem { Header = "☕ 休憩中" };
            breakItem.Click += async (s, args) => await SetStatusAsync("休憩中", ":coffee:");

            var focusItem = new MenuItem { Header = "🎯 集中モード" };
            focusItem.Click += async (s, args) => await SetStatusAsync("集中モード - 通知オフ", ":dart:");

            var awayItem = new MenuItem { Header = "🚶 離席中" };
            awayItem.Click += async (s, args) => await SetStatusAsync("離席中", ":walking:");

            setStatusMenu.Items.Add(workingItem);
            setStatusMenu.Items.Add(meetingItem);
            setStatusMenu.Items.Add(breakItem);
            setStatusMenu.Items.Add(focusItem);
            setStatusMenu.Items.Add(awayItem);
            contextMenu.Items.Add(setStatusMenu);

            // カスタムステータス
            var customStatusItem = new MenuItem { Header = "✏️ カスタムステータス..." };
            customStatusItem.Click += OnCustomStatusClick;
            contextMenu.Items.Add(customStatusItem);

            contextMenu.Items.Add(new Separator());

            // ステータスクリア
            var clearStatusItem = new MenuItem { Header = "🗑️ ステータスをクリア" };
            clearStatusItem.Click += async (s, args) => await ClearStatusAsync();
            contextMenu.Items.Add(clearStatusItem);

            contextMenu.Items.Add(new Separator());

            // プレゼンス設定
            var presenceMenu = new MenuItem { Header = "🔵 プレゼンス" };

            var onlineItem = new MenuItem { Header = "🟢 オンライン" };
            onlineItem.Click += async (s, args) => await SetPresenceAsync(OnlineStatus.Online);

            var idleItem = new MenuItem { Header = "🟡 退席中" };
            idleItem.Click += async (s, args) => await SetPresenceAsync(OnlineStatus.Idle);

            var dndItem = new MenuItem { Header = "🔴 取り込み中" };
            dndItem.Click += async (s, args) => await SetPresenceAsync(OnlineStatus.DoNotDisturb);

            var invisibleItem = new MenuItem { Header = "⚫ オフライン表示" };
            invisibleItem.Click += async (s, args) => await SetPresenceAsync(OnlineStatus.Invisible);

            presenceMenu.Items.Add(onlineItem);
            presenceMenu.Items.Add(idleItem);
            presenceMenu.Items.Add(dndItem);
            presenceMenu.Items.Add(invisibleItem);
            contextMenu.Items.Add(presenceMenu);

            contextMenu.PlacementTarget = button;
            contextMenu.Placement = PlacementMode.Bottom;
            contextMenu.IsOpen = true;
        }

        /// <summary>
        /// 現在のステータスを表示します
        /// </summary>
        private async Task ShowCurrentStatusAsync()
        {
            var statuses = new List<string>();

            // Discordステータス
            if (AppSettings.Instance.Discord.IsConfigured)
            {
                _discordService ??= new DiscordStatusService();
                var discordStatus = await _discordService.GetCurrentStatusAsync();
                if (discordStatus != null && !string.IsNullOrEmpty(discordStatus.StatusText))
                {
                    statuses.Add($"**Discord**: {discordStatus.Emoji} {discordStatus.StatusText}");
                }
            }

            // Slackステータス
            if (AppSettings.Instance.Slack.IsConfigured)
            {
                _slackService ??= new SlackStatusService();
                var slackStatus = await _slackService.GetCurrentStatusAsync();
                if (slackStatus != null && !string.IsNullOrEmpty(slackStatus.StatusText))
                {
                    var expireText = slackStatus.ExpiresAt.HasValue
                        ? $" (期限: {slackStatus.ExpiresAt:HH:mm})"
                        : "";
                    statuses.Add($"**Slack**: {slackStatus.Emoji} {slackStatus.StatusText}{expireText}");
                }
            }

            if (statuses.Count == 0)
            {
                ShowTransientMessage("ステータスが設定されていないか、\nサービスが未設定です", 3000);
            }
            else
            {
                // チャットを開く
                if (!_isChatOpen)
                {
                    _isChatOpen = true;
                    ChatBalloon.Visibility = Visibility.Visible;
                }

                AddSystemMessage("📡 現在のステータス");
                AddChatMessage(string.Join("\n", statuses), false);
            }
        }

        /// <summary>
        /// ステータスを設定します
        /// </summary>
        private async Task SetStatusAsync(string statusText, string emoji)
        {
            var results = new List<string>();

            // Discord
            if (AppSettings.Instance.Discord.IsConfigured)
            {
                _discordService ??= new DiscordStatusService();
                var success = await _discordService.SetStatusAsync(statusText, emoji);
                results.Add($"Discord: {(success ? "✅" : "❌")}");
            }

            // Slack
            if (AppSettings.Instance.Slack.IsConfigured)
            {
                _slackService ??= new SlackStatusService();
                var success = await _slackService.SetStatusAsync(statusText, emoji);
                results.Add($"Slack: {(success ? "✅" : "❌")}");
            }

            if (results.Count > 0)
            {
                ShowTransientMessage($"ステータス設定\n{string.Join("\n", results)}", 2000);
            }
            else
            {
                ShowTransientMessage("Discord/Slackが設定されていません", 2000);
            }
        }

        /// <summary>
        /// カスタムステータス入力ダイアログを表示します
        /// </summary>
        private void OnCustomStatusClick(object sender, RoutedEventArgs e)
        {
            // チャットを開いてカスタムステータスの入力を促す
            if (!_isChatOpen)
            {
                _isChatOpen = true;
                ChatBalloon.Visibility = Visibility.Visible;
            }

            AddSystemMessage("📝 カスタムステータス");
            AddChatMessage("カスタムステータスを入力してください。\n例: `status: 集中作業中`\n\nコマンド形式:\n- `status: テキスト` - ステータスを設定\n- `status clear` - ステータスをクリア", false);
        }

        /// <summary>
        /// ステータスをクリアします
        /// </summary>
        private async Task ClearStatusAsync()
        {
            var results = new List<string>();

            // Discord
            if (AppSettings.Instance.Discord.IsConfigured)
            {
                _discordService ??= new DiscordStatusService();
                var success = await _discordService.ClearStatusAsync();
                results.Add($"Discord: {(success ? "✅ クリア" : "❌")}");
            }

            // Slack
            if (AppSettings.Instance.Slack.IsConfigured)
            {
                _slackService ??= new SlackStatusService();
                var success = await _slackService.ClearStatusAsync();
                results.Add($"Slack: {(success ? "✅ クリア" : "❌")}");
            }

            if (results.Count > 0)
            {
                ShowTransientMessage($"ステータスクリア\n{string.Join("\n", results)}", 2000);
            }
            else
            {
                ShowTransientMessage("Discord/Slackが設定されていません", 2000);
            }
        }

        /// <summary>
        /// プレゼンスを設定します
        /// </summary>
        private async Task SetPresenceAsync(OnlineStatus presence)
        {
            var results = new List<string>();

            // Discord (Webhookでは制限あり)
            if (AppSettings.Instance.Discord.IsConfigured)
            {
                _discordService ??= new DiscordStatusService();
                var success = await _discordService.SetPresenceAsync(presence);
                results.Add($"Discord: {(success ? "✅" : "⚠️ Webhook制限")}");
            }

            // Slack
            if (AppSettings.Instance.Slack.IsConfigured)
            {
                _slackService ??= new SlackStatusService();
                var success = await _slackService.SetPresenceAsync(presence);
                results.Add($"Slack: {(success ? "✅" : "❌")}");
            }

            var presenceText = presence switch
            {
                OnlineStatus.Online => "オンライン",
                OnlineStatus.Idle => "退席中",
                OnlineStatus.DoNotDisturb => "取り込み中",
                OnlineStatus.Invisible => "オフライン表示",
                _ => presence.ToString()
            };

            if (results.Count > 0)
            {
                ShowTransientMessage($"プレゼンス: {presenceText}\n{string.Join("\n", results)}", 2000);
            }
            else
            {
                ShowTransientMessage("Discord/Slackが設定されていません", 2000);
            }
        }

        #endregion

        #region Translation Handlers

        /// <summary>
        /// 翻訳ボタンクリック - コンテキストメニューを表示
        /// </summary>
        private void OnTranslateButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            var contextMenu = new ContextMenu();

            var clipboardItem = new MenuItem { Header = "📋 クリップボードを翻訳 (Ctrl+Shift+T)" };
            clipboardItem.Click += async (s, args) => await TranslateClipboardTextAsync();

            var toJapaneseItem = new MenuItem { Header = "🇯🇵 日本語に翻訳" };
            toJapaneseItem.Click += async (s, args) => await TranslateClipboardToLanguageAsync("Japanese");

            var toEnglishItem = new MenuItem { Header = "🇺🇸 英語に翻訳" };
            toEnglishItem.Click += async (s, args) => await TranslateClipboardToLanguageAsync("English");

            var toKoreanItem = new MenuItem { Header = "🇰🇷 韓国語に翻訳" };
            toKoreanItem.Click += async (s, args) => await TranslateClipboardToLanguageAsync("Korean");

            var toChineseItem = new MenuItem { Header = "🇨🇳 中国語に翻訳" };
            toChineseItem.Click += async (s, args) => await TranslateClipboardToLanguageAsync("Chinese");

            contextMenu.Items.Add(clipboardItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(toJapaneseItem);
            contextMenu.Items.Add(toEnglishItem);
            contextMenu.Items.Add(toKoreanItem);
            contextMenu.Items.Add(toChineseItem);

            contextMenu.PlacementTarget = button;
            contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            contextMenu.IsOpen = true;
        }

        /// <summary>
        /// クリップボードのテキストを自動翻訳します（ホットキー用）
        /// </summary>
        private async Task TranslateClipboardTextAsync()
        {
            if (_translationService == null)
            {
                ShowTransientMessage("翻訳サービスが初期化されていません", 2000);
                return;
            }

            try
            {
                // クリップボードからテキストを取得
                var clipboardText = GetClipboardText();
                if (string.IsNullOrWhiteSpace(clipboardText))
                {
                    ShowTransientMessage("クリップボードにテキストがありません", 2000);
                    return;
                }

                ShowTransientMessage("翻訳中...", 30000);

                // 自動翻訳を実行
                var result = await _translationService.AutoTranslateAsync(clipboardText);

                if (result.IsSuccess)
                {
                    // 翻訳結果を表示
                    ShowTranslationResult(result);

                    // 設定に応じてクリップボードにコピー
                    var settings = AppSettings.Instance.Translation;
                    if (settings.CopyToClipboard)
                    {
                        SetClipboardText(result.TranslatedText);
                    }
                }
                else
                {
                    ShowTransientMessage(result.ErrorMessage ?? "翻訳に失敗しました", 3000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Translation] エラー: {ex.Message}");
                ShowTransientMessage("翻訳中にエラーが発生しました", 3000);
            }
        }

        /// <summary>
        /// クリップボードのテキストを指定言語に翻訳します
        /// </summary>
        private async Task TranslateClipboardToLanguageAsync(string targetLanguage)
        {
            if (_translationService == null)
            {
                ShowTransientMessage("翻訳サービスが初期化されていません", 2000);
                return;
            }

            try
            {
                var clipboardText = GetClipboardText();
                if (string.IsNullOrWhiteSpace(clipboardText))
                {
                    ShowTransientMessage("クリップボードにテキストがありません", 2000);
                    return;
                }

                ShowTransientMessage($"{targetLanguage}に翻訳中...", 30000);

                var result = await _translationService.TranslateAsync(clipboardText, targetLanguage);

                if (result.IsSuccess)
                {
                    result.TargetLanguage = targetLanguage;
                    ShowTranslationResult(result);

                    var settings = AppSettings.Instance.Translation;
                    if (settings.CopyToClipboard)
                    {
                        SetClipboardText(result.TranslatedText);
                    }
                }
                else
                {
                    ShowTransientMessage(result.ErrorMessage ?? "翻訳に失敗しました", 3000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Translation] エラー: {ex.Message}");
                ShowTransientMessage("翻訳中にエラーが発生しました", 3000);
            }
        }

        /// <summary>
        /// 翻訳結果をチャットに表示します
        /// </summary>
        private void ShowTranslationResult(TranslationResult result)
        {
            // チャットを開く
            if (!_isChatOpen)
            {
                _isChatOpen = true;
                ChatBalloon.Visibility = Visibility.Visible;

                if (_isClickThrough)
                {
                    _isClickThrough = false;
                    ClickThroughHelper.SetClickThrough(this, false);
                }
            }

            // 結果を構築
            var settings = AppSettings.Instance.Assistant;
            var isDark = settings.IsDarkTheme;

            // ヘッダー
            var langInfo = !string.IsNullOrEmpty(result.DetectedLanguage) && !string.IsNullOrEmpty(result.TargetLanguage)
                ? $"{result.DetectedLanguage} → {result.TargetLanguage}"
                : !string.IsNullOrEmpty(result.TargetLanguage)
                    ? $"→ {result.TargetLanguage}"
                    : "翻訳結果";

            AddSystemMessage($"🌐 翻訳 ({langInfo})");

            // 翻訳結果を表示
            AddTranslationMessage(result.OriginalText, result.TranslatedText, isDark);

            // クリップボードにコピーされた場合の通知
            var translationSettings = AppSettings.Instance.Translation;
            if (translationSettings.CopyToClipboard)
            {
                ShowTransientMessage("翻訳結果をクリップボードにコピーしました", 2000);
            }
            else
            {
                TransientBorder.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 翻訳メッセージをチャットに追加します
        /// </summary>
        private void AddTranslationMessage(string original, string translated, bool isDark)
        {
            var container = new Border
            {
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(10, 8, 10, 8),
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(isDark
                    ? Color.FromRgb(25, 50, 75)
                    : Color.FromRgb(230, 245, 255)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                BorderThickness = new Thickness(1),
                MaxWidth = 310
            };

            var panel = new StackPanel();

            // 元のテキスト（短縮表示）
            var originalLabel = new TextBlock
            {
                FontSize = 10,
                Foreground = new SolidColorBrush(isDark
                    ? Color.FromRgb(180, 180, 180)
                    : Color.FromRgb(100, 100, 100)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 6),
                Text = TruncateText(original, 100)
            };
            panel.Children.Add(originalLabel);

            // セパレータ
            var separator = new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                Opacity = 0.3,
                Margin = new Thickness(0, 0, 0, 6)
            };
            panel.Children.Add(separator);

            // 翻訳結果
            var translatedLabel = new TextBlock
            {
                FontSize = 13,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(isDark
                    ? Color.FromRgb(220, 240, 255)
                    : Color.FromRgb(20, 60, 100)),
                TextWrapping = TextWrapping.Wrap,
                Text = translated
            };
            panel.Children.Add(translatedLabel);

            // コピーボタン
            var copyButton = new Button
            {
                Content = "📋 コピー",
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 8, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 10,
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Left,
                Tag = translated
            };
            copyButton.Click += OnCopyTranslationClick;
            panel.Children.Add(copyButton);

            container.Child = panel;
            ChatMessagesPanel.Children.Add(container);
            ScrollChatToBottom();
        }

        /// <summary>
        /// 翻訳結果コピーボタンクリック
        /// </summary>
        private void OnCopyTranslationClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string text)
            {
                SetClipboardText(text);
                ShowTransientMessage("コピーしました", 1500);
            }
        }

        /// <summary>
        /// クリップボードからテキストを取得します
        /// </summary>
        private static string? GetClipboardText()
        {
            try
            {
                if (System.Windows.Clipboard.ContainsText())
                {
                    return System.Windows.Clipboard.GetText();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Clipboard] 取得エラー: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// クリップボードにテキストを設定します
        /// </summary>
        private static void SetClipboardText(string text)
        {
            try
            {
                System.Windows.Clipboard.SetText(text);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Clipboard] 設定エラー: {ex.Message}");
            }
        }

        #endregion

        #region Consolidated Menu Handlers

        /// <summary>
        /// クイックアクションメニュー（情報系）を表示
        /// </summary>
        private void OnQuickActionsButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            var contextMenu = new ContextMenu();

            // カレンダー
            var calendarMenu = new MenuItem { Header = "📅 カレンダー" };
            var calWeekItem = new MenuItem { Header = "今週の予定" };
            calWeekItem.Click += async (s, args) => await ShowCalendarEventsAsync(CalendarPeriod.ThisWeek);
            var calNextWeekItem = new MenuItem { Header = "来週の予定" };
            calNextWeekItem.Click += async (s, args) => await ShowCalendarEventsAsync(CalendarPeriod.NextWeek);
            var calMonthItem = new MenuItem { Header = "今月の予定" };
            calMonthItem.Click += async (s, args) => await ShowCalendarEventsAsync(CalendarPeriod.ThisMonth);
            calendarMenu.Items.Add(calWeekItem);
            calendarMenu.Items.Add(calNextWeekItem);
            calendarMenu.Items.Add(calMonthItem);
            contextMenu.Items.Add(calendarMenu);

            // 天気
            var weatherItem = new MenuItem { Header = "🌤 天気予報" };
            weatherItem.Click += (s, args) => OnWeatherButtonClick(s, args);
            contextMenu.Items.Add(weatherItem);

            // ファンド
            var fundItem = new MenuItem { Header = "💹 ファンド情報" };
            fundItem.Click += (s, args) => OnFundButtonClick(s, args);
            contextMenu.Items.Add(fundItem);

            contextMenu.Items.Add(new Separator());

            // Gmail
            var gmailItem = new MenuItem { Header = "📧 Gmail" };
            gmailItem.Click += (s, args) => OnGmailButtonClick(s, args);
            contextMenu.Items.Add(gmailItem);

            // 為替
            var currencyItem = new MenuItem { Header = "💱 為替レート" };
            currencyItem.Click += (s, args) => OnCurrencyButtonClick(s, args);
            contextMenu.Items.Add(currencyItem);

            contextMenu.PlacementTarget = button;
            contextMenu.Placement = PlacementMode.Bottom;
            contextMenu.IsOpen = true;
        }

        /// <summary>
        /// ツールメニュー（開発・連携系）を表示
        /// </summary>
        private void OnToolsButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            var contextMenu = new ContextMenu();

            // GitHub
            var githubMenu = new MenuItem { Header = "🐙 GitHub" };
            var ghNotifyItem = new MenuItem { Header = "通知を表示" };
            ghNotifyItem.Click += async (s, args) => await ShowGitHubNotificationsAsync();
            var ghPrItem = new MenuItem { Header = "レビュー待ちPR" };
            ghPrItem.Click += async (s, args) => await ShowPullRequestsAwaitingReviewAsync();
            var ghReadAllItem = new MenuItem { Header = "すべて既読" };
            ghReadAllItem.Click += async (s, args) => await MarkAllGitHubNotificationsReadAsync();
            githubMenu.Items.Add(ghNotifyItem);
            githubMenu.Items.Add(ghPrItem);
            githubMenu.Items.Add(new Separator());
            githubMenu.Items.Add(ghReadAllItem);
            contextMenu.Items.Add(githubMenu);

            // Discord/Slack ステータス
            var statusMenu = new MenuItem { Header = "📡 ステータス (Discord/Slack)" };
            var statusViewItem = new MenuItem { Header = "現在のステータス" };
            statusViewItem.Click += async (s, args) => await ShowCurrentStatusAsync();
            var statusWorkingItem = new MenuItem { Header = "💻 作業中に設定" };
            statusWorkingItem.Click += async (s, args) => await SetStatusAsync("作業中", ":computer:");
            var statusMeetingItem = new MenuItem { Header = "🗓️ 会議中に設定" };
            statusMeetingItem.Click += async (s, args) => await SetStatusAsync("会議中", ":calendar:");
            var statusBreakItem = new MenuItem { Header = "☕ 休憩中に設定" };
            statusBreakItem.Click += async (s, args) => await SetStatusAsync("休憩中", ":coffee:");
            var statusClearItem = new MenuItem { Header = "🗑️ クリア" };
            statusClearItem.Click += async (s, args) => await ClearStatusAsync();
            statusMenu.Items.Add(statusViewItem);
            statusMenu.Items.Add(new Separator());
            statusMenu.Items.Add(statusWorkingItem);
            statusMenu.Items.Add(statusMeetingItem);
            statusMenu.Items.Add(statusBreakItem);
            statusMenu.Items.Add(new Separator());
            statusMenu.Items.Add(statusClearItem);
            contextMenu.Items.Add(statusMenu);

            contextMenu.Items.Add(new Separator());

            // 翻訳
            var translateMenu = new MenuItem { Header = "🌐 翻訳" };
            var transClipItem = new MenuItem { Header = "クリップボードを翻訳 (Ctrl+Shift+T)" };
            transClipItem.Click += async (s, args) => await TranslateClipboardTextAsync();
            var transJpItem = new MenuItem { Header = "🇯🇵 日本語に" };
            transJpItem.Click += async (s, args) => await TranslateClipboardToLanguageAsync("Japanese");
            var transEnItem = new MenuItem { Header = "🇺🇸 英語に" };
            transEnItem.Click += async (s, args) => await TranslateClipboardToLanguageAsync("English");
            var transKoItem = new MenuItem { Header = "🇰🇷 韓国語に" };
            transKoItem.Click += async (s, args) => await TranslateClipboardToLanguageAsync("Korean");
            var transZhItem = new MenuItem { Header = "🇨🇳 中国語に" };
            transZhItem.Click += async (s, args) => await TranslateClipboardToLanguageAsync("Chinese");
            translateMenu.Items.Add(transClipItem);
            translateMenu.Items.Add(new Separator());
            translateMenu.Items.Add(transJpItem);
            translateMenu.Items.Add(transEnItem);
            translateMenu.Items.Add(transKoItem);
            translateMenu.Items.Add(transZhItem);
            contextMenu.Items.Add(translateMenu);

            // Claude使用量
            var claudeItem = new MenuItem { Header = "📊 Claude使用量" };
            claudeItem.Click += async (s, args) => await ShowClaudeUsageInfoAsync();
            contextMenu.Items.Add(claudeItem);

            contextMenu.PlacementTarget = button;
            contextMenu.Placement = PlacementMode.Bottom;
            contextMenu.IsOpen = true;
        }

        /// <summary>
        /// 設定メニューを表示
        /// </summary>
        private void OnSettingsButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            var contextMenu = new ContextMenu();

            // ペット選択
            var petMenu = new MenuItem { Header = "🐾 ペット変更" };
            foreach (PetType petType in Enum.GetValues(typeof(PetType)))
            {
                var petItem = new MenuItem { Header = petType.ToString() };
                var capturedPetType = petType;
                petItem.Click += (s, args) => ChangePet(capturedPetType.ToString());
                petMenu.Items.Add(petItem);
            }
            contextMenu.Items.Add(petMenu);

            // チャットスタイル
            var styleMenu = new MenuItem { Header = "🎨 チャットスタイル" };
            var currentPreset = AppSettings.Instance.ChatBubble.Preset;
            foreach (ChatBubblePreset preset in Enum.GetValues(typeof(ChatBubblePreset)))
            {
                if (preset == ChatBubblePreset.Custom) continue;
                var emoji = GetPresetEmoji(preset);
                var styleItem = new MenuItem
                {
                    Header = $"{emoji} {GetPresetDisplayName(preset)}",
                    IsChecked = currentPreset == preset.ToString(),
                    Tag = preset.ToString()
                };
                styleItem.Click += OnStylePresetSelected;
                styleMenu.Items.Add(styleItem);
            }
            contextMenu.Items.Add(styleMenu);

            // ウィンドウサイズ
            var sizeMenu = new MenuItem { Header = "📐 ウィンドウサイズ" };
            var currentSize = AppSettings.Instance.Assistant.WindowSize;
            var sizes = new[] { ("Small", "小 (180×200)"), ("Medium", "中 (240×267)"), ("Large", "大 (360×400)") };
            foreach (var (sizeKey, sizeLabel) in sizes)
            {
                var sizeItem = new MenuItem
                {
                    Header = sizeLabel,
                    IsChecked = currentSize == sizeKey,
                    Tag = sizeKey
                };
                sizeItem.Click += OnWindowSizeSelected;
                sizeMenu.Items.Add(sizeItem);
            }
            contextMenu.Items.Add(sizeMenu);

            contextMenu.Items.Add(new Separator());

            // チャット履歴クリア
            var clearChatItem = new MenuItem { Header = "🗑️ チャットをクリア" };
            clearChatItem.Click += (s, args) =>
            {
                ChatMessagesPanel.Children.Clear();
                ShowTransientMessage("チャットをクリアしました", 2000);
            };
            contextMenu.Items.Add(clearChatItem);

            contextMenu.Items.Add(new Separator());

            // 設定ウィンドウ
            var settingsWindowItem = new MenuItem { Header = "🔧 設定ウィンドウ" };
            settingsWindowItem.Click += (s, args) => OpenSettingsWindow();
            contextMenu.Items.Add(settingsWindowItem);

            contextMenu.PlacementTarget = button;
            contextMenu.Placement = PlacementMode.Bottom;
            contextMenu.IsOpen = true;
        }

        /// <summary>
        /// ウィンドウサイズが選択されたときの処理
        /// </summary>
        private void OnWindowSizeSelected(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem || menuItem.Tag is not string sizeKey) return;

            try
            {
                // サイズプリセットを取得 (width, height, charSize, chatWidth, chatHeight)
                var (width, height, charSize, chatWidth, chatHeight) = sizeKey switch
                {
                    "Small" => (180.0, 200.0, 160.0, 170.0, 190.0),
                    "Medium" => (240.0, 267.0, 220.0, 230.0, 257.0),
                    "Large" => (360.0, 400.0, 320.0, 340.0, 380.0),
                    _ => (360.0, 400.0, 320.0, 340.0, 380.0)
                };

                // 設定を更新
                AppSettings.Instance.Assistant.WindowSize = sizeKey;
                AppSettings.Instance.Assistant.WindowWidth = width;
                AppSettings.Instance.Assistant.WindowHeight = height;
                AppSettings.Instance.Save();

                // ウィンドウサイズを適用
                this.Width = width;
                this.Height = height;

                // キャラクター表示エリアをリサイズ（GIFは自動的にUniformでスケール）
                CharacterBorder.Width = charSize;
                CharacterBorder.Height = charSize;

                // チャットボックスをリサイズ
                ChatBalloon.Width = chatWidth;
                ChatBalloon.Height = chatHeight;

                ShowTransientMessage($"ウィンドウサイズを変更しました: {width}×{height}", 2000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WindowSize] エラー: {ex.Message}");
                ShowTransientMessage("サイズの変更に失敗しました", 2000);
            }
        }

        #endregion

        #region Quick Action Handlers

        /// <summary>
        /// カレンダーボタンクリック - コンテキストメニューを表示
        /// </summary>
        private void OnCalendarButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            var contextMenu = new ContextMenu();

            var weekItem = new MenuItem { Header = "📅 今週の予定" };
            weekItem.Click += async (s, args) => await ShowCalendarEventsAsync(CalendarPeriod.ThisWeek);

            var nextWeekItem = new MenuItem { Header = "📅 来週の予定" };
            nextWeekItem.Click += async (s, args) => await ShowCalendarEventsAsync(CalendarPeriod.NextWeek);

            var monthItem = new MenuItem { Header = "🗓 今月の予定" };
            monthItem.Click += async (s, args) => await ShowCalendarEventsAsync(CalendarPeriod.ThisMonth);

            var nextMonthItem = new MenuItem { Header = "🗓 来月の予定" };
            nextMonthItem.Click += async (s, args) => await ShowCalendarEventsAsync(CalendarPeriod.NextMonth);

            contextMenu.Items.Add(weekItem);
            contextMenu.Items.Add(nextWeekItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(monthItem);
            contextMenu.Items.Add(nextMonthItem);

            contextMenu.PlacementTarget = button;
            contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            contextMenu.IsOpen = true;
        }

        /// <summary>
        /// カレンダー期間の種類
        /// </summary>
        private enum CalendarPeriod
        {
            ThisWeek,
            NextWeek,
            ThisMonth,
            NextMonth
        }

        /// <summary>
        /// カレンダーイベントを表示します
        /// </summary>
        private async Task ShowCalendarEventsAsync(CalendarPeriod period)
        {
            if (_calendarService == null) return;

            try
            {
                var label = period switch
                {
                    CalendarPeriod.ThisWeek => "今週",
                    CalendarPeriod.NextWeek => "来週",
                    CalendarPeriod.ThisMonth => "今月",
                    CalendarPeriod.NextMonth => "来月",
                    _ => "予定"
                };

                ShowTransientMessage($"{label}のカレンダーを取得中...", 10000);

                // 認証確認
                if (!_calendarService.IsAuthenticated)
                {
                    ShowTransientMessage("Googleアカウントにログイン中...", 10000);
                    var authenticated = await _calendarService.AuthenticateAsync();
                    if (!authenticated)
                    {
                        ShowTransientMessage("Google認証に失敗しました。設定を確認してください。", 3000);
                        return;
                    }
                }

                // イベントを取得
                var events = period switch
                {
                    CalendarPeriod.ThisWeek => await _calendarService.GetWeekEventsAsync(),
                    CalendarPeriod.NextWeek => await _calendarService.GetNextWeekEventsAsync(),
                    CalendarPeriod.ThisMonth => await _calendarService.GetMonthEventsAsync(),
                    CalendarPeriod.NextMonth => await _calendarService.GetNextMonthEventsAsync(),
                    _ => await _calendarService.GetWeekEventsAsync()
                };

                var summary = _calendarService.FormatEventsSummary(events, label);

                // チャットに表示
                OpenChatAndShowMessage(summary);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Calendar] エラー: {ex.Message}");
                ShowTransientMessage("カレンダーの取得に失敗しました", 3000);
            }
        }

        /// <summary>
        /// 天気ボタンクリック
        /// </summary>
        private async void OnWeatherButtonClick(object sender, RoutedEventArgs e)
        {
            if (_weatherService == null) return;

            try
            {
                ShowTransientMessage("天気を取得中...", 10000);

                var current = await _weatherService.GetCurrentWeatherAsync();
                var forecast = await _weatherService.GetWeeklyForecastAsync();
                var summary = _weatherService.FormatWeatherSummary(current, forecast);

                // チャットに表示
                OpenChatAndShowMessage(summary);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Weather] エラー: {ex.Message}");
                ShowTransientMessage("天気の取得に失敗しました", 3000);
            }
        }

        /// <summary>
        /// ファンドボタンクリック
        /// </summary>
        private async void OnFundButtonClick(object sender, RoutedEventArgs e)
        {
            if (_fundService == null) return;

            try
            {
                ShowTransientMessage("ファンド情報を取得中...", 10000);

                var funds = await _fundService.GetAllFundsInfoAsync();
                var summary = _fundService.FormatFundsSummary(funds);

                // チャットに表示
                OpenChatAndShowMessage(summary);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Fund] エラー: {ex.Message}");
                ShowTransientMessage("ファンド情報の取得に失敗しました", 3000);
            }
        }

        /// <summary>
        /// Gmailボタンクリック
        /// </summary>
        private async void OnGmailButtonClick(object sender, RoutedEventArgs e)
        {
            if (_gmailService == null) return;

            try
            {
                ShowTransientMessage("メールを取得中...", 10000);

                // 認証確認
                if (!_gmailService.IsAuthenticated)
                {
                    ShowTransientMessage("Googleアカウントにログイン中...", 10000);
                    var authenticated = await _gmailService.AuthenticateAsync();
                    if (!authenticated)
                    {
                        ShowTransientMessage("Google認証に失敗しました。設定を確認してください。", 3000);
                        return;
                    }
                }

                // 未読メールを取得
                var emails = await _gmailService.GetUnreadEmailsAsync(10);
                _currentEmails = emails;

                // チャットに表示（クリック可能なメールリスト）
                OpenChatAndShowEmailList(emails, "未読メール");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Gmail] エラー: {ex.Message}");
                ShowTransientMessage("メールの取得に失敗しました", 3000);
            }
        }

        /// <summary>
        /// 為替レートボタンクリック
        /// </summary>
        private async void OnCurrencyButtonClick(object sender, RoutedEventArgs e)
        {
            if (_currencyService == null) return;

            try
            {
                ShowTransientMessage("為替レートを取得中...", 10000);

                // JPY, HKD, KRW の為替レートを取得
                var rates = await _currencyService.GetRatesAsync(new[] { "JPY", "HKD", "KRW" });
                var summary = _currencyService.FormatRatesSummary(rates);

                // チャットに表示
                OpenChatAndShowMessage(summary);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Currency] エラー: {ex.Message}");
                ShowTransientMessage("為替レートの取得に失敗しました", 3000);
            }
        }

        /// <summary>
        /// Claude使用量ボタンクリック - メニューを表示
        /// </summary>
        private void OnClaudeUsageButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;
            if (_claudeUsageService == null) return;

            var contextMenu = new ContextMenu();

            var showInfoItem = new MenuItem { Header = "📊 使用量情報を表示" };
            showInfoItem.Click += async (s, args) => await ShowClaudeUsageInfoAsync();

            var openConsoleItem = new MenuItem { Header = "🌐 コンソールを開く" };
            openConsoleItem.Click += (s, args) => _claudeUsageService.OpenConsoleInBrowser();

            contextMenu.Items.Add(showInfoItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(openConsoleItem);

            contextMenu.PlacementTarget = button;
            contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            contextMenu.IsOpen = true;
        }

        /// <summary>
        /// Claude使用量情報を表示します
        /// </summary>
        private async Task ShowClaudeUsageInfoAsync()
        {
            if (_claudeUsageService == null) return;

            try
            {
                ShowTransientMessage("使用量情報を取得中...", 5000);

                var usage = await _claudeUsageService.GetCurrentMonthUsageAsync();
                var summary = _claudeUsageService.FormatUsageSummary(usage);

                // チャットに使用量情報を表示
                OpenChatAndShowUsageInfo(summary);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClaudeUsage] エラー: {ex.Message}");
                ShowTransientMessage("使用量情報の取得に失敗しました", 3000);
            }
        }

        /// <summary>
        /// 使用量情報をチャットに表示します（コンソールを開くボタン付き）
        /// </summary>
        private void OpenChatAndShowUsageInfo(string message)
        {
            // チャットを開く
            if (!_isChatOpen)
            {
                _isChatOpen = true;
                ChatBalloon.Visibility = Visibility.Visible;

                if (_isClickThrough)
                {
                    _isClickThrough = false;
                    ClickThroughHelper.SetClickThrough(this, false);
                }
            }

            // メッセージを追加
            AddSystemMessage(message);

            // コンソールを開くボタンを追加
            AddConsoleButton();

            TransientBorder.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// コンソールを開くボタンをチャットに追加します
        /// </summary>
        private void AddConsoleButton()
        {
            var settings = AppSettings.Instance.Assistant;
            var isDark = settings.IsDarkTheme;

            var button = new Button
            {
                Content = "🌐 Anthropic Consoleを開く",
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 4, 0, 8),
                Background = new SolidColorBrush(Color.FromRgb(210, 105, 30)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            button.Click += (s, e) => _claudeUsageService?.OpenConsoleInBrowser();

            ChatMessagesPanel.Children.Add(button);
            ScrollChatToBottom();
        }

        /// <summary>
        /// チャットを開いてクリック可能なメールリストを表示します
        /// </summary>
        private void OpenChatAndShowEmailList(IReadOnlyList<EmailInfo> emails, string label)
        {
            // チャットを開く
            if (!_isChatOpen)
            {
                _isChatOpen = true;
                ChatBalloon.Visibility = Visibility.Visible;

                // クリックスルーを無効化
                if (_isClickThrough)
                {
                    _isClickThrough = false;
                    ClickThroughHelper.SetClickThrough(this, false);
                }
            }

            // ヘッダーを追加
            AddSystemMessage($"📧 {label} ({emails.Count}件)\nメールをクリックで詳細表示・フラグ変更");

            // 各メールをクリック可能なアイテムとして追加
            foreach (var email in emails)
            {
                AddEmailItem(email);
            }

            // 一時メッセージを消す
            TransientBorder.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// クリック可能なメールアイテムをチャットに追加します
        /// </summary>
        private void AddEmailItem(EmailInfo email)
        {
            var settings = AppSettings.Instance.Assistant;
            var isDark = settings.IsDarkTheme;

            // メインコンテナ
            var container = new Border
            {
                Margin = new Thickness(0, 0, 0, 6),
                Padding = new Thickness(8, 6, 8, 6),
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(isDark
                    ? Color.FromRgb(45, 55, 72)
                    : Color.FromRgb(248, 250, 252)),
                BorderBrush = new SolidColorBrush(email.IsUnread
                    ? Color.FromRgb(59, 130, 246) // 未読: 青
                    : Color.FromRgb(200, 200, 200)), // 既読: グレー
                BorderThickness = new Thickness(email.IsUnread ? 2 : 1),
                Cursor = Cursors.Hand,
                MaxWidth = 300,
                Tag = email.Id
            };

            var mainGrid = new Grid();
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // 左側: メール情報
            var infoPanel = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };

            // 差出人と時刻
            var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
            var unreadMarker = new TextBlock
            {
                Text = email.IsUnread ? "●" : " ",
                Foreground = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0)
            };
            var fromText = new TextBlock
            {
                Text = TruncateText(email.From, 18),
                FontWeight = email.IsUnread ? FontWeights.Bold : FontWeights.Normal,
                Foreground = new SolidColorBrush(isDark ? Colors.White : Colors.Black),
                FontSize = 12
            };
            var timeText = new TextBlock
            {
                Text = email.ReceivedAt.ToString(" HH:mm"),
                Foreground = new SolidColorBrush(isDark ? Colors.LightGray : Colors.Gray),
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center
            };
            headerPanel.Children.Add(unreadMarker);
            headerPanel.Children.Add(fromText);
            headerPanel.Children.Add(timeText);

            // 件名
            var subjectText = new TextBlock
            {
                Text = TruncateText(email.Subject, 30),
                Foreground = new SolidColorBrush(isDark
                    ? Color.FromRgb(200, 220, 255)
                    : Color.FromRgb(30, 64, 175)),
                FontSize = 11,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            infoPanel.Children.Add(headerPanel);
            infoPanel.Children.Add(subjectText);
            Grid.SetColumn(infoPanel, 0);
            mainGrid.Children.Add(infoPanel);

            // 右側: アクションボタン
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Center
            };

            // 既読/未読トグルボタン
            var toggleButton = new Button
            {
                Content = email.IsUnread ? "✓" : "○",
                Width = 24,
                Height = 24,
                Margin = new Thickness(0, 0, 0, 2),
                Background = new SolidColorBrush(email.IsUnread
                    ? Color.FromRgb(34, 197, 94) // 緑: 既読にする
                    : Color.FromRgb(59, 130, 246)), // 青: 未読にする
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand,
                ToolTip = email.IsUnread ? "既読にする" : "未読にする",
                Tag = email.Id
            };
            toggleButton.Click += OnEmailToggleReadClick;

            buttonPanel.Children.Add(toggleButton);
            Grid.SetColumn(buttonPanel, 1);
            mainGrid.Children.Add(buttonPanel);

            container.Child = mainGrid;

            // クリックイベント（詳細表示）
            container.MouseLeftButtonUp += OnEmailItemClick;

            ChatMessagesPanel.Children.Add(container);
            ScrollChatToBottom();
        }

        /// <summary>
        /// メールアイテムクリック（詳細表示）
        /// </summary>
        private async void OnEmailItemClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border border || border.Tag is not string emailId)
                return;

            // ボタンクリックは無視
            if (e.OriginalSource is Button) return;

            if (_gmailService == null) return;

            try
            {
                ShowTransientMessage("メール内容を取得中...", 5000);

                var details = await _gmailService.GetEmailDetailsAsync(emailId);
                if (details == null)
                {
                    ShowTransientMessage("メールの取得に失敗しました", 2000);
                    return;
                }

                // 詳細を表示
                ShowEmailDetails(details);
                TransientBorder.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Gmail] 詳細取得エラー: {ex.Message}");
                ShowTransientMessage("メールの取得に失敗しました", 2000);
            }
        }

        /// <summary>
        /// メール詳細を表示します
        /// </summary>
        private void ShowEmailDetails(EmailInfo email)
        {
            var settings = AppSettings.Instance.Assistant;
            var isDark = settings.IsDarkTheme;

            // 詳細コンテナ
            var container = new Border
            {
                Margin = new Thickness(0, 4, 0, 8),
                Padding = new Thickness(10, 8, 10, 8),
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(isDark
                    ? Color.FromRgb(30, 41, 59)
                    : Color.FromRgb(241, 245, 249)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(99, 102, 241)),
                BorderThickness = new Thickness(1),
                MaxWidth = 310
            };

            var panel = new StackPanel();

            // ヘッダー
            var header = new TextBlock
            {
                Text = "📨 メール詳細",
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(99, 102, 241)),
                Margin = new Thickness(0, 0, 0, 6)
            };
            panel.Children.Add(header);

            // 差出人
            var fromLabel = new TextBlock
            {
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(isDark ? Colors.White : Colors.Black)
            };
            fromLabel.Inlines.Add(new Run("差出人: ") { FontWeight = FontWeights.Bold });
            fromLabel.Inlines.Add(new Run(email.From));
            if (!string.IsNullOrEmpty(email.FromEmail) && email.FromEmail != email.From)
            {
                fromLabel.Inlines.Add(new Run($"\n        <{email.FromEmail}>") { FontSize = 10, Foreground = Brushes.Gray });
            }
            panel.Children.Add(fromLabel);

            // 件名
            var subjectLabel = new TextBlock
            {
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 0),
                Foreground = new SolidColorBrush(isDark ? Colors.White : Colors.Black)
            };
            subjectLabel.Inlines.Add(new Run("件名: ") { FontWeight = FontWeights.Bold });
            subjectLabel.Inlines.Add(new Run(email.Subject));
            panel.Children.Add(subjectLabel);

            // 日時
            var dateLabel = new TextBlock
            {
                Text = $"日時: {email.ReceivedAt:yyyy/MM/dd HH:mm}",
                FontSize = 10,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 2, 0, 6)
            };
            panel.Children.Add(dateLabel);

            // 本文（セパレータ付き）
            var separator = new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                Margin = new Thickness(0, 0, 0, 6)
            };
            panel.Children.Add(separator);

            var bodyText = new TextBlock
            {
                Text = TruncateText(email.Body ?? email.Snippet, 500),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(isDark
                    ? Color.FromRgb(226, 232, 240)
                    : Color.FromRgb(51, 65, 85)),
                LineHeight = 16
            };
            panel.Children.Add(bodyText);

            // アクションボタン
            var actionPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 8, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var toggleBtn = new Button
            {
                Content = email.IsUnread ? "既読にする" : "未読にする",
                Padding = new Thickness(8, 4, 8, 4),
                Background = new SolidColorBrush(email.IsUnread
                    ? Color.FromRgb(34, 197, 94)
                    : Color.FromRgb(59, 130, 246)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 10,
                Cursor = Cursors.Hand,
                Tag = email.Id
            };
            toggleBtn.Click += OnEmailToggleReadClick;
            actionPanel.Children.Add(toggleBtn);

            panel.Children.Add(actionPanel);

            container.Child = panel;
            ChatMessagesPanel.Children.Add(container);
            ScrollChatToBottom();
        }

        /// <summary>
        /// 既読/未読トグルボタンクリック
        /// </summary>
        private async void OnEmailToggleReadClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not string emailId)
                return;

            e.Handled = true; // 親のクリックイベントを止める

            if (_gmailService == null || _currentEmails == null) return;

            // 現在のメール状態を取得
            var email = _currentEmails.FirstOrDefault(m => m.Id == emailId);
            if (email == null) return;

            try
            {
                bool success;
                if (email.IsUnread)
                {
                    success = await _gmailService.MarkAsReadAsync(emailId);
                    if (success)
                    {
                        ShowTransientMessage("既読にしました", 1500);
                    }
                }
                else
                {
                    success = await _gmailService.MarkAsUnreadAsync(emailId);
                    if (success)
                    {
                        ShowTransientMessage("未読にしました", 1500);
                    }
                }

                if (!success)
                {
                    ShowTransientMessage("操作に失敗しました", 2000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Gmail] フラグ変更エラー: {ex.Message}");
                ShowTransientMessage("操作に失敗しました", 2000);
            }
        }

        /// <summary>
        /// テキストを指定長で切り詰めます
        /// </summary>
        private static string TruncateText(string? text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return "(なし)";
            if (text.Length <= maxLength) return text;
            return text[..(maxLength - 1)] + "…";
        }

        /// <summary>
        /// チャットを開いてメッセージを表示します
        /// </summary>
        private void OpenChatAndShowMessage(string message)
        {
            // チャットを開く
            if (!_isChatOpen)
            {
                _isChatOpen = true;
                ChatBalloon.Visibility = Visibility.Visible;

                // クリックスルーを無効化
                if (_isClickThrough)
                {
                    _isClickThrough = false;
                    ClickThroughHelper.SetClickThrough(this, false);
                }
            }

            // メッセージを追加（システムメッセージとして）
            AddSystemMessage(message);

            // 一時メッセージを消す
            TransientBorder.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// システムメッセージをチャットに追加します
        /// </summary>
        private void AddSystemMessage(string message)
        {
            var settings = AppSettings.Instance.Assistant;
            var isDark = settings.IsDarkTheme;

            var messageContainer = new Border
            {
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(10, 6, 10, 6),
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(isDark
                    ? Color.FromRgb(40, 60, 80)
                    : Color.FromRgb(230, 245, 255)),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                MaxWidth = 300
            };

            var textBlock = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(isDark
                    ? Color.FromRgb(220, 240, 255)
                    : Color.FromRgb(20, 40, 60)),
                FontSize = 12,
                FontFamily = new FontFamily("Consolas, MS Gothic, monospace")
            };

            messageContainer.Child = textBlock;
            ChatMessagesPanel.Children.Add(messageContainer);

            ScrollChatToBottom();
        }

        #endregion

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