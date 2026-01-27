#nullable enable
using System;
using AiAssistant.Companionship;

namespace AiAssistant.Character
{
    /// <summary>
    /// 対話のトリガータイプ
    /// </summary>
    public enum DialogueTrigger
    {
        /// <summary>アプリ起動時の挨拶</summary>
        Greeting,

        /// <summary>アイドル時のランダム発言</summary>
        IdleChat,

        /// <summary>長時間放置後の反応</summary>
        LongAbsence,

        /// <summary>ユーザーがクリック/タップした時</summary>
        UserTouch,

        /// <summary>ポモドーロ開始時</summary>
        PomodoroStart,

        /// <summary>ポモドーロ完了時</summary>
        PomodoroComplete,

        /// <summary>休憩開始時</summary>
        BreakStart,

        /// <summary>休憩終了時</summary>
        BreakEnd,

        /// <summary>天気情報表示時</summary>
        WeatherInfo,

        /// <summary>絆レベルアップ時</summary>
        BondLevelUp,

        /// <summary>深夜の注意</summary>
        LateNightWarning,

        /// <summary>励まし</summary>
        Encouragement,

        /// <summary>お別れ（アプリ終了時）</summary>
        Farewell
    }

    /// <summary>
    /// 対話生成のコンテキスト情報
    /// </summary>
    public sealed class DialogueContext
    {
        /// <summary>対話のトリガー</summary>
        public DialogueTrigger Trigger { get; }

        /// <summary>現在の感情状態</summary>
        public EmotionType Emotion { get; }

        /// <summary>現在の絆レベル</summary>
        public BondLevel BondLevel { get; }

        /// <summary>現在の時間帯</summary>
        public TimeOfDay TimeOfDay { get; }

        /// <summary>追加パラメータ（天気情報、ポモドーロ回数など）</summary>
        public string? ExtraParameter { get; }

        /// <summary>前回の対話からの経過時間</summary>
        public TimeSpan TimeSinceLastDialogue { get; }

        public DialogueContext(
            DialogueTrigger trigger,
            EmotionType emotion,
            BondLevel bondLevel,
            TimeOfDay timeOfDay,
            string? extraParameter = null,
            TimeSpan? timeSinceLastDialogue = null)
        {
            Trigger = trigger;
            Emotion = emotion;
            BondLevel = bondLevel;
            TimeOfDay = timeOfDay;
            ExtraParameter = extraParameter;
            TimeSinceLastDialogue = timeSinceLastDialogue ?? TimeSpan.Zero;
        }

        /// <summary>
        /// 現在の時刻から時間帯を判定
        /// </summary>
        public static TimeOfDay GetCurrentTimeOfDay()
        {
            var hour = DateTime.Now.Hour;
            return hour switch
            {
                >= 5 and < 10 => TimeOfDay.Morning,
                >= 10 and < 12 => TimeOfDay.LateMorning,
                >= 12 and < 14 => TimeOfDay.Noon,
                >= 14 and < 17 => TimeOfDay.Afternoon,
                >= 17 and < 19 => TimeOfDay.Evening,
                >= 19 and < 22 => TimeOfDay.Night,
                _ => TimeOfDay.LateNight
            };
        }
    }

    /// <summary>
    /// 時間帯
    /// </summary>
    public enum TimeOfDay
    {
        /// <summary>早朝（5-10時）</summary>
        Morning,

        /// <summary>午前（10-12時）</summary>
        LateMorning,

        /// <summary>昼（12-14時）</summary>
        Noon,

        /// <summary>午後（14-17時）</summary>
        Afternoon,

        /// <summary>夕方（17-19時）</summary>
        Evening,

        /// <summary>夜（19-22時）</summary>
        Night,

        /// <summary>深夜（22-5時）</summary>
        LateNight
    }
}
