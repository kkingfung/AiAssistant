#nullable enable
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;
using AiAssistant.Companionship;

namespace AiAssistant.Character
{
    /// <summary>
    /// キャラクター対話サービスの実装
    /// </summary>
    public sealed class CharacterDialogueService : ICharacterDialogueService
    {
        private readonly IBondService _bondService;
        private readonly IEmotionService _emotionService;
        private readonly Random _random = new();

        private System.Timers.Timer? _idleTimer;
        private DateTime _lastDialogueTime = DateTime.MinValue;
        private bool _isDisposed;

        /// <summary>対話の基本表示時間（ミリ秒）</summary>
        private const int BaseDisplayDuration = 5000;

        /// <inheritdoc/>
        public CharacterPersonality CurrentPersonality { get; private set; }

        /// <inheritdoc/>
        public bool IsDialogueActive { get; private set; }

        /// <inheritdoc/>
        public event EventHandler<DialogueEventArgs>? DialogueRequested;

        /// <inheritdoc/>
        public event EventHandler? DialogueDismissed;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public CharacterDialogueService(IBondService bondService, IEmotionService emotionService)
        {
            _bondService = bondService ?? throw new ArgumentNullException(nameof(bondService));
            _emotionService = emotionService ?? throw new ArgumentNullException(nameof(emotionService));

            // デフォルトの性格を設定
            CurrentPersonality = CharacterPersonality.CreateGentle();

            Debug.WriteLine("[CharacterDialogue] サービスを初期化しました");
        }

        /// <inheritdoc/>
        public void SetPersonality(CharacterPersonality personality)
        {
            CurrentPersonality = personality ?? throw new ArgumentNullException(nameof(personality));
            Debug.WriteLine($"[CharacterDialogue] 性格を設定: {personality.Name} ({personality.Type})");
        }

        /// <inheritdoc/>
        public async Task<string> TriggerDialogueAsync(DialogueContext context)
        {
            var dialogue = GenerateDialogue(context);

            // 表示時間を計算（テキストの長さに応じて調整）
            var displayDuration = CalculateDisplayDuration(dialogue);

            // イベントを発火
            var args = new DialogueEventArgs(
                dialogue,
                CurrentPersonality.Name,
                context.Trigger,
                displayDuration,
                context.Emotion);

            IsDialogueActive = true;
            _lastDialogueTime = DateTime.Now;

            DialogueRequested?.Invoke(this, args);

            Debug.WriteLine($"[CharacterDialogue] 対話: {dialogue}");

            return await Task.FromResult(dialogue);
        }

        /// <inheritdoc/>
        public async Task ShowGreetingAsync()
        {
            var context = new DialogueContext(
                DialogueTrigger.Greeting,
                _emotionService.CurrentEmotionType,
                _bondService.CurrentLevel,
                DialogueContext.GetCurrentTimeOfDay());

            await TriggerDialogueAsync(context);
        }

        /// <inheritdoc/>
        public async Task ShowIdleChatAsync()
        {
            // 最後の対話から一定時間経過していない場合はスキップ
            if ((DateTime.Now - _lastDialogueTime).TotalSeconds < 30)
            {
                return;
            }

            var context = new DialogueContext(
                DialogueTrigger.IdleChat,
                _emotionService.CurrentEmotionType,
                _bondService.CurrentLevel,
                DialogueContext.GetCurrentTimeOfDay());

            await TriggerDialogueAsync(context);
        }

        /// <inheritdoc/>
        public async Task ShowTouchReactionAsync()
        {
            var context = new DialogueContext(
                DialogueTrigger.UserTouch,
                _emotionService.CurrentEmotionType,
                _bondService.CurrentLevel,
                DialogueContext.GetCurrentTimeOfDay());

            await TriggerDialogueAsync(context);
        }

        /// <inheritdoc/>
        public async Task ShowPomodoroDialogueAsync(bool isComplete, int sessionCount = 0)
        {
            var trigger = isComplete ? DialogueTrigger.PomodoroComplete : DialogueTrigger.PomodoroStart;

            var context = new DialogueContext(
                trigger,
                _emotionService.CurrentEmotionType,
                _bondService.CurrentLevel,
                DialogueContext.GetCurrentTimeOfDay(),
                extraParameter: sessionCount.ToString());

            await TriggerDialogueAsync(context);
        }

        /// <inheritdoc/>
        public async Task ShowBondLevelUpAsync(BondLevel newLevel)
        {
            var context = new DialogueContext(
                DialogueTrigger.BondLevelUp,
                EmotionType.Excited, // レベルアップ時は興奮
                newLevel,
                DialogueContext.GetCurrentTimeOfDay());

            await TriggerDialogueAsync(context);
        }

        /// <inheritdoc/>
        public async Task ShowLongAbsenceReactionAsync(TimeSpan absentDuration)
        {
            var context = new DialogueContext(
                DialogueTrigger.LongAbsence,
                EmotionType.Lonely, // 長時間放置後は寂しい
                _bondService.CurrentLevel,
                DialogueContext.GetCurrentTimeOfDay(),
                timeSinceLastDialogue: absentDuration);

            await TriggerDialogueAsync(context);
        }

        /// <inheritdoc/>
        public void DismissDialogue()
        {
            if (IsDialogueActive)
            {
                IsDialogueActive = false;
                DialogueDismissed?.Invoke(this, EventArgs.Empty);
                Debug.WriteLine("[CharacterDialogue] 対話を閉じました");
            }
        }

        /// <inheritdoc/>
        public void StartIdleDialogueTimer(int intervalMinutes = 5)
        {
            StopIdleDialogueTimer();

            _idleTimer = new System.Timers.Timer(intervalMinutes * 60 * 1000);
            _idleTimer.Elapsed += OnIdleTimerElapsed;
            _idleTimer.AutoReset = true;
            _idleTimer.Start();

            Debug.WriteLine($"[CharacterDialogue] アイドルタイマー開始: {intervalMinutes}分間隔");
        }

        /// <inheritdoc/>
        public void StopIdleDialogueTimer()
        {
            if (_idleTimer != null)
            {
                _idleTimer.Stop();
                _idleTimer.Elapsed -= OnIdleTimerElapsed;
                _idleTimer.Dispose();
                _idleTimer = null;

                Debug.WriteLine("[CharacterDialogue] アイドルタイマー停止");
            }
        }

        /// <summary>
        /// アイドルタイマーのイベントハンドラ
        /// </summary>
        private async void OnIdleTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            // ランダムで発言するかどうかを決定（50%の確率）
            if (_random.NextDouble() < 0.5)
            {
                await ShowIdleChatAsync();
            }
        }

        /// <summary>
        /// コンテキストに基づいてセリフを生成
        /// </summary>
        private string GenerateDialogue(DialogueContext context)
        {
            var personality = CurrentPersonality;
            var userAddress = personality.UserAddress;

            return context.Trigger switch
            {
                DialogueTrigger.Greeting => GenerateGreeting(context, personality),
                DialogueTrigger.IdleChat => GenerateIdleChat(personality),
                DialogueTrigger.UserTouch => GenerateTouchReaction(context, personality),
                DialogueTrigger.PomodoroComplete => GeneratePomodoroComplete(personality),
                DialogueTrigger.BondLevelUp => GenerateBondLevelUp(context, personality),
                DialogueTrigger.LongAbsence => GenerateLongAbsenceReaction(context, personality),
                _ => GenerateIdleChat(personality)
            };
        }

        /// <summary>
        /// 挨拶を生成
        /// </summary>
        private string GenerateGreeting(DialogueContext context, CharacterPersonality personality)
        {
            var greetings = DialogueTemplates.Greetings;
            var timeOfDay = context.TimeOfDay;

            // 定義されていない時間帯の場合、フォールバック
            if (!greetings[personality.Type].ContainsKey(timeOfDay))
            {
                timeOfDay = DialogueTemplates.GetDefaultTimeOfDay(timeOfDay);
            }

            if (!greetings[personality.Type].ContainsKey(timeOfDay))
            {
                timeOfDay = TimeOfDay.Morning; // 最終フォールバック
            }

            var templates = greetings[personality.Type][timeOfDay];
            return DialogueTemplates.GetDialogue(templates, personality, personality.UserAddress);
        }

        /// <summary>
        /// アイドル発言を生成
        /// </summary>
        private string GenerateIdleChat(CharacterPersonality personality)
        {
            var templates = DialogueTemplates.IdleChats[personality.Type];
            return DialogueTemplates.GetDialogue(templates, personality, personality.UserAddress);
        }

        /// <summary>
        /// タッチ反応を生成
        /// </summary>
        private string GenerateTouchReaction(DialogueContext context, CharacterPersonality personality)
        {
            var reactions = DialogueTemplates.TouchReactions;
            var bondLevel = context.BondLevel;

            var templates = reactions[personality.Type][bondLevel];
            return DialogueTemplates.GetDialogue(templates, personality, personality.UserAddress);
        }

        /// <summary>
        /// ポモドーロ完了メッセージを生成
        /// </summary>
        private string GeneratePomodoroComplete(CharacterPersonality personality)
        {
            var templates = DialogueTemplates.PomodoroComplete[personality.Type];
            return DialogueTemplates.GetDialogue(templates, personality, personality.UserAddress);
        }

        /// <summary>
        /// 絆レベルアップメッセージを生成
        /// </summary>
        private string GenerateBondLevelUp(DialogueContext context, CharacterPersonality personality)
        {
            var messages = DialogueTemplates.BondLevelUp;
            var template = messages[personality.Type][context.BondLevel];
            return template
                .Replace("{first}", personality.FirstPerson)
                .Replace("{user}", personality.UserAddress)
                .Replace("{name}", personality.Name);
        }

        /// <summary>
        /// 長時間放置後の反応を生成
        /// </summary>
        private string GenerateLongAbsenceReaction(DialogueContext context, CharacterPersonality personality)
        {
            var hours = context.TimeSinceLastDialogue.TotalHours;

            // 性格と放置時間に応じたメッセージ
            return personality.Type switch
            {
                PersonalityType.Cheerful => hours > 24
                    ? $"わーっ！{personality.UserAddress}！会いたかったよ〜！"
                    : $"おかえり〜！待ってたんだよ？",
                PersonalityType.Gentle => hours > 24
                    ? $"おかえりなさい、{personality.UserAddress}。ずっと待っていましたよ。"
                    : $"おかえりなさい。少し寂しかったです。",
                PersonalityType.Tsundere => hours > 24
                    ? $"…遅いわよ、バカ。心配したんだから…って、してないし！"
                    : $"ふーん、やっと来たんだ。…別に待ってないし。",
                PersonalityType.Airhead => hours > 24
                    ? $"わ〜い！ご主人さま！ココ、寂しかったのです〜！"
                    : $"おかえりなのです〜！えへへ、会えて嬉しい〜",
                PersonalityType.Diligent => hours > 24
                    ? $"おかえりなさいませ、マスター。長らくお待ちしておりました。"
                    : $"おかえりなさいませ。ご不在の間、準備を整えておりました。",
                _ => $"おかえりなさい。"
            };
        }

        /// <summary>
        /// テキストの長さに応じた表示時間を計算
        /// </summary>
        private static int CalculateDisplayDuration(string text)
        {
            // 基本5秒 + 10文字ごとに1秒追加、最大15秒
            var additionalTime = (text.Length / 10) * 1000;
            return Math.Min(BaseDisplayDuration + additionalTime, 15000);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_isDisposed) return;

            StopIdleDialogueTimer();
            _isDisposed = true;

            Debug.WriteLine("[CharacterDialogue] サービスを破棄しました");
        }
    }
}
