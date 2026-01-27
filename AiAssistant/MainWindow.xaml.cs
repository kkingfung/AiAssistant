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
using AiAssistant.Companionship;

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
        private PomodoroService? _pomodoroService;
        private MiniGameService? _miniGameService;
        private VoiceInputService? _voiceInputService;
        private VoiceOutputService? _voiceOutputService;
        private ClipboardHistoryService? _clipboardHistoryService;
        private QuickNotesService? _quickNotesService;
        private MediaControlService? _mediaControlService;
        private DailyGoalsService? _dailyGoalsService;
        private ScreenshotOcrService? _screenshotService;
        private IEducationService? _educationService;

        // コンパニオンシップシステム（癒しペット機能）
        private CompanionshipManager? _companionshipManager;

        // Phase 3: キャラクター対話システム
        private Character.ICharacterDialogueService? _dialogueService;
        private System.Timers.Timer? _dialogueDismissTimer;


        // Phase 2: Live2D/音声サービス
        private Live2D.ILive2DService? _live2dService;
        private Voice.IVoiceSynthesisService? _voiceSynthesisService;
        private bool _useLive2DMode = false; // GIF/Live2D切り替えフラグ

        // 音楽プレーヤー
        private Audio.IYouTubePlayerService? _youtubePlayer;
        private Audio.IBgmService? _bgmService;

        // ウィジェットモード
        private bool _isWidgetMode;
        private System.Windows.Threading.DispatcherTimer? _widgetTimer;
        private Size _normalSize;
        private Size _widgetSize = new Size(160, 180);

        // 翻訳用ホットキー
        private const int TRANSLATE_HOTKEY_ID = 0xB002;
        private const uint MOD_SHIFT = 0x0004;

        // 音声読み上げ用ホットキー
        private const int SPEECH_HOTKEY_ID = 0xB003;

        // スクリーンショット用ホットキー
        private const int SCREENSHOT_HOTKEY_ID = 0xB004;

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

            // ポモドーロタイマー
            _pomodoroService = new PomodoroService();
            _pomodoroService.Tick += OnPomodoroTick;
            _pomodoroService.StateChanged += OnPomodoroStateChanged;
            _pomodoroService.PomodoroCompleted += OnPomodoroCompleted;

            // ミニゲームサービス
            _miniGameService = new MiniGameService();

            // 音声入力サービス
            _voiceInputService = new VoiceInputService();
            _voiceInputService.Recognized += OnVoiceRecognized;
            _voiceInputService.StateChanged += OnVoiceInputStateChanged;
            _voiceInputService.Error += OnVoiceError;

            // 音声出力サービス
            _voiceOutputService = new VoiceOutputService();

            // クリップボード履歴サービス
            _clipboardHistoryService = new ClipboardHistoryService();

            // クイックノートサービス
            _quickNotesService = new QuickNotesService();

            // メディアコントロールサービス
            _mediaControlService = new MediaControlService();

            // デイリーゴールサービス
            _dailyGoalsService = new DailyGoalsService();

            // スクリーンショットサービス
            _screenshotService = new ScreenshotOcrService();

            // コンパニオンシップシステム初期化
            InitializeCompanionshipSystem();

            // Phase 2: Live2D/音声サービス初期化（バックグラウンドで）
            _ = InitializeLive2DServiceAsync();
            _ = InitializeVoiceServiceAsync();

            // 音楽プレーヤー初期化
            _ = InitializeMusicPlayerAsync();

            Console.WriteLine("[QuickAction] クイックアクションサービスを初期化しました");
        }

        /// <summary>
        /// コンパニオンシップシステム（癒しペット機能）を初期化します
        /// </summary>
        private void InitializeCompanionshipSystem()
        {
            try
            {
                _companionshipManager = new CompanionshipManager();

                // イベントを購読
                _companionshipManager.BondService.LevelUp += OnBondLevelUp;
                _companionshipManager.BehaviorService.AnimationChanged += OnCompanionAnimationChanged;
                _companionshipManager.EmotionService.EmotionChanged += OnCompanionEmotionChanged;

                // システムを開始
                _companionshipManager.Start();

                // アニメーションコントローラーで感情ベースモードを有効化
                if (_animationController != null)
                {
                    _animationController.EmotionBasedMode = true;
                    _animationController.SetEmotion(_companionshipManager.Context.CurrentEmotionType);
                }

                // Phase 3: キャラクター対話サービスを初期化
                var context = _companionshipManager.Context;
                InitializeDialogueService();

                Console.WriteLine($"[Companionship] 初期化完了 - 絆Lv.{(int)context.BondLevel} ({context.BondPoints}pt), 感情: {context.Emotion.GetJapaneseName()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Companionship] 初期化エラー: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Companionship初期化エラー: {ex}");
            }
        }

        /// <summary>
        /// Phase 3: キャラクター対話サービスを初期化します
        /// </summary>
        private void InitializeDialogueService()
        {
            if (_companionshipManager == null) return;

            try
            {
                _dialogueService = new Character.CharacterDialogueService(
                    _companionshipManager.BondService,
                    _companionshipManager.EmotionService);

                // デフォルトの性格を設定（設定から読み込む場合は後で変更可能）
                _dialogueService.SetPersonality(Character.CharacterPersonality.CreateGentle());

                // イベントを購読
                _dialogueService.DialogueRequested += OnDialogueRequested;
                _dialogueService.DialogueDismissed += OnDialogueDismissed;

                // アイドル対話タイマーを開始（5分間隔）
                _dialogueService.StartIdleDialogueTimer(5);

                // 起動時の挨拶を表示
                _ = _dialogueService.ShowGreetingAsync();

                Console.WriteLine("[Dialogue] 対話サービス初期化完了");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Dialogue] 初期化エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// 対話表示イベントハンドラ
        /// </summary>
        private void OnDialogueRequested(object? sender, Character.DialogueEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // キャラクター名を設定
                DialogueCharacterName.Text = e.CharacterName;

                // セリフを設定
                DialogueText.Text = e.Text;

                // バブルを表示
                DialogueBubble.Visibility = Visibility.Visible;

                // 自動消去タイマーを設定
                if (e.DisplayDurationMs > 0)
                {
                    _dialogueDismissTimer?.Stop();
                    _dialogueDismissTimer?.Dispose();

                    _dialogueDismissTimer = new System.Timers.Timer(e.DisplayDurationMs);
                    _dialogueDismissTimer.Elapsed += (s, args) =>
                    {
                        _dialogueDismissTimer?.Stop();
                        Dispatcher.Invoke(() => DismissDialogueBubble());
                    };
                    _dialogueDismissTimer.AutoReset = false;
                    _dialogueDismissTimer.Start();
                }

                // 音声合成が有効な場合、読み上げ
                if (_voiceSynthesisService?.IsAvailable == true && AppSettings.Instance.Voice.Enabled)
                {
                    _ = _voiceSynthesisService.SpeakAsync(e.Text);
                }

                Console.WriteLine($"[Dialogue] 表示: {e.Text}");
            });
        }

        /// <summary>
        /// 対話終了イベントハンドラ
        /// </summary>
        private void OnDialogueDismissed(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                DismissDialogueBubble();
            });
        }

        /// <summary>
        /// セリフバブルを非表示にする
        /// </summary>
        private void DismissDialogueBubble()
        {
            DialogueBubble.Visibility = Visibility.Collapsed;
            _dialogueDismissTimer?.Stop();
        }

        /// <summary>
        /// セリフバブルクリック時（閉じるか次へ進む）
        /// </summary>
        private void OnDialogueBubbleClick(object sender, MouseButtonEventArgs e)
        {
            // クリックで閉じる
            _dialogueService?.DismissDialogue();
            e.Handled = true;
        }

        /// <summary>
        /// セリフバブル閉じるボタンクリック
        /// </summary>
        private void OnDialogueDismissClick(object sender, RoutedEventArgs e)
        {
            _dialogueService?.DismissDialogue();
        }

        /// <summary>
        /// コンパニオンの感情変化時の処理
        /// </summary>
        private void OnCompanionEmotionChanged(object? sender, EmotionChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // アニメーションコントローラーに感情を反映
                _animationController?.SetEmotion(e.NewEmotion);

                Console.WriteLine($"[Companionship] 感情変化: {e.PreviousEmotion} -> {e.NewEmotion} ({e.Reason})");
            });
        }

        /// <summary>
        /// 絆レベルアップ時の処理
        /// </summary>
        private void OnBondLevelUp(object? sender, BondLevelUpEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var levelInfo = BondLevelInfo.GetInfo(e.NewLevel);
                var message = $"🎉 絆レベルアップ！ Lv.{(int)e.NewLevel} 「{levelInfo.Name}」になりました！";
                ShowTransientMessage(message, 4000);
                Console.WriteLine($"[Companionship] {message}");
            });
        }

        /// <summary>
        /// コンパニオンアニメーション変更時の処理
        /// </summary>
        private void OnCompanionAnimationChanged(object? sender, AnimationChangedEventArgs e)
        {
            // 将来的にCharacterAnimationControllerと連携
            // 現時点ではログ出力のみ
            System.Diagnostics.Debug.WriteLine($"[Companionship] Animation: {e.Animation} (Priority: {e.Priority})");
        }

        /// <summary>
        /// Live2Dサービスを初期化します（Phase 2）
        /// </summary>
        private async Task InitializeLive2DServiceAsync()
        {
            try
            {
                // WebView2コントロールを取得
                var webView = FindName("Live2DWebView") as Microsoft.Web.WebView2.Wpf.WebView2;
                if (webView == null)
                {
                    Console.WriteLine("[Live2D] WebView2コントロールが見つかりません");
                    return;
                }

                _live2dService = new Live2D.WebView2Live2DService(webView);

                // イベントを購読
                _live2dService.ModelLoaded += (s, e) =>
                {
                    Console.WriteLine("[Live2D] モデル読み込み完了");
                };

                _live2dService.ErrorOccurred += (s, e) =>
                {
                    Console.WriteLine($"[Live2D] エラー: {e.Message}");
                };

                // サービスを初期化（モデルはまだ読み込まない）
                await _live2dService.InitializeAsync();

                Console.WriteLine("[Live2D] サービス初期化完了（待機中）");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Live2D] 初期化エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// 音声合成サービスを初期化します（Phase 2）
        /// </summary>
        private async Task InitializeVoiceServiceAsync()
        {
            try
            {
                _voiceSynthesisService = new Voice.VoicevoxSynthesizer();

                // リップシンクイベントをLive2Dに連携
                _voiceSynthesisService.LipSyncUpdate += OnVoiceLipSyncUpdate;
                _voiceSynthesisService.SpeakCompleted += (s, e) =>
                {
                    Console.WriteLine("[Voice] 読み上げ完了");
                };
                _voiceSynthesisService.ErrorOccurred += (s, e) =>
                {
                    Console.WriteLine($"[Voice] エラー: {e.Message}");
                };

                await _voiceSynthesisService.InitializeAsync();

                if (_voiceSynthesisService.IsAvailable)
                {
                    Console.WriteLine($"[Voice] VOICEVOX初期化完了 - {_voiceSynthesisService.AvailableVoices.Count}話者");
                }
                else
                {
                    Console.WriteLine("[Voice] VOICEVOXが利用できません（VOICEVOXを起動してください）");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Voice] 初期化エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// 音楽プレーヤーを初期化します
        /// </summary>
        private async Task InitializeMusicPlayerAsync()
        {
            try
            {
                // BGMサービスを初期化
                _bgmService = new Audio.BgmService();
                Console.WriteLine($"[Music] BGMサービス初期化完了 - {_bgmService.AvailableTracks.Count}曲");

                // YouTubeプレーヤーを初期化
                var youtubeWebView = FindName("YouTubeWebView") as Microsoft.Web.WebView2.Wpf.WebView2;
                if (youtubeWebView != null)
                {
                    _youtubePlayer = new Audio.YouTubePlayerService(youtubeWebView);
                    await _youtubePlayer.InitializeAsync();
                    Console.WriteLine("[Music] YouTubeプレーヤー初期化完了");
                }
                else
                {
                    Console.WriteLine("[Music] YouTubeWebViewが見つかりません");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Music] 初期化エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// 音声のリップシンクをLive2Dに反映
        /// </summary>
        private void OnVoiceLipSyncUpdate(object? sender, Voice.LipSyncEventArgs e)
        {
            if (_live2dService != null && _useLive2DMode)
            {
                _ = _live2dService.SetLipSyncAsync(e.Volume);
            }
        }

        /// <summary>
        /// Live2DモードとGIFモードを切り替え
        /// </summary>
        /// <param name="useLive2D">true=Live2D、false=GIF</param>
        private async Task SetCharacterModeAsync(bool useLive2D)
        {
            _useLive2DMode = useLive2D;

            await Dispatcher.InvokeAsync(() =>
            {
                var webView = FindName("Live2DWebView") as Microsoft.Web.WebView2.Wpf.WebView2;
                var avatarImage = FindName("AvatarImage") as System.Windows.Controls.Image;

                if (useLive2D)
                {
                    // Live2Dモード
                    if (webView != null) webView.Visibility = Visibility.Visible;
                    if (avatarImage != null) avatarImage.Visibility = Visibility.Collapsed;

                    // アニメーションコントローラーを停止
                    _animationController?.Stop();

                    Console.WriteLine("[Character] Live2Dモードに切り替え");
                }
                else
                {
                    // GIFモード
                    if (webView != null) webView.Visibility = Visibility.Collapsed;
                    if (avatarImage != null) avatarImage.Visibility = Visibility.Visible;

                    // アニメーションコントローラーを再開
                    _animationController?.Start();

                    Console.WriteLine("[Character] GIFモードに切り替え");
                }
            });
        }

        /// <summary>
        /// Live2Dモデルを読み込み
        /// </summary>
        /// <param name="modelPath">モデルファイルのパス（.model3.json）</param>
        public async Task LoadLive2DModelAsync(string modelPath)
        {
            if (_live2dService == null)
            {
                Console.WriteLine("[Live2D] サービスが初期化されていません");
                return;
            }

            await _live2dService.LoadModelAsync(modelPath);
            await SetCharacterModeAsync(true);
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

                // 教育サービスも初期化
                _educationService = new EducationService(_viewModel.AiService);
                Console.WriteLine("[Education] 教育サービスを初期化しました");
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

            // 音声読み上げ用ホットキー Ctrl+Shift+S を登録
            var vkS = (uint)KeyInterop.VirtualKeyFromKey(Key.S);
            _ = RegisterHotKey(hwnd, SPEECH_HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, vkS);

            // スクリーンショット用ホットキー Ctrl+Shift+P を登録
            var vkP = (uint)KeyInterop.VirtualKeyFromKey(Key.P);
            _ = RegisterHotKey(hwnd, SCREENSHOT_HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, vkP);
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            if (_hwndSource != null)
                _hwndSource.RemoveHook(WndProc);

            var helper = new WindowInteropHelper(this);
            UnregisterHotKey(helper.Handle, HOTKEY_ID);
            UnregisterHotKey(helper.Handle, TRANSLATE_HOTKEY_ID);
            UnregisterHotKey(helper.Handle, SPEECH_HOTKEY_ID);
            UnregisterHotKey(helper.Handle, SCREENSHOT_HOTKEY_ID);

            // アニメーションコントローラーをクリーンアップ
            _animationController?.Dispose();

            // クイックアクションサービスをクリーンアップ
            (_calendarService as IDisposable)?.Dispose();
            (_weatherService as IDisposable)?.Dispose();
            (_fundService as IDisposable)?.Dispose();
            (_gmailService as IDisposable)?.Dispose();
            (_currencyService as IDisposable)?.Dispose();
            _claudeUsageService?.Dispose();

            // コンパニオンシップシステムをクリーンアップ
            _companionshipManager?.Dispose();

            // Phase 3: 対話サービスをクリーンアップ
            _dialogueDismissTimer?.Stop();
            _dialogueDismissTimer?.Dispose();
            _dialogueService?.Dispose();

            // Phase 2: Live2D/音声サービスをクリーンアップ
            _live2dService?.Dispose();
            _voiceSynthesisService?.Dispose();

            // 音楽サービスをクリーンアップ
            _youtubePlayer?.Dispose();
            _bgmService?.Dispose();
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
                else if (hotkeyId == SPEECH_HOTKEY_ID)
                {
                    SpeakClipboardText();
                    handled = true;
                }
                else if (hotkeyId == SCREENSHOT_HOTKEY_ID)
                {
                    CaptureScreenshotWithHotkey();
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

            // コンパニオンシップ：ユーザーとの交流を記録
            _companionshipManager?.RecordInteraction();

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
                    // 最新の応答を保存（音声読み上げ用）
                    _lastAssistantResponse = _viewModel.ResponseText;
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

            contextMenu.Items.Add(new Separator());

            // ポモドーロタイマー
            var pomodoroItem = new MenuItem { Header = "🍅 ポモドーロタイマー" };
            pomodoroItem.Click += (s, args) => ShowPomodoroMenu(button);
            contextMenu.Items.Add(pomodoroItem);

            // デイリーゴール
            var goalsItem = new MenuItem { Header = "🎯 今日の目標" };
            goalsItem.Click += (s, args) => ShowDailyGoalsMenu(button);
            contextMenu.Items.Add(goalsItem);

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

            contextMenu.Items.Add(new Separator());

            // 音声入力
            var voiceItem = new MenuItem { Header = "🎤 音声入力" };
            voiceItem.Click += (s, args) => StartVoiceInput();
            if (_voiceInputService == null || !_voiceInputService.IsAvailable)
            {
                voiceItem.IsEnabled = false;
                voiceItem.Header = "🎤 音声入力 (利用不可)";
            }
            contextMenu.Items.Add(voiceItem);

            // 音声出力（読み上げ）
            var voiceOutputMenu = new MenuItem { Header = "🔊 音声読み上げ" };
            if (_voiceOutputService != null && _voiceOutputService.IsAvailable)
            {
                var speakClipboardItem = new MenuItem { Header = "📋 選択テキストを読み上げ (Ctrl+Shift+S)" };
                speakClipboardItem.Click += (s, args) => SpeakClipboardText();
                voiceOutputMenu.Items.Add(speakClipboardItem);

                var speakLastItem = new MenuItem { Header = "📢 最新の返答を読み上げ" };
                speakLastItem.Click += (s, args) => SpeakLastResponse();
                voiceOutputMenu.Items.Add(speakLastItem);

                var stopSpeakItem = new MenuItem { Header = "⏹️ 読み上げ停止" };
                stopSpeakItem.Click += (s, args) => _voiceOutputService?.Stop();
                voiceOutputMenu.Items.Add(stopSpeakItem);

                voiceOutputMenu.Items.Add(new Separator());

                // 速度設定
                var speedMenu = new MenuItem { Header = "⚡ 速度" };
                var speeds = new[] { (-5, "遅い"), (0, "普通"), (3, "速い"), (6, "とても速い") };
                foreach (var (rate, label) in speeds)
                {
                    var speedItem = new MenuItem
                    {
                        Header = label,
                        IsChecked = _voiceOutputService.Rate == rate
                    };
                    var r = rate;
                    speedItem.Click += (s, args) =>
                    {
                        _voiceOutputService.Rate = r;
                        ShowTransientMessage($"読み上げ速度: {label}", 1500);
                    };
                    speedMenu.Items.Add(speedItem);
                }
                voiceOutputMenu.Items.Add(speedMenu);

                // テスト読み上げ
                var testItem = new MenuItem { Header = "🧪 テスト読み上げ" };
                testItem.Click += (s, args) =>
                {
                    _voiceOutputService.SpeakAsync("こんにちは！音声出力のテストです。");
                };
                voiceOutputMenu.Items.Add(testItem);
            }
            else
            {
                voiceOutputMenu.IsEnabled = false;
                voiceOutputMenu.Header = "🔊 音声読み上げ (利用不可)";
            }
            contextMenu.Items.Add(voiceOutputMenu);

            // ミニゲーム
            var gameItem = new MenuItem { Header = "🎮 じゃんけん" };
            gameItem.Click += (s, args) => ShowMiniGameMenu(button);
            contextMenu.Items.Add(gameItem);

            contextMenu.Items.Add(new Separator());

            // クリップボード履歴
            var clipboardItem = new MenuItem { Header = "📋 クリップボード履歴" };
            clipboardItem.Click += (s, args) => ShowClipboardHistoryMenu(button);
            contextMenu.Items.Add(clipboardItem);

            // クイックノート
            var notesItem = new MenuItem { Header = "📝 クイックノート" };
            notesItem.Click += (s, args) => ShowQuickNotesMenu(button);
            contextMenu.Items.Add(notesItem);

            // メディアコントロール
            var mediaMenu = new MenuItem { Header = "🎵 メディア操作" };
            var playPauseItem = new MenuItem { Header = "⏯️ 再生/一時停止" };
            playPauseItem.Click += (s, args) => { _mediaControlService?.PlayPause(); ShowTransientMessage("⏯️ Play/Pause", 1000); };
            var nextItem = new MenuItem { Header = "⏭️ 次の曲" };
            nextItem.Click += (s, args) => { _mediaControlService?.NextTrack(); ShowTransientMessage("⏭️ Next", 1000); };
            var prevItem = new MenuItem { Header = "⏮️ 前の曲" };
            prevItem.Click += (s, args) => { _mediaControlService?.PreviousTrack(); ShowTransientMessage("⏮️ Previous", 1000); };
            var volUpItem = new MenuItem { Header = "🔊 音量+" };
            volUpItem.Click += (s, args) => { _mediaControlService?.VolumeUp(); };
            var volDownItem = new MenuItem { Header = "🔉 音量-" };
            volDownItem.Click += (s, args) => { _mediaControlService?.VolumeDown(); };
            var muteItem = new MenuItem { Header = "🔇 ミュート" };
            muteItem.Click += (s, args) => { _mediaControlService?.ToggleMute(); ShowTransientMessage("🔇 Mute Toggle", 1000); };
            mediaMenu.Items.Add(playPauseItem);
            mediaMenu.Items.Add(nextItem);
            mediaMenu.Items.Add(prevItem);
            mediaMenu.Items.Add(new Separator());
            mediaMenu.Items.Add(volUpItem);
            mediaMenu.Items.Add(volDownItem);
            mediaMenu.Items.Add(muteItem);
            contextMenu.Items.Add(mediaMenu);

            // スクリーンショット
            var screenshotMenu = new MenuItem { Header = "📸 スクリーンショット" };
            var captureFullItem = new MenuItem { Header = "🖥️ 全画面" };
            captureFullItem.Click += (s, args) => CaptureScreenshot(false);
            var captureWindowItem = new MenuItem { Header = "🪟 アクティブウィンドウ" };
            captureWindowItem.Click += (s, args) => CaptureScreenshot(true);
            screenshotMenu.Items.Add(captureFullItem);
            screenshotMenu.Items.Add(captureWindowItem);
            contextMenu.Items.Add(screenshotMenu);

            contextMenu.PlacementTarget = button;
            contextMenu.Placement = PlacementMode.Bottom;
            contextMenu.IsOpen = true;
        }

        /// <summary>
        /// 教育機能メニュー（言語・プログラミング学習）を表示
        /// </summary>
        private void OnEducationButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            var contextMenu = new ContextMenu();

            // 言語学習セクション
            var languageMenu = new MenuItem { Header = "🌍 言語学習" };

            // 英語
            var englishMenu = new MenuItem { Header = "🇺🇸 English" };
            var engGrammarItem = new MenuItem { Header = "📝 文法チェック" };
            engGrammarItem.Click += async (s, args) => await ShowGrammarCheckDialogAsync(SupportedLanguage.English);
            var engWordItem = new MenuItem { Header = "📖 単語・フレーズ解説" };
            engWordItem.Click += async (s, args) => await ShowWordExplanationDialogAsync(SupportedLanguage.English);
            var engPracticeItem = new MenuItem { Header = "💬 会話練習" };
            engPracticeItem.Click += (s, args) => StartConversationPracticeAsync(SupportedLanguage.English);
            var engRandomItem = new MenuItem { Header = "🎲 ランダム単語学習" };
            engRandomItem.Click += async (s, args) => await ShowRandomWordAsync(SupportedLanguage.English);
            englishMenu.Items.Add(engGrammarItem);
            englishMenu.Items.Add(engWordItem);
            englishMenu.Items.Add(engPracticeItem);
            englishMenu.Items.Add(new Separator());
            englishMenu.Items.Add(engRandomItem);
            languageMenu.Items.Add(englishMenu);

            // 日本語
            var japaneseMenu = new MenuItem { Header = "🇯🇵 日本語" };
            var jpGrammarItem = new MenuItem { Header = "📝 文法チェック" };
            jpGrammarItem.Click += async (s, args) => await ShowGrammarCheckDialogAsync(SupportedLanguage.Japanese);
            var jpWordItem = new MenuItem { Header = "📖 単語・フレーズ解説" };
            jpWordItem.Click += async (s, args) => await ShowWordExplanationDialogAsync(SupportedLanguage.Japanese);
            var jpPracticeItem = new MenuItem { Header = "💬 会話練習" };
            jpPracticeItem.Click += (s, args) => StartConversationPracticeAsync(SupportedLanguage.Japanese);
            var jpRandomItem = new MenuItem { Header = "🎲 ランダム単語学習" };
            jpRandomItem.Click += async (s, args) => await ShowRandomWordAsync(SupportedLanguage.Japanese);
            japaneseMenu.Items.Add(jpGrammarItem);
            japaneseMenu.Items.Add(jpWordItem);
            japaneseMenu.Items.Add(jpPracticeItem);
            japaneseMenu.Items.Add(new Separator());
            japaneseMenu.Items.Add(jpRandomItem);
            languageMenu.Items.Add(japaneseMenu);

            // 韓国語
            var koreanMenu = new MenuItem { Header = "🇰🇷 한국어" };
            var koGrammarItem = new MenuItem { Header = "📝 文法チェック" };
            koGrammarItem.Click += async (s, args) => await ShowGrammarCheckDialogAsync(SupportedLanguage.Korean);
            var koWordItem = new MenuItem { Header = "📖 単語・フレーズ解説" };
            koWordItem.Click += async (s, args) => await ShowWordExplanationDialogAsync(SupportedLanguage.Korean);
            var koPracticeItem = new MenuItem { Header = "💬 会話練習" };
            koPracticeItem.Click += (s, args) => StartConversationPracticeAsync(SupportedLanguage.Korean);
            var koRandomItem = new MenuItem { Header = "🎲 ランダム単語学習" };
            koRandomItem.Click += async (s, args) => await ShowRandomWordAsync(SupportedLanguage.Korean);
            koreanMenu.Items.Add(koGrammarItem);
            koreanMenu.Items.Add(koWordItem);
            koreanMenu.Items.Add(koPracticeItem);
            koreanMenu.Items.Add(new Separator());
            koreanMenu.Items.Add(koRandomItem);
            languageMenu.Items.Add(koreanMenu);

            contextMenu.Items.Add(languageMenu);
            contextMenu.Items.Add(new Separator());

            // プログラミング学習セクション
            var programmingMenu = new MenuItem { Header = "💻 プログラミング" };

            // コードレビュー
            var codeReviewMenu = new MenuItem { Header = "🔍 コードレビュー" };
            AddProgrammingLanguageItems(codeReviewMenu, "review");
            programmingMenu.Items.Add(codeReviewMenu);

            // エラー解説
            var debugHelpMenu = new MenuItem { Header = "🐛 エラー解説・デバッグ" };
            AddProgrammingLanguageItems(debugHelpMenu, "debug");
            programmingMenu.Items.Add(debugHelpMenu);

            // 概念説明
            var conceptMenu = new MenuItem { Header = "📚 概念・パターン解説" };
            AddProgrammingLanguageItems(conceptMenu, "concept");
            programmingMenu.Items.Add(conceptMenu);

            // コード改善
            var improveMenu = new MenuItem { Header = "✨ コード改善提案" };
            AddProgrammingLanguageItems(improveMenu, "improve");
            programmingMenu.Items.Add(improveMenu);

            programmingMenu.Items.Add(new Separator());

            // ランダムAPI学習
            var randomApiMenu = new MenuItem { Header = "🎲 ランダムAPI学習" };
            AddProgrammingLanguageItems(randomApiMenu, "randomApi");
            programmingMenu.Items.Add(randomApiMenu);

            // ランダム概念学習
            var randomConceptMenu = new MenuItem { Header = "🎯 ランダム概念学習" };
            AddProgrammingLanguageItems(randomConceptMenu, "randomConcept");
            programmingMenu.Items.Add(randomConceptMenu);

            contextMenu.Items.Add(programmingMenu);
            contextMenu.Items.Add(new Separator());

            // クリップボードのコードを操作
            var clipboardMenu = new MenuItem { Header = "📋 クリップボードのコード" };
            var clipReviewItem = new MenuItem { Header = "🔍 レビュー" };
            clipReviewItem.Click += async (s, args) => await ReviewClipboardCodeAsync();
            var clipImproveItem = new MenuItem { Header = "✨ 改善提案" };
            clipImproveItem.Click += async (s, args) => await ImproveClipboardCodeAsync();
            clipboardMenu.Items.Add(clipReviewItem);
            clipboardMenu.Items.Add(clipImproveItem);
            contextMenu.Items.Add(clipboardMenu);

            contextMenu.PlacementTarget = button;
            contextMenu.Placement = PlacementMode.Bottom;
            contextMenu.IsOpen = true;
        }

        /// <summary>
        /// プログラミング言語メニュー項目を追加します
        /// </summary>
        private void AddProgrammingLanguageItems(MenuItem parentMenu, string action)
        {
            var languages = new[]
            {
                (ProgrammingLanguage.CSharp, "💻 C#"),
                (ProgrammingLanguage.Python, "🐍 Python"),
                (ProgrammingLanguage.JavaScript, "📜 JavaScript"),
                (ProgrammingLanguage.TypeScript, "📘 TypeScript"),
                (ProgrammingLanguage.Java, "☕ Java"),
                (ProgrammingLanguage.Rust, "🦀 Rust"),
                (ProgrammingLanguage.Go, "🐹 Go"),
                (ProgrammingLanguage.Ruby, "💎 Ruby"),
                (ProgrammingLanguage.PHP, "🐘 PHP"),
                (ProgrammingLanguage.Cpp, "⚡ C++"),
                (ProgrammingLanguage.Swift, "🎯 Swift"),
                (ProgrammingLanguage.Kotlin, "🎯 Kotlin")
            };

            foreach (var (lang, label) in languages)
            {
                var item = new MenuItem { Header = label };
                var capturedLang = lang;
                item.Click += async (s, args) =>
                {
                    switch (action)
                    {
                        case "review":
                            await ShowCodeReviewDialogAsync(capturedLang);
                            break;
                        case "debug":
                            await ShowDebugHelpDialogAsync(capturedLang);
                            break;
                        case "concept":
                            await ShowConceptExplanationDialogAsync(capturedLang);
                            break;
                        case "improve":
                            await ShowCodeImprovementDialogAsync(capturedLang);
                            break;
                        case "randomApi":
                            await ShowRandomApiAsync(capturedLang);
                            break;
                        case "randomConcept":
                            await ShowRandomConceptAsync(capturedLang);
                            break;
                    }
                };
                parentMenu.Items.Add(item);
            }
        }

        #region Education Service Handlers

        /// <summary>
        /// 文法チェックダイアログを表示します
        /// </summary>
        private async Task ShowGrammarCheckDialogAsync(SupportedLanguage language)
        {
            if (_educationService == null)
            {
                ShowTransientMessage("教育サービスが初期化されていません", 2000);
                return;
            }

            var langName = language switch
            {
                SupportedLanguage.English => "英語",
                SupportedLanguage.Japanese => "日本語",
                SupportedLanguage.Korean => "韓国語",
                _ => "英語"
            };

            var text = InputDialog.Show($"{langName}の文法チェック", "チェックするテキストを入力してください:", "", this);
            if (string.IsNullOrWhiteSpace(text)) return;

            ShowTransientMessage($"📝 {langName}の文法をチェックしています...", 3000);

            try
            {
                var result = await _educationService.CheckGrammarAsync(text, language);
                if (result.IsSuccess)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"📝 {langName}文法チェック結果\n");
                    sb.AppendLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                    sb.AppendLine($"\n【元のテキスト】\n{result.OriginalText}");
                    sb.AppendLine($"\n【修正後】\n{result.CorrectedText}");

                    if (result.HasIssues)
                    {
                        sb.AppendLine("\n【問題点】");
                        foreach (var issue in result.Issues)
                        {
                            sb.AppendLine($"\n  ❌ {issue.Original}");
                            sb.AppendLine($"  ✅ {issue.Correction}");
                            sb.AppendLine($"  💡 {issue.Explanation}");
                        }
                    }
                    else
                    {
                        sb.AppendLine("\n✅ 文法に問題はありません！");
                    }

                    var content = sb.ToString();
                    LearningResultDialog.Show(
                        $"📝 {langName}文法チェック",
                        content,
                        (note) => _quickNotesService?.AddNote(note),
                        this);
                }
                else
                {
                    ShowTransientMessage($"❌ エラー: {result.ErrorMessage}", 3000);
                }
            }
            catch (Exception ex)
            {
                ShowTransientMessage($"❌ エラーが発生しました: {ex.Message}", 3000);
            }
        }

        /// <summary>
        /// 単語説明ダイアログを表示します
        /// </summary>
        private async Task ShowWordExplanationDialogAsync(SupportedLanguage language)
        {
            if (_educationService == null)
            {
                ShowTransientMessage("教育サービスが初期化されていません", 2000);
                return;
            }

            var langName = language switch
            {
                SupportedLanguage.English => "英語",
                SupportedLanguage.Japanese => "日本語",
                SupportedLanguage.Korean => "韓国語",
                _ => "英語"
            };

            var word = InputDialog.Show($"{langName}の単語・フレーズ解説", "解説してほしい単語やフレーズを入力:", "", this);
            if (string.IsNullOrWhiteSpace(word)) return;

            ShowTransientMessage($"📖 「{word}」を調べています...", 3000);

            try
            {
                var result = await _educationService.ExplainWordAsync(word, language);
                if (result.IsSuccess)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"📖 「{result.Word}」の解説\n");
                    sb.AppendLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                    sb.AppendLine($"\n【意味】 {result.Definition}");

                    if (!string.IsNullOrEmpty(result.Pronunciation))
                    {
                        sb.AppendLine($"【発音】 {result.Pronunciation}");
                    }

                    if (result.ExampleSentences.Count > 0)
                    {
                        sb.AppendLine("\n【例文】");
                        foreach (var example in result.ExampleSentences)
                        {
                            sb.AppendLine($"  • {example}");
                        }
                    }

                    if (result.Synonyms.Count > 0)
                    {
                        sb.AppendLine($"\n【類義語】 {string.Join(", ", result.Synonyms)}");
                    }

                    var content = sb.ToString();
                    LearningResultDialog.Show(
                        $"📖 「{result.Word}」",
                        content,
                        (note) => _quickNotesService?.AddNote(note),
                        this);
                }
                else
                {
                    ShowTransientMessage($"❌ エラー: {result.ErrorMessage}", 3000);
                }
            }
            catch (Exception ex)
            {
                ShowTransientMessage($"❌ エラーが発生しました: {ex.Message}", 3000);
            }
        }

        /// <summary>
        /// 会話練習を開始します
        /// </summary>
        private void StartConversationPracticeAsync(SupportedLanguage language)
        {
            if (_educationService == null)
            {
                ShowTransientMessage("教育サービスが初期化されていません", 2000);
                return;
            }

            var langName = language switch
            {
                SupportedLanguage.English => "英語",
                SupportedLanguage.Japanese => "日本語",
                SupportedLanguage.Korean => "韓国語",
                _ => "英語"
            };

            var sb = new StringBuilder();
            sb.AppendLine($"💬 **{langName}会話練習モード**\n");
            sb.AppendLine($"{langName}で話しかけてください。間違いがあれば優しく修正します。\n");
            sb.AppendLine("終了するには「終了」または「exit」と入力してください。");

            OpenChatAndShowMessage(sb.ToString());

            // 会話練習は通常のチャットで行う（特別なモード設定なし）
            ShowTransientMessage($"{langName}会話練習を開始しました", 2000);
        }

        /// <summary>
        /// コードレビューダイアログを表示します
        /// </summary>
        private async Task ShowCodeReviewDialogAsync(ProgrammingLanguage language)
        {
            if (_educationService == null)
            {
                ShowTransientMessage("教育サービスが初期化されていません", 2000);
                return;
            }

            var langName = GetProgrammingLanguageDisplayName(language);
            var code = CodeInputDialog.Show($"{langName}コードレビュー", "レビューするコードを貼り付けてください:", "", this);
            if (string.IsNullOrWhiteSpace(code)) return;

            ShowTransientMessage($"🔍 {langName}コードをレビューしています...", 3000);

            try
            {
                var result = await _educationService.ReviewCodeAsync(code, language);
                if (result.IsSuccess)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"🔍 {langName}コードレビュー結果\n");
                    sb.AppendLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                    sb.AppendLine($"\n【総合スコア】 {result.OverallScore}/10");
                    sb.AppendLine($"【概要】 {result.Summary}");

                    if (result.Items.Count > 0)
                    {
                        sb.AppendLine("\n【詳細】");
                        foreach (var item in result.Items)
                        {
                            var severityEmoji = item.Severity switch
                            {
                                "Critical" => "🔴",
                                "Warning" => "🟡",
                                _ => "🔵"
                            };
                            sb.AppendLine($"\n{severityEmoji} [{item.Category}] {item.Description}");
                            if (!string.IsNullOrEmpty(item.Suggestion))
                            {
                                sb.AppendLine($"   💡 {item.Suggestion}");
                            }
                        }
                    }
                    else
                    {
                        sb.AppendLine("\n✅ 問題は見つかりませんでした！");
                    }

                    var content = sb.ToString();
                    LearningResultDialog.Show(
                        $"🔍 {langName}コードレビュー",
                        content,
                        (note) => _quickNotesService?.AddNote(note),
                        this);
                }
                else
                {
                    ShowTransientMessage($"❌ エラー: {result.ErrorMessage}", 3000);
                }
            }
            catch (Exception ex)
            {
                ShowTransientMessage($"❌ エラーが発生しました: {ex.Message}", 3000);
            }
        }

        /// <summary>
        /// デバッグヘルプダイアログを表示します
        /// </summary>
        private async Task ShowDebugHelpDialogAsync(ProgrammingLanguage language)
        {
            if (_educationService == null)
            {
                ShowTransientMessage("教育サービスが初期化されていません", 2000);
                return;
            }

            var langName = GetProgrammingLanguageDisplayName(language);
            var errorMsg = CodeInputDialog.Show($"{langName}エラー解説", "エラーメッセージを入力してください:", "", this);
            if (string.IsNullOrWhiteSpace(errorMsg)) return;

            var code = CodeInputDialog.Show("コード（任意）", "関連するコードがあれば貼り付けてください（なければ空のまま）:", "", this);

            ShowTransientMessage($"🐛 エラーを分析しています...", 3000);

            try
            {
                var result = await _educationService.ExplainErrorAsync(code ?? "", errorMsg, language);
                if (result.IsSuccess)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"🐛 {langName}エラー解説\n");
                    sb.AppendLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                    sb.AppendLine($"\n【エラー】 {result.ErrorMessage}");
                    sb.AppendLine($"\n【説明】\n{result.Explanation}");
                    sb.AppendLine($"\n【原因】\n{result.PossibleCause}");
                    sb.AppendLine($"\n【解決方法】\n{result.SuggestedFix}");

                    if (!string.IsNullOrEmpty(result.FixedCode))
                    {
                        sb.AppendLine($"\n【修正コード】\n{result.FixedCode}");
                    }

                    var content = sb.ToString();
                    LearningResultDialog.Show(
                        $"🐛 {langName}エラー解説",
                        content,
                        (note) => _quickNotesService?.AddNote(note),
                        this);
                }
                else
                {
                    ShowTransientMessage($"❌ エラー: {result.Error}", 3000);
                }
            }
            catch (Exception ex)
            {
                ShowTransientMessage($"❌ エラーが発生しました: {ex.Message}", 3000);
            }
        }

        /// <summary>
        /// 概念説明ダイアログを表示します
        /// </summary>
        private async Task ShowConceptExplanationDialogAsync(ProgrammingLanguage language)
        {
            if (_educationService == null)
            {
                ShowTransientMessage("教育サービスが初期化されていません", 2000);
                return;
            }

            var langName = GetProgrammingLanguageDisplayName(language);
            var topic = InputDialog.Show($"{langName}概念解説", "学びたい概念やパターンを入力してください\n（例: async/await, SOLID, デザインパターン）:", "", this);
            if (string.IsNullOrWhiteSpace(topic)) return;

            ShowTransientMessage($"📚 「{topic}」について調べています...", 3000);

            try
            {
                var result = await _educationService.ExplainConceptAsync(topic, language);
                if (result.IsSuccess)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"📚 {topic} ({langName})\n");
                    sb.AppendLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                    sb.AppendLine($"\n【説明】\n{result.Explanation}");

                    if (result.KeyPoints.Count > 0)
                    {
                        sb.AppendLine("\n【ポイント】");
                        foreach (var point in result.KeyPoints)
                        {
                            sb.AppendLine($"  • {point}");
                        }
                    }

                    if (!string.IsNullOrEmpty(result.CodeExample))
                    {
                        sb.AppendLine($"\n【コード例】\n{result.CodeExample}");
                    }

                    if (result.RelatedTopics.Count > 0)
                    {
                        sb.AppendLine($"\n【関連トピック】 {string.Join(", ", result.RelatedTopics)}");
                    }

                    var content = sb.ToString();
                    LearningResultDialog.Show(
                        $"📚 {topic}",
                        content,
                        (note) => _quickNotesService?.AddNote(note),
                        this);
                }
                else
                {
                    ShowTransientMessage($"❌ エラー: {result.ErrorMessage}", 3000);
                }
            }
            catch (Exception ex)
            {
                ShowTransientMessage($"❌ エラーが発生しました: {ex.Message}", 3000);
            }
        }

        /// <summary>
        /// コード改善ダイアログを表示します
        /// </summary>
        private async Task ShowCodeImprovementDialogAsync(ProgrammingLanguage language)
        {
            if (_educationService == null)
            {
                ShowTransientMessage("教育サービスが初期化されていません", 2000);
                return;
            }

            var langName = GetProgrammingLanguageDisplayName(language);
            var code = CodeInputDialog.Show($"{langName}コード改善", "改善したいコードを貼り付けてください:", "", this);
            if (string.IsNullOrWhiteSpace(code)) return;

            ShowTransientMessage($"✨ {langName}コードの改善点を分析しています...", 3000);

            try
            {
                var result = await _educationService.SuggestImprovementsAsync(code, language);
                if (result.IsSuccess)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"✨ {langName}コード改善提案\n");
                    sb.AppendLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                    if (result.Improvements.Count > 0)
                    {
                        sb.AppendLine("\n【改善点】");
                        foreach (var improvement in result.Improvements)
                        {
                            sb.AppendLine($"\n📌 [{improvement.Category}]");
                            sb.AppendLine($"   変更前: {improvement.Before}");
                            sb.AppendLine($"   変更後: {improvement.After}");
                            sb.AppendLine($"   理由: {improvement.Explanation}");
                        }
                    }

                    if (!string.IsNullOrEmpty(result.ImprovedCode))
                    {
                        sb.AppendLine($"\n【改善後のコード】\n{result.ImprovedCode}");
                    }

                    var content = sb.ToString();
                    LearningResultDialog.Show(
                        $"✨ {langName}コード改善提案",
                        content,
                        (note) => _quickNotesService?.AddNote(note),
                        this);
                }
                else
                {
                    ShowTransientMessage($"❌ エラー: {result.ErrorMessage}", 3000);
                }
            }
            catch (Exception ex)
            {
                ShowTransientMessage($"❌ エラーが発生しました: {ex.Message}", 3000);
            }
        }

        /// <summary>
        /// クリップボードのコードをレビューします
        /// </summary>
        private async Task ReviewClipboardCodeAsync()
        {
            var code = GetClipboardText();
            if (string.IsNullOrWhiteSpace(code))
            {
                ShowTransientMessage("クリップボードにテキストがありません", 2000);
                return;
            }

            // 言語を自動検出（簡易版）
            var language = DetectProgrammingLanguage(code);
            await ShowCodeReviewFromCodeAsync(code, language);
        }

        /// <summary>
        /// クリップボードのコードを改善します
        /// </summary>
        private async Task ImproveClipboardCodeAsync()
        {
            var code = GetClipboardText();
            if (string.IsNullOrWhiteSpace(code))
            {
                ShowTransientMessage("クリップボードにテキストがありません", 2000);
                return;
            }

            var language = DetectProgrammingLanguage(code);
            await ShowCodeImprovementFromCodeAsync(code, language);
        }

        /// <summary>
        /// コードから直接レビューを実行します
        /// </summary>
        private async Task ShowCodeReviewFromCodeAsync(string code, ProgrammingLanguage language)
        {
            if (_educationService == null)
            {
                ShowTransientMessage("教育サービスが初期化されていません", 2000);
                return;
            }

            var langName = GetProgrammingLanguageDisplayName(language);
            ShowTransientMessage($"🔍 {langName}コードをレビューしています...", 3000);

            try
            {
                var result = await _educationService.ReviewCodeAsync(code, language);
                if (result.IsSuccess)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"🔍 {langName}コードレビュー結果\n");
                    sb.AppendLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                    sb.AppendLine($"\n【総合スコア】 {result.OverallScore}/10");
                    sb.AppendLine($"【概要】 {result.Summary}");

                    if (result.Items.Count > 0)
                    {
                        sb.AppendLine("\n【詳細】");
                        foreach (var item in result.Items)
                        {
                            var severityEmoji = item.Severity switch
                            {
                                "Critical" => "🔴",
                                "Warning" => "🟡",
                                _ => "🔵"
                            };
                            sb.AppendLine($"\n{severityEmoji} [{item.Category}] {item.Description}");
                            if (!string.IsNullOrEmpty(item.Suggestion))
                            {
                                sb.AppendLine($"   💡 {item.Suggestion}");
                            }
                        }
                    }

                    var content = sb.ToString();
                    LearningResultDialog.Show(
                        $"🔍 {langName}コードレビュー",
                        content,
                        (note) => _quickNotesService?.AddNote(note),
                        this);
                }
                else
                {
                    ShowTransientMessage($"❌ エラー: {result.ErrorMessage}", 3000);
                }
            }
            catch (Exception ex)
            {
                ShowTransientMessage($"❌ エラーが発生しました: {ex.Message}", 3000);
            }
        }

        /// <summary>
        /// コードから直接改善を実行します
        /// </summary>
        private async Task ShowCodeImprovementFromCodeAsync(string code, ProgrammingLanguage language)
        {
            if (_educationService == null)
            {
                ShowTransientMessage("教育サービスが初期化されていません", 2000);
                return;
            }

            var langName = GetProgrammingLanguageDisplayName(language);
            ShowTransientMessage($"✨ {langName}コードの改善点を分析しています...", 3000);

            try
            {
                var result = await _educationService.SuggestImprovementsAsync(code, language);
                if (result.IsSuccess)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"✨ {langName}コード改善提案\n");
                    sb.AppendLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                    if (result.Improvements.Count > 0)
                    {
                        sb.AppendLine("\n【改善点】");
                        foreach (var improvement in result.Improvements)
                        {
                            sb.AppendLine($"\n📌 [{improvement.Category}]");
                            sb.AppendLine($"   理由: {improvement.Explanation}");
                        }
                    }

                    if (!string.IsNullOrEmpty(result.ImprovedCode))
                    {
                        sb.AppendLine($"\n【改善後のコード】\n{result.ImprovedCode}");
                    }

                    var content = sb.ToString();
                    LearningResultDialog.Show(
                        $"✨ {langName}コード改善提案",
                        content,
                        (note) => _quickNotesService?.AddNote(note),
                        this);
                }
                else
                {
                    ShowTransientMessage($"❌ エラー: {result.ErrorMessage}", 3000);
                }
            }
            catch (Exception ex)
            {
                ShowTransientMessage($"❌ エラーが発生しました: {ex.Message}", 3000);
            }
        }

        /// <summary>
        /// プログラミング言語の表示名を取得します
        /// </summary>
        private static string GetProgrammingLanguageDisplayName(ProgrammingLanguage language)
        {
            return language switch
            {
                ProgrammingLanguage.CSharp => "C#",
                ProgrammingLanguage.Python => "Python",
                ProgrammingLanguage.JavaScript => "JavaScript",
                ProgrammingLanguage.TypeScript => "TypeScript",
                ProgrammingLanguage.Java => "Java",
                ProgrammingLanguage.Rust => "Rust",
                ProgrammingLanguage.Go => "Go",
                ProgrammingLanguage.Ruby => "Ruby",
                ProgrammingLanguage.PHP => "PHP",
                ProgrammingLanguage.C => "C",
                ProgrammingLanguage.Cpp => "C++",
                ProgrammingLanguage.Swift => "Swift",
                ProgrammingLanguage.Kotlin => "Kotlin",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// コードからプログラミング言語を簡易検出します
        /// </summary>
        private static ProgrammingLanguage DetectProgrammingLanguage(string code)
        {
            if (code.Contains("using System") || code.Contains("namespace ") || code.Contains("public class") || code.Contains("async Task"))
                return ProgrammingLanguage.CSharp;
            if (code.Contains("def ") || code.Contains("import ") && !code.Contains("import {"))
                return ProgrammingLanguage.Python;
            if (code.Contains("interface ") && code.Contains(": "))
                return ProgrammingLanguage.TypeScript;
            if (code.Contains("function ") || code.Contains("const ") || code.Contains("let ") || code.Contains("=>"))
                return ProgrammingLanguage.JavaScript;
            if (code.Contains("public static void main") || code.Contains("System.out.println"))
                return ProgrammingLanguage.Java;
            if (code.Contains("fn ") || code.Contains("let mut ") || code.Contains("impl "))
                return ProgrammingLanguage.Rust;
            if (code.Contains("func ") && code.Contains("package "))
                return ProgrammingLanguage.Go;
            if (code.Contains("<?php") || code.Contains("$_"))
                return ProgrammingLanguage.PHP;
            if (code.Contains("#include") || code.Contains("int main("))
                return ProgrammingLanguage.Cpp;

            return ProgrammingLanguage.CSharp; // デフォルト
        }

        /// <summary>
        /// ランダム単語学習を表示します
        /// </summary>
        private async Task ShowRandomWordAsync(SupportedLanguage language)
        {
            if (_educationService == null)
            {
                ShowTransientMessage("教育サービスが初期化されていません", 2000);
                return;
            }

            var langName = language switch
            {
                SupportedLanguage.English => "英語",
                SupportedLanguage.Japanese => "日本語",
                SupportedLanguage.Korean => "韓国語",
                _ => "英語"
            };

            ShowTransientMessage($"🎲 {langName}のランダムな単語を選んでいます...", 3000);

            try
            {
                var result = await _educationService.GetRandomWordAsync(language, LanguageLevel.Intermediate);
                if (result.IsSuccess)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"🎲 {langName}ランダム単語学習\n");
                    sb.AppendLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                    sb.AppendLine($"\n【単語】 {result.Word}");
                    if (!string.IsNullOrEmpty(result.Pronunciation))
                    {
                        sb.AppendLine($"【発音】 {result.Pronunciation}");
                    }
                    sb.AppendLine($"【意味】 {result.Definition}");
                    if (!string.IsNullOrEmpty(result.Category))
                    {
                        sb.AppendLine($"【カテゴリ】 {result.Category}");
                    }

                    if (result.ExampleSentences.Count > 0)
                    {
                        sb.AppendLine("\n【例文】");
                        foreach (var example in result.ExampleSentences)
                        {
                            sb.AppendLine($"  • {example}");
                        }
                    }

                    if (!string.IsNullOrEmpty(result.UsageNote))
                    {
                        sb.AppendLine($"\n【使い方のポイント】\n  {result.UsageNote}");
                    }

                    if (result.RelatedWords.Count > 0)
                    {
                        sb.AppendLine($"\n【関連語】 {string.Join(", ", result.RelatedWords)}");
                    }

                    var content = sb.ToString();
                    LearningResultDialog.Show(
                        $"🎲 {langName}ランダム単語学習",
                        content,
                        (note) => _quickNotesService?.AddNote(note),
                        this);
                }
                else
                {
                    ShowTransientMessage($"❌ エラー: {result.ErrorMessage}", 3000);
                }
            }
            catch (Exception ex)
            {
                ShowTransientMessage($"❌ エラーが発生しました: {ex.Message}", 3000);
            }
        }

        /// <summary>
        /// ランダムAPI学習を表示します
        /// </summary>
        private async Task ShowRandomApiAsync(ProgrammingLanguage language)
        {
            if (_educationService == null)
            {
                ShowTransientMessage("教育サービスが初期化されていません", 2000);
                return;
            }

            var langName = GetProgrammingLanguageDisplayName(language);
            ShowTransientMessage($"🎲 {langName}のランダムなAPIを選んでいます...", 3000);

            try
            {
                var result = await _educationService.GetRandomApiAsync(language);
                if (result.IsSuccess)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"🎲 {langName} ランダムAPI学習\n");
                    sb.AppendLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                    sb.AppendLine($"\n【API名】 {result.ApiName}");
                    if (!string.IsNullOrEmpty(result.Library))
                    {
                        sb.AppendLine($"【ライブラリ】 {result.Library}");
                    }
                    sb.AppendLine($"【説明】 {result.Description}");

                    if (!string.IsNullOrEmpty(result.Syntax))
                    {
                        sb.AppendLine($"\n【構文】\n{result.Syntax}");
                    }

                    if (result.Parameters.Count > 0)
                    {
                        sb.AppendLine("\n【パラメータ】");
                        foreach (var param in result.Parameters)
                        {
                            sb.AppendLine($"  • {param}");
                        }
                    }

                    if (!string.IsNullOrEmpty(result.ReturnValue))
                    {
                        sb.AppendLine($"\n【戻り値】 {result.ReturnValue}");
                    }

                    if (!string.IsNullOrEmpty(result.CodeExample))
                    {
                        sb.AppendLine($"\n【使用例】\n{result.CodeExample}");
                    }

                    if (result.UseCases.Count > 0)
                    {
                        sb.AppendLine("\n【ユースケース】");
                        foreach (var useCase in result.UseCases)
                        {
                            sb.AppendLine($"  • {useCase}");
                        }
                    }

                    if (!string.IsNullOrEmpty(result.Tip))
                    {
                        sb.AppendLine($"\n💡 Tips: {result.Tip}");
                    }

                    var content = sb.ToString();
                    LearningResultDialog.Show(
                        $"🎲 {langName} ランダムAPI学習",
                        content,
                        (note) => _quickNotesService?.AddNote(note),
                        this);
                }
                else
                {
                    ShowTransientMessage($"❌ エラー: {result.ErrorMessage}", 3000);
                }
            }
            catch (Exception ex)
            {
                ShowTransientMessage($"❌ エラーが発生しました: {ex.Message}", 3000);
            }
        }

        /// <summary>
        /// ランダム概念学習を表示します
        /// </summary>
        private async Task ShowRandomConceptAsync(ProgrammingLanguage language)
        {
            if (_educationService == null)
            {
                ShowTransientMessage("教育サービスが初期化されていません", 2000);
                return;
            }

            var langName = GetProgrammingLanguageDisplayName(language);
            ShowTransientMessage($"🎯 {langName}のランダムな概念を選んでいます...", 3000);

            try
            {
                var result = await _educationService.GetRandomConceptAsync(language);
                if (result.IsSuccess)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"🎯 {langName} ランダム概念学習\n");
                    sb.AppendLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                    sb.AppendLine($"\n【概念名】 {result.ConceptName}");
                    if (!string.IsNullOrEmpty(result.Category))
                    {
                        sb.AppendLine($"【カテゴリ】 {result.Category}");
                    }
                    sb.AppendLine($"\n【説明】\n{result.Explanation}");

                    if (result.KeyPoints.Count > 0)
                    {
                        sb.AppendLine("\n【ポイント】");
                        foreach (var point in result.KeyPoints)
                        {
                            sb.AppendLine($"  • {point}");
                        }
                    }

                    if (!string.IsNullOrEmpty(result.CodeExample))
                    {
                        sb.AppendLine($"\n【コード例】\n{result.CodeExample}");
                    }

                    if (!string.IsNullOrEmpty(result.WhenToUse))
                    {
                        sb.AppendLine($"\n【いつ使う？】\n{result.WhenToUse}");
                    }

                    if (result.RelatedConcepts.Count > 0)
                    {
                        sb.AppendLine($"\n【関連概念】 {string.Join(", ", result.RelatedConcepts)}");
                    }

                    var content = sb.ToString();
                    LearningResultDialog.Show(
                        $"🎯 {langName} ランダム概念学習",
                        content,
                        (note) => _quickNotesService?.AddNote(note),
                        this);
                }
                else
                {
                    ShowTransientMessage($"❌ エラー: {result.ErrorMessage}", 3000);
                }
            }
            catch (Exception ex)
            {
                ShowTransientMessage($"❌ エラーが発生しました: {ex.Message}", 3000);
            }
        }

        #endregion

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

            // ホットキーヘルプ
            var hotkeyHelpItem = new MenuItem { Header = "⌨️ ホットキー一覧" };
            hotkeyHelpItem.Click += (s, args) => ShowHotkeyHelp();
            contextMenu.Items.Add(hotkeyHelpItem);

            // 設定ウィンドウ
            var settingsWindowItem = new MenuItem { Header = "🔧 設定ウィンドウ" };
            settingsWindowItem.Click += (s, args) => OpenSettingsWindow();
            contextMenu.Items.Add(settingsWindowItem);

            contextMenu.PlacementTarget = button;
            contextMenu.Placement = PlacementMode.Bottom;
            contextMenu.IsOpen = true;
        }

        /// <summary>
        /// ホットキー一覧を表示します
        /// </summary>
        private void ShowHotkeyHelp()
        {
            var helpText = @"⌨️ ホットキー一覧

📸 Ctrl+Shift+P
   → スクリーンショット撮影

🔊 Ctrl+Shift+S
   → 選択テキストを読み上げ

🌐 Ctrl+Shift+T
   → 選択テキストを翻訳

👆 Ctrl+Alt+T
   → クリックスルー切替
   （ウィンドウを透過）

💡 使い方:
・スクリーンショット: 任意のウィンドウでホットキーを押す
・読み上げ/翻訳: テキスト選択→Ctrl+C→ホットキー";

            OpenChatAndShowMessage(helpText);
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
                bool wasUnread = email.IsUnread;

                if (wasUnread)
                {
                    success = await _gmailService.MarkAsReadAsync(emailId);
                    if (success)
                    {
                        // ローカルの状態を更新
                        email.IsUnread = false;
                        // ボタンの表示を更新
                        button.Content = "○";
                        button.Background = new SolidColorBrush(Color.FromRgb(59, 130, 246)); // 青: 未読にする
                        button.ToolTip = "未読にする";
                        ShowTransientMessage("既読にしました", 1500);
                    }
                }
                else
                {
                    success = await _gmailService.MarkAsUnreadAsync(emailId);
                    if (success)
                    {
                        // ローカルの状態を更新
                        email.IsUnread = true;
                        // ボタンの表示を更新
                        button.Content = "✓";
                        button.Background = new SolidColorBrush(Color.FromRgb(34, 197, 94)); // 緑: 既読にする
                        button.ToolTip = "既読にする";
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

        #region Pomodoro Timer Handlers

        /// <summary>
        /// ポモドーロタイマーの更新時に呼び出されます
        /// </summary>
        private void OnPomodoroTick(object? sender, PomodoroTickEventArgs e)
        {
            // タイトルまたはトランジェントメッセージで残り時間を表示
            var timeText = _pomodoroService?.GetTimeDisplayText() ?? "00:00";
            var stateText = _pomodoroService?.GetStateDisplayText() ?? "";
            TransientMessage.Text = $"{stateText} {timeText}";
            TransientBorder.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// ポモドーロタイマーの状態変化時に呼び出されます
        /// </summary>
        private void OnPomodoroStateChanged(object? sender, PomodoroStateChangedEventArgs e)
        {
            if (e.NewState == PomodoroState.Stopped)
            {
                TransientBorder.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// ポモドーロが完了したときに呼び出されます
        /// </summary>
        private void OnPomodoroCompleted(object? sender, PomodoroCompletedEventArgs e)
        {
            var message = e.WasWorkSession
                ? $"🍅 ポモドーロ {e.TotalCompleted} 完了！休憩を取りましょう。"
                : "☕ 休憩終了！作業を再開しましょう。";

            OpenChatAndShowMessage(message);

            // コンパニオンシップ：ポモドーロ完了を記録（作業セッションのみ）
            if (e.WasWorkSession)
            {
                _companionshipManager?.RecordPomodoroComplete();
            }
        }

        /// <summary>
        /// ポモドーロメニューを表示します
        /// </summary>
        private void ShowPomodoroMenu(Button button)
        {
            var contextMenu = new ContextMenu();

            if (_pomodoroService == null) return;

            var state = _pomodoroService.State;

            if (state == PomodoroState.Stopped)
            {
                var startWorkItem = new MenuItem { Header = "🍅 作業開始 (25分)" };
                startWorkItem.Click += (s, e) =>
                {
                    _pomodoroService.StartWork();
                    ShowTransientMessage("🍅 作業開始！25分集中しましょう", 3000);
                };
                contextMenu.Items.Add(startWorkItem);

                var startBreakItem = new MenuItem { Header = "☕ 休憩開始" };
                startBreakItem.Click += (s, e) =>
                {
                    _pomodoroService.StartBreak();
                    ShowTransientMessage("☕ 休憩開始！", 3000);
                };
                contextMenu.Items.Add(startBreakItem);

                contextMenu.Items.Add(new Separator());

                // カスタムリマインダー
                var reminderMenu = new MenuItem { Header = "⏰ リマインダー設定" };
                var reminders = new[] { (5, "5分後"), (10, "10分後"), (15, "15分後"), (30, "30分後"), (60, "1時間後") };
                foreach (var (minutes, label) in reminders)
                {
                    var reminderItem = new MenuItem { Header = label };
                    var min = minutes;
                    reminderItem.Click += (s, e) =>
                    {
                        _pomodoroService.SetReminder(min, $"⏰ {label}のリマインダー", msg =>
                        {
                            OpenChatAndShowMessage(msg);
                        });
                        ShowTransientMessage($"⏰ {label}にリマインダーを設定しました", 2000);
                    };
                    reminderMenu.Items.Add(reminderItem);
                }
                contextMenu.Items.Add(reminderMenu);
            }
            else if (state == PomodoroState.Paused)
            {
                var resumeItem = new MenuItem { Header = "▶️ 再開" };
                resumeItem.Click += (s, e) => _pomodoroService.Resume();
                contextMenu.Items.Add(resumeItem);

                var stopItem = new MenuItem { Header = "⏹️ 停止" };
                stopItem.Click += (s, e) =>
                {
                    _pomodoroService.Stop();
                    ShowTransientMessage("タイマーを停止しました", 2000);
                };
                contextMenu.Items.Add(stopItem);
            }
            else
            {
                var pauseItem = new MenuItem { Header = "⏸️ 一時停止" };
                pauseItem.Click += (s, e) => _pomodoroService.Pause();
                contextMenu.Items.Add(pauseItem);

                var stopItem = new MenuItem { Header = "⏹️ 停止" };
                stopItem.Click += (s, e) =>
                {
                    _pomodoroService.Stop();
                    ShowTransientMessage("タイマーを停止しました", 2000);
                };
                contextMenu.Items.Add(stopItem);
            }

            contextMenu.Items.Add(new Separator());

            // 統計
            var statsItem = new MenuItem { Header = $"📊 完了: {_pomodoroService.CompletedPomodoros} ポモドーロ" };
            statsItem.IsEnabled = false;
            contextMenu.Items.Add(statsItem);

            var resetCountItem = new MenuItem { Header = "🔄 カウントリセット" };
            resetCountItem.Click += (s, e) =>
            {
                _pomodoroService.ResetCount();
                ShowTransientMessage("カウントをリセットしました", 2000);
            };
            contextMenu.Items.Add(resetCountItem);

            contextMenu.PlacementTarget = button;
            contextMenu.Placement = PlacementMode.Bottom;
            contextMenu.IsOpen = true;
        }

        #endregion

        #region Mini Game Handlers

        /// <summary>
        /// じゃんけんゲームメニューを表示します
        /// </summary>
        private void ShowMiniGameMenu(Button button)
        {
            if (_miniGameService == null) return;

            var contextMenu = new ContextMenu();

            var titleItem = new MenuItem { Header = "🎮 ペットとじゃんけん！", IsEnabled = false };
            contextMenu.Items.Add(titleItem);
            contextMenu.Items.Add(new Separator());

            // じゃんけんの選択肢
            var rockItem = new MenuItem { Header = "✊ グー" };
            rockItem.Click += (s, e) => PlayRockPaperScissors(RpsChoice.Rock);
            contextMenu.Items.Add(rockItem);

            var paperItem = new MenuItem { Header = "✋ パー" };
            paperItem.Click += (s, e) => PlayRockPaperScissors(RpsChoice.Paper);
            contextMenu.Items.Add(paperItem);

            var scissorsItem = new MenuItem { Header = "✌️ チョキ" };
            scissorsItem.Click += (s, e) => PlayRockPaperScissors(RpsChoice.Scissors);
            contextMenu.Items.Add(scissorsItem);

            contextMenu.Items.Add(new Separator());

            // 戦績
            var statsItem = new MenuItem { Header = _miniGameService.GetStatsText(), IsEnabled = false };
            contextMenu.Items.Add(statsItem);

            var resetItem = new MenuItem { Header = "🔄 戦績リセット" };
            resetItem.Click += (s, e) =>
            {
                _miniGameService.ResetStats();
                ShowTransientMessage("戦績をリセットしました", 2000);
            };
            contextMenu.Items.Add(resetItem);

            contextMenu.PlacementTarget = button;
            contextMenu.Placement = PlacementMode.Bottom;
            contextMenu.IsOpen = true;
        }

        /// <summary>
        /// じゃんけんをプレイします
        /// </summary>
        private void PlayRockPaperScissors(RpsChoice playerChoice)
        {
            if (_miniGameService == null) return;

            var result = _miniGameService.PlayRockPaperScissors(playerChoice);
            var detailText = result.GetDetailText();
            var petReaction = _miniGameService.GetPetReaction(result.Result);

            var message = $"{detailText}\n\n🐾 ペット「{petReaction}」\n\n{_miniGameService.GetStatsText()}";
            OpenChatAndShowMessage(message);
        }

        #endregion

        #region Voice Input/Output Handlers

        /// <summary>
        /// 最後の応答を保存するフィールド
        /// </summary>
        private string? _lastAssistantResponse;

        /// <summary>
        /// 音声認識が完了したときに呼び出されます
        /// </summary>
        private void OnVoiceRecognized(object? sender, VoiceRecognizedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // 認識されたテキストを入力欄に設定
                ChatInputBox.Text = e.Text;
                ShowTransientMessage($"🎤 認識完了 (信頼度: {e.Confidence:P0})", 2000);

                // ボタン状態をリセット
                UpdateVoiceInputButtonState(VoiceInputState.Stopped);

                // 信頼度が高ければ自動送信
                if (e.Confidence >= 0.8f)
                {
                    OnSendMessageClick(this, new RoutedEventArgs());
                }
            });
        }

        /// <summary>
        /// 音声入力の状態が変化したときに呼び出されます
        /// </summary>
        private void OnVoiceInputStateChanged(object? sender, VoiceInputState state)
        {
            UpdateVoiceInputButtonState(state);
        }

        /// <summary>
        /// 音声認識エラー時に呼び出されます
        /// </summary>
        private void OnVoiceError(object? sender, string error)
        {
            Dispatcher.Invoke(() =>
            {
                ShowTransientMessage($"⚠️ {error}", 3000);
                UpdateVoiceInputButtonState(VoiceInputState.Stopped);
            });
        }

        /// <summary>
        /// 音声入力ボタンがクリックされたときに呼び出されます
        /// </summary>
        private void OnVoiceInputButtonClick(object sender, RoutedEventArgs e)
        {
            if (_voiceInputService == null || !_voiceInputService.IsAvailable)
            {
                ShowTransientMessage("音声認識が利用できません", 2000);
                return;
            }

            // 既に聞いている場合は停止
            if (_voiceInputService.State == VoiceInputState.Listening)
            {
                _voiceInputService.StopListening();
                UpdateVoiceInputButtonState(VoiceInputState.Stopped);
                ShowTransientMessage("音声入力をキャンセルしました", 1500);
                return;
            }

            // 音声入力を開始
            _voiceInputService.StartListening();
            UpdateVoiceInputButtonState(VoiceInputState.Listening);
            ShowTransientMessage("🎤 話してください...", 10000);
        }

        /// <summary>
        /// 音声入力ボタンの見た目を更新します
        /// </summary>
        private void UpdateVoiceInputButtonState(VoiceInputState state)
        {
            Dispatcher.Invoke(() =>
            {
                switch (state)
                {
                    case VoiceInputState.Listening:
                        VoiceInputButtonBorder.Background = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // 赤 - 録音中
                        VoiceInputButton.Content = "🔴";
                        VoiceInputButton.ToolTip = "クリックで停止";
                        break;
                    case VoiceInputState.Processing:
                        VoiceInputButtonBorder.Background = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // オレンジ - 処理中
                        VoiceInputButton.Content = "⏳";
                        VoiceInputButton.ToolTip = "処理中...";
                        break;
                    default:
                        VoiceInputButtonBorder.Background = new SolidColorBrush(Color.FromRgb(156, 39, 176)); // 紫 - 待機
                        VoiceInputButton.Content = "🎤";
                        VoiceInputButton.ToolTip = "音声入力";
                        break;
                }
            });
        }

        /// <summary>
        /// 音声入力を開始します
        /// </summary>
        private void StartVoiceInput()
        {
            if (_voiceInputService == null)
            {
                ShowTransientMessage("音声入力サービスが利用できません", 2000);
                return;
            }

            if (!_voiceInputService.IsAvailable)
            {
                ShowTransientMessage("音声認識エンジンが見つかりません", 2000);
                return;
            }

            // チャットを開く
            if (!_isChatOpen)
            {
                _isChatOpen = true;
                ChatBalloon.Visibility = Visibility.Visible;
            }

            _voiceInputService.StartListening();
            ShowTransientMessage("🎤 話してください...", 10000);
        }

        /// <summary>
        /// 最新のAI応答を読み上げます
        /// </summary>
        private void SpeakLastResponse()
        {
            if (_voiceOutputService == null || !_voiceOutputService.IsAvailable)
            {
                ShowTransientMessage("音声出力が利用できません", 2000);
                return;
            }

            if (string.IsNullOrEmpty(_lastAssistantResponse))
            {
                ShowTransientMessage("読み上げる応答がありません", 2000);
                return;
            }

            ShowTransientMessage("🔊 読み上げ中...", 5000);
            _voiceOutputService.SpeakAsync(_lastAssistantResponse);
        }

        /// <summary>
        /// 指定したテキストを読み上げます
        /// </summary>
        private void SpeakText(string text)
        {
            if (_voiceOutputService == null || !_voiceOutputService.IsAvailable)
            {
                return;
            }

            _voiceOutputService.SpeakAsync(text);
        }

        /// <summary>
        /// クリップボードのテキストを読み上げます（ホットキー用）
        /// </summary>
        private void SpeakClipboardText()
        {
            if (_voiceOutputService == null || !_voiceOutputService.IsAvailable)
            {
                ShowTransientMessage("音声出力が利用できません", 2000);
                return;
            }

            var text = GetClipboardText();
            if (string.IsNullOrWhiteSpace(text))
            {
                ShowTransientMessage("読み上げるテキストがありません", 2000);
                return;
            }

            // 長すぎるテキストは警告
            if (text.Length > 5000)
            {
                ShowTransientMessage("⚠️ テキストが長すぎます（最初の5000文字のみ読み上げ）", 2000);
                text = text[..5000];
            }

            ShowTransientMessage($"🔊 読み上げ中... ({text.Length}文字)", 3000);
            _voiceOutputService.SpeakAsync(text);
        }

        #endregion

        #region Clipboard History Handlers

        /// <summary>
        /// クリップボード履歴メニューを表示
        /// </summary>
        private void ShowClipboardHistoryMenu(Button button)
        {
            if (_clipboardHistoryService == null) return;

            // 現在のクリップボードをチェック
            _clipboardHistoryService.CheckAndAddFromClipboard();

            var contextMenu = new ContextMenu();

            var titleItem = new MenuItem { Header = $"📋 履歴 ({_clipboardHistoryService.Count}件)", IsEnabled = false };
            contextMenu.Items.Add(titleItem);
            contextMenu.Items.Add(new Separator());

            if (_clipboardHistoryService.Count == 0)
            {
                var emptyItem = new MenuItem { Header = "履歴がありません", IsEnabled = false };
                contextMenu.Items.Add(emptyItem);
            }
            else
            {
                for (int i = 0; i < Math.Min(_clipboardHistoryService.History.Count, 10); i++)
                {
                    var item = _clipboardHistoryService.History[i];
                    var index = i;
                    var menuItem = new MenuItem
                    {
                        Header = $"{i + 1}. {item.Preview}",
                        ToolTip = item.Text.Length > 200 ? item.Text[..200] + "..." : item.Text
                    };
                    menuItem.Click += (s, e) =>
                    {
                        _clipboardHistoryService.CopyFromHistory(index);
                        ShowTransientMessage("📋 クリップボードにコピーしました", 1500);
                    };
                    contextMenu.Items.Add(menuItem);
                }

                contextMenu.Items.Add(new Separator());

                var clearItem = new MenuItem { Header = "🗑️ 履歴をクリア" };
                clearItem.Click += (s, e) =>
                {
                    _clipboardHistoryService.Clear();
                    ShowTransientMessage("履歴をクリアしました", 1500);
                };
                contextMenu.Items.Add(clearItem);
            }

            contextMenu.PlacementTarget = button;
            contextMenu.Placement = PlacementMode.Bottom;
            contextMenu.IsOpen = true;
        }

        #endregion

        #region Quick Notes Handlers

        /// <summary>
        /// クイックノートメニューを表示
        /// </summary>
        private void ShowQuickNotesMenu(Button button)
        {
            if (_quickNotesService == null) return;

            var contextMenu = new ContextMenu();

            var titleItem = new MenuItem { Header = $"📝 ノート ({_quickNotesService.Count}件)", IsEnabled = false };
            contextMenu.Items.Add(titleItem);

            // 新規ノート追加
            var addItem = new MenuItem { Header = "➕ 新しいノートを追加" };
            addItem.Click += (s, e) => ShowAddNoteDialog();
            contextMenu.Items.Add(addItem);

            contextMenu.Items.Add(new Separator());

            if (_quickNotesService.Count == 0)
            {
                var emptyItem = new MenuItem { Header = "ノートがありません", IsEnabled = false };
                contextMenu.Items.Add(emptyItem);
            }
            else
            {
                foreach (var note in _quickNotesService.Notes.Take(10))
                {
                    var noteMenu = new MenuItem
                    {
                        Header = (note.IsPinned ? "📌 " : "") + note.Preview
                    };

                    var viewItem = new MenuItem { Header = "👁️ 表示" };
                    var noteContent = note.Content;
                    viewItem.Click += (s, e) => LearningResultDialog.Show("📝 ノート", noteContent, null, this);

                    var copyItem = new MenuItem { Header = "📋 コピー" };
                    copyItem.Click += (s, e) =>
                    {
                        System.Windows.Clipboard.SetText(note.Content);
                        ShowTransientMessage("ノートをコピーしました", 1500);
                    };

                    var pinItem = new MenuItem { Header = note.IsPinned ? "📌 ピン解除" : "📌 ピン留め" };
                    var noteId = note.Id;
                    pinItem.Click += (s, e) => _quickNotesService.TogglePin(noteId);

                    var deleteItem = new MenuItem { Header = "🗑️ 削除" };
                    deleteItem.Click += (s, e) =>
                    {
                        _quickNotesService.DeleteNote(noteId);
                        ShowTransientMessage("ノートを削除しました", 1500);
                    };

                    noteMenu.Items.Add(viewItem);
                    noteMenu.Items.Add(copyItem);
                    noteMenu.Items.Add(pinItem);
                    noteMenu.Items.Add(new Separator());
                    noteMenu.Items.Add(deleteItem);

                    contextMenu.Items.Add(noteMenu);
                }
            }

            contextMenu.PlacementTarget = button;
            contextMenu.Placement = PlacementMode.Bottom;
            contextMenu.IsOpen = true;
        }

        /// <summary>
        /// ノート追加ダイアログを表示
        /// </summary>
        private void ShowAddNoteDialog()
        {
            var input = InputDialog.Show("📝 新しいノート", "ノートの内容を入力してください:", "", this);
            if (!string.IsNullOrWhiteSpace(input))
            {
                _quickNotesService?.AddNote(input);
                ShowTransientMessage("📝 ノートを追加しました", 1500);
            }
        }

        #endregion

        #region Daily Goals Handlers

        /// <summary>
        /// デイリーゴールメニューを表示
        /// </summary>
        private void ShowDailyGoalsMenu(Button button)
        {
            if (_dailyGoalsService == null) return;

            var contextMenu = new ContextMenu();

            // 進捗表示
            var progressItem = new MenuItem { Header = _dailyGoalsService.GetProgressText(), IsEnabled = false };
            contextMenu.Items.Add(progressItem);

            var motivationItem = new MenuItem { Header = _dailyGoalsService.GetMotivationMessage(), IsEnabled = false };
            contextMenu.Items.Add(motivationItem);

            contextMenu.Items.Add(new Separator());

            // 新規目標追加
            var addItem = new MenuItem { Header = "➕ 新しい目標を追加" };
            addItem.Click += (s, e) => ShowAddGoalDialog();
            contextMenu.Items.Add(addItem);

            contextMenu.Items.Add(new Separator());

            if (_dailyGoalsService.Goals.Count == 0)
            {
                var emptyItem = new MenuItem { Header = "目標がありません", IsEnabled = false };
                contextMenu.Items.Add(emptyItem);
            }
            else
            {
                foreach (var goal in _dailyGoalsService.Goals)
                {
                    var emoji = goal.IsCompleted ? "✅" : "⬜";
                    var streakText = goal.Streak > 0 ? $" 🔥{goal.Streak}" : "";
                    var goalItem = new MenuItem
                    {
                        Header = $"{emoji} {goal.Title}{streakText}"
                    };

                    var goalId = goal.Id;
                    goalItem.Click += (s, e) =>
                    {
                        _dailyGoalsService.ToggleGoal(goalId);
                        var newState = _dailyGoalsService.Goals.FirstOrDefault(g => g.Id == goalId)?.IsCompleted ?? false;
                        ShowTransientMessage(newState ? "✅ 完了！" : "⬜ 未完了に戻しました", 1500);

                        // コンパニオンシップ：ゴール達成を記録（完了時のみ）
                        if (newState)
                        {
                            _companionshipManager?.RecordGoalAchieved();
                        }
                    };

                    contextMenu.Items.Add(goalItem);
                }

                contextMenu.Items.Add(new Separator());

                // 全目標削除
                var clearItem = new MenuItem { Header = "🗑️ すべての目標を削除" };
                clearItem.Click += (s, e) =>
                {
                    foreach (var goal in _dailyGoalsService.Goals.ToList())
                    {
                        _dailyGoalsService.DeleteGoal(goal.Id);
                    }
                    ShowTransientMessage("目標をすべて削除しました", 1500);
                };
                contextMenu.Items.Add(clearItem);
            }

            contextMenu.PlacementTarget = button;
            contextMenu.Placement = PlacementMode.Bottom;
            contextMenu.IsOpen = true;
        }

        /// <summary>
        /// 目標追加ダイアログを表示
        /// </summary>
        private void ShowAddGoalDialog()
        {
            var input = InputDialog.Show("🎯 新しい目標", "今日の目標を入力してください:", "", this);
            if (!string.IsNullOrWhiteSpace(input))
            {
                _dailyGoalsService?.AddGoal(input);
                ShowTransientMessage("🎯 目標を追加しました", 1500);
            }
        }

        #endregion

        #region Screenshot Handlers

        /// <summary>
        /// スクリーンショットを撮影（メニューから呼び出し）
        /// </summary>
        private void CaptureScreenshot(bool activeWindowOnly)
        {
            if (_screenshotService == null) return;

            try
            {
                // アシスタントウィンドウを一時的に非表示にする
                var wasVisible = this.Visibility == Visibility.Visible;
                if (activeWindowOnly && wasVisible)
                {
                    this.Visibility = Visibility.Hidden;
                }

                // 少し待ってからキャプチャ（メニューが閉じる＆ウィンドウが非表示になるのを待つ）
                System.Threading.Tasks.Task.Delay(400).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        var bitmap = activeWindowOnly
                            ? _screenshotService.CaptureActiveWindow()
                            : _screenshotService.CaptureFullScreen();

                        // ウィンドウを再表示
                        if (wasVisible)
                        {
                            this.Visibility = Visibility.Visible;
                        }

                        if (bitmap != null)
                        {
                            // クリップボードにコピー
                            _screenshotService.CopyToClipboard(bitmap);

                            // ファイルに保存
                            var path = _screenshotService.SaveToFile(bitmap);

                            ShowTransientMessage($"📸 スクリーンショットを保存しました", 2000);

                            bitmap.Dispose();
                        }
                        else
                        {
                            ShowTransientMessage("スクリーンショットの撮影に失敗しました", 2000);
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                ShowTransientMessage($"エラー: {ex.Message}", 2000);
            }
        }

        /// <summary>
        /// ホットキーからスクリーンショットを撮影（アクティブウィンドウのみ）
        /// </summary>
        private void CaptureScreenshotWithHotkey()
        {
            if (_screenshotService == null) return;

            try
            {
                // アシスタントウィンドウを一時的に非表示にする
                var wasVisible = this.Visibility == Visibility.Visible;
                this.Visibility = Visibility.Hidden;

                // 少し待ってからキャプチャ（ウィンドウが非表示になるのを待つ）
                System.Threading.Tasks.Task.Delay(200).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        var bitmap = _screenshotService.CaptureActiveWindow();

                        // ウィンドウを再表示
                        if (wasVisible)
                        {
                            this.Visibility = Visibility.Visible;
                        }

                        if (bitmap != null)
                        {
                            // クリップボードにコピー
                            _screenshotService.CopyToClipboard(bitmap);

                            // ファイルに保存
                            var path = _screenshotService.SaveToFile(bitmap);

                            ShowTransientMessage($"📸 スクリーンショットを保存しました", 2000);

                            bitmap.Dispose();
                        }
                        else
                        {
                            ShowTransientMessage("スクリーンショットの撮影に失敗しました", 2000);
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                ShowTransientMessage($"エラー: {ex.Message}", 2000);
            }
        }

        #endregion

        #region Music Player Handlers

        /// <summary>
        /// 音楽ボタンクリック - ポップアップを開閉
        /// </summary>
        private void OnMusicButtonClick(object sender, RoutedEventArgs e)
        {
            MusicPlayerPopup.Visibility = MusicPlayerPopup.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        /// <summary>
        /// 音楽プレーヤーポップアップを閉じる
        /// </summary>
        private void OnCloseMusicPlayerClick(object sender, RoutedEventArgs e)
        {
            MusicPlayerPopup.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Lo-Fi再生ボタンクリック
        /// </summary>
        private async void OnLofiPlayClick(object sender, RoutedEventArgs e)
        {
            if (_youtubePlayer == null)
            {
                ShowTransientMessage("音楽プレーヤーが初期化されていません", 2000);
                return;
            }

            try
            {
                await _youtubePlayer.PlayLofiAsync();
                ShowTransientMessage("Lofi Girl を再生中...", 2000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Music] Lo-Fi再生エラー: {ex.Message}");
                ShowTransientMessage("再生に失敗しました", 2000);
            }
        }

        /// <summary>
        /// 音楽停止ボタンクリック
        /// </summary>
        private async void OnMusicStopClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // YouTubeを停止
                if (_youtubePlayer?.IsPlaying == true)
                {
                    await _youtubePlayer.StopAsync();
                }

                // BGMを停止
                if (_bgmService?.IsPlaying == true)
                {
                    _bgmService.Stop();
                }

                ShowTransientMessage("停止しました", 1500);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Music] 停止エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// 音量スライダー変更
        /// </summary>
        private async void OnMusicVolumeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var volume = (int)e.NewValue;

            try
            {
                // YouTubeの音量を設定
                if (_youtubePlayer != null)
                {
                    await _youtubePlayer.SetVolumeAsync(volume);
                }

                // BGMの音量を設定
                if (_bgmService != null)
                {
                    _bgmService.Volume = volume / 100f;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Music] 音量設定エラー: {ex.Message}");
            }
        }

        #endregion

        #region Widget Mode Handlers

        /// <summary>
        /// ウィジェットモードボタンクリック
        /// </summary>
        private void OnWidgetModeButtonClick(object sender, RoutedEventArgs e)
        {
            EnterWidgetMode();
        }

        /// <summary>
        /// ウィジェットモードから通常モードに戻る
        /// </summary>
        private void OnExpandFromWidgetClick(object sender, RoutedEventArgs e)
        {
            ExitWidgetMode();
        }

        /// <summary>
        /// ウィジェットモードに入る
        /// </summary>
        private void EnterWidgetMode()
        {
            if (_isWidgetMode) return;

            _isWidgetMode = true;

            // 現在のサイズを保存
            _normalSize = new Size(Width, Height);

            // 通常のUIを非表示
            CharacterBorder.Visibility = Visibility.Collapsed;
            ChatBalloon.Visibility = Visibility.Collapsed;
            MusicPlayerPopup.Visibility = Visibility.Collapsed;
            PetSelectorPopup.Visibility = Visibility.Collapsed;
            DialogueBubble.Visibility = Visibility.Collapsed;

            // 右側のボタンパネルを非表示（上部のボタンは残す）
            var quickActionsPanel = FindName("WidgetModeButton")?.GetType().DeclaringType;

            // ウィジェットパネルを表示
            WidgetPanel.Visibility = Visibility.Visible;

            // ウィンドウサイズを縮小
            Width = _widgetSize.Width;
            Height = _widgetSize.Height;

            // 時計タイマーを開始
            StartWidgetTimer();

            // ウィジェット情報を更新
            UpdateWidgetInfo();

            Console.WriteLine("[Widget] ウィジェットモードに切り替え");
        }

        /// <summary>
        /// ウィジェットモードから抜ける
        /// </summary>
        private void ExitWidgetMode()
        {
            if (!_isWidgetMode) return;

            _isWidgetMode = false;

            // 時計タイマーを停止
            StopWidgetTimer();

            // ウィジェットパネルを非表示
            WidgetPanel.Visibility = Visibility.Collapsed;

            // 通常のUIを復元
            CharacterBorder.Visibility = Visibility.Visible;

            // ウィンドウサイズを復元
            Width = _normalSize.Width;
            Height = _normalSize.Height;

            Console.WriteLine("[Widget] 通常モードに切り替え");
        }

        /// <summary>
        /// ウィジェットタイマーを開始
        /// </summary>
        private void StartWidgetTimer()
        {
            _widgetTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _widgetTimer.Tick += OnWidgetTimerTick;
            _widgetTimer.Start();
        }

        /// <summary>
        /// ウィジェットタイマーを停止
        /// </summary>
        private void StopWidgetTimer()
        {
            _widgetTimer?.Stop();
            _widgetTimer = null;
        }

        /// <summary>
        /// ウィジェットタイマーのティック
        /// </summary>
        private void OnWidgetTimerTick(object? sender, EventArgs e)
        {
            UpdateWidgetTime();
        }

        /// <summary>
        /// ウィジェットの時刻を更新
        /// </summary>
        private void UpdateWidgetTime()
        {
            var now = DateTime.Now;
            WidgetTimeText.Text = now.ToString("HH:mm");
            WidgetDateText.Text = now.ToString("M/d (ddd)");
        }

        /// <summary>
        /// ウィジェット情報を更新
        /// </summary>
        private void UpdateWidgetInfo()
        {
            // 時刻を更新
            UpdateWidgetTime();

            // 音楽ステータスを更新
            if (_youtubePlayer?.IsPlaying == true)
            {
                WidgetMusicPanel.Visibility = Visibility.Visible;
                WidgetMusicText.Text = _youtubePlayer.CurrentTitle ?? "再生中";
            }
            else if (_bgmService?.IsPlaying == true)
            {
                WidgetMusicPanel.Visibility = Visibility.Visible;
                WidgetMusicText.Text = _bgmService.CurrentTrack?.Name ?? "再生中";
            }
            else
            {
                WidgetMusicPanel.Visibility = Visibility.Collapsed;
            }

            // ポモドーロステータスを更新
            if (_pomodoroService != null &&
                (_pomodoroService.State == PomodoroState.Working ||
                 _pomodoroService.State == PomodoroState.ShortBreak ||
                 _pomodoroService.State == PomodoroState.LongBreak))
            {
                WidgetPomodoroPanel.Visibility = Visibility.Visible;
                var remaining = _pomodoroService.RemainingTime;
                WidgetPomodoroText.Text = $"{remaining.Minutes:D2}:{remaining.Seconds:D2}";
            }
            else
            {
                WidgetPomodoroPanel.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// ウィジェットのチャットボタンクリック
        /// </summary>
        private void OnWidgetChatClick(object sender, RoutedEventArgs e)
        {
            ExitWidgetMode();
            _isChatOpen = true;
            ChatBalloon.Visibility = Visibility.Visible;
            ChatInputBox.Focus();
        }

        /// <summary>
        /// ウィジェットの音楽ボタンクリック
        /// </summary>
        private void OnWidgetMusicClick(object sender, RoutedEventArgs e)
        {
            ExitWidgetMode();
            MusicPlayerPopup.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// ウィジェットのポモドーロボタンクリック
        /// </summary>
        private void OnWidgetPomodoroClick(object sender, RoutedEventArgs e)
        {
            // ポモドーロをトグル
            if (_pomodoroService == null) return;

            var state = _pomodoroService.State;

            if (state == PomodoroState.Working || state == PomodoroState.ShortBreak || state == PomodoroState.LongBreak)
            {
                _pomodoroService.Pause();
                ShowTransientMessage("🍅 ポモドーロを一時停止", 2000);
            }
            else if (state == PomodoroState.Paused)
            {
                _pomodoroService.Resume();
                ShowTransientMessage("🍅 ポモドーロを再開", 2000);
            }
            else
            {
                _pomodoroService.StartWork();
                ShowTransientMessage("🍅 ポモドーロ開始！", 2000);
            }

            UpdateWidgetInfo();
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