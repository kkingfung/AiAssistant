#nullable enable
using System;
using System.Diagnostics;
using System.Timers;

namespace AiAssistant.Companionship
{
    /// <summary>
    /// コンパニオンシップシステムの統合マネージャー
    /// 絆、感情、行動サービスを統合し、自動更新を管理
    /// </summary>
    public sealed class CompanionshipManager : IDisposable
    {
        private readonly CompanionContext _context;
        private readonly IBondService _bondService;
        private readonly IEmotionService _emotionService;
        private readonly IPetBehaviorService _behaviorService;
        private readonly ICompanionshipDataService _dataService;

        private readonly System.Timers.Timer _updateTimer;
        private readonly System.Timers.Timer _autoSaveTimer;

        private bool _isDisposed;
        private bool _isFirstInteractionToday = true;

        /// <summary>
        /// 更新間隔（秒）
        /// </summary>
        public const int UpdateIntervalSeconds = 30;

        /// <summary>
        /// 自動保存間隔（秒）
        /// </summary>
        public const int AutoSaveIntervalSeconds = 60;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public CompanionshipManager()
        {
            // データサービスを初期化して既存データを読み込み
            _dataService = new CompanionshipDataService();
            _context = _dataService.Load();

            // 各サービスを初期化
            _bondService = new BondService(_context);
            _emotionService = new EmotionService(_context);
            _behaviorService = new PetBehaviorService(_context);

            // イベントを接続
            _bondService.LevelUp += OnBondLevelUp;
            _emotionService.EmotionChanged += OnEmotionChanged;

            // 更新タイマーを設定
            _updateTimer = new System.Timers.Timer(UpdateIntervalSeconds * 1000);
            _updateTimer.Elapsed += OnUpdateTimerElapsed;
            _updateTimer.AutoReset = true;

            // 自動保存タイマーを設定
            _autoSaveTimer = new System.Timers.Timer(AutoSaveIntervalSeconds * 1000);
            _autoSaveTimer.Elapsed += OnAutoSaveTimerElapsed;
            _autoSaveTimer.AutoReset = true;

            // 初日チェック
            _isFirstInteractionToday = _context.LastActiveDate.Date != DateTime.Today;

            Debug.WriteLine($"[CompanionshipManager] Initialized. BondLevel: {_context.BondLevel}, Emotion: {_context.CurrentEmotionType}");
        }

        /// <summary>コンパニオンコンテキスト</summary>
        public CompanionContext Context => _context;

        /// <summary>絆サービス</summary>
        public IBondService BondService => _bondService;

        /// <summary>感情サービス</summary>
        public IEmotionService EmotionService => _emotionService;

        /// <summary>行動サービス</summary>
        public IPetBehaviorService BehaviorService => _behaviorService;

        /// <summary>データサービス</summary>
        public ICompanionshipDataService DataService => _dataService;

        /// <summary>
        /// システムを開始
        /// </summary>
        public void Start()
        {
            _updateTimer.Start();
            _autoSaveTimer.Start();

            // 起動イベントを発火
            _behaviorService.TriggerEvent(PetEventType.AppStarted);

            // デイリーログインボーナス
            if (_isFirstInteractionToday)
            {
                _bondService.AddPoints(BondPointSource.DailyLogin);
                _isFirstInteractionToday = false;
            }

            Debug.WriteLine("[CompanionshipManager] Started.");
        }

        /// <summary>
        /// システムを停止
        /// </summary>
        public void Stop()
        {
            _updateTimer.Stop();
            _autoSaveTimer.Stop();

            // 停止時に保存
            _dataService.Save(_context);

            Debug.WriteLine("[CompanionshipManager] Stopped.");
        }

        /// <summary>
        /// ユーザーとの交流を記録
        /// </summary>
        public void RecordInteraction()
        {
            // 長時間放置後の復帰チェック
            var timeSinceLastInteraction = _context.TimeSinceLastInteraction;
            var wasLongAbsence = timeSinceLastInteraction > TimeSpan.FromHours(2);

            // 交流を記録
            _emotionService.OnUserInteraction();
            _bondService.AddPoints(BondPointSource.ChatMessage);

            // 長時間放置後の復帰ボーナス
            if (wasLongAbsence)
            {
                _bondService.AddPoints(BondPointSource.ReturnBonus);
                _behaviorService.TriggerEvent(PetEventType.UserReturned);
            }
        }

        /// <summary>
        /// ポモドーロ完了を記録
        /// </summary>
        public void RecordPomodoroComplete()
        {
            _emotionService.OnPomodoroComplete();
            _bondService.AddPoints(BondPointSource.PomodoroComplete);
            _behaviorService.TriggerEvent(PetEventType.PomodoroComplete);
        }

        /// <summary>
        /// ゴール達成を記録
        /// </summary>
        public void RecordGoalAchieved()
        {
            _emotionService.OnGoalAchieved();
            _bondService.AddPoints(BondPointSource.GoalAchieved);
            _behaviorService.TriggerEvent(PetEventType.GoalAchieved);
        }

        /// <summary>
        /// 長い会話を記録（5往復以上）
        /// </summary>
        public void RecordLongConversation()
        {
            _bondService.AddPoints(BondPointSource.LongConversationBonus);
        }

        /// <summary>
        /// データを手動で保存
        /// </summary>
        public void SaveData()
        {
            _dataService.Save(_context);
        }

        /// <summary>
        /// 更新タイマーイベント
        /// </summary>
        private void OnUpdateTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            try
            {
                _emotionService.UpdateEmotion();
                _behaviorService.Update();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CompanionshipManager] Update error: {ex.Message}");
            }
        }

        /// <summary>
        /// 自動保存タイマーイベント
        /// </summary>
        private void OnAutoSaveTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            try
            {
                _dataService.Save(_context);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CompanionshipManager] Auto-save error: {ex.Message}");
            }
        }

        /// <summary>
        /// 絆レベルアップイベントハンドラ
        /// </summary>
        private void OnBondLevelUp(object? sender, BondLevelUpEventArgs e)
        {
            _behaviorService.TriggerEvent(PetEventType.BondLevelUp);
            Debug.WriteLine($"[CompanionshipManager] Bond level up: {e.PreviousLevel} -> {e.NewLevel}");
        }

        /// <summary>
        /// 感情変化イベントハンドラ
        /// </summary>
        private void OnEmotionChanged(object? sender, EmotionChangedEventArgs e)
        {
            _behaviorService.OnEmotionChanged(e.NewEmotion);
            Debug.WriteLine($"[CompanionshipManager] Emotion changed: {e.PreviousEmotion} -> {e.NewEmotion} ({e.Reason})");
        }

        /// <summary>
        /// リソースを解放
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;

            Stop();

            _bondService.LevelUp -= OnBondLevelUp;
            _emotionService.EmotionChanged -= OnEmotionChanged;

            _updateTimer.Dispose();
            _autoSaveTimer.Dispose();

            _isDisposed = true;

            Debug.WriteLine("[CompanionshipManager] Disposed.");
        }
    }
}
