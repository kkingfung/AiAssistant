#nullable enable
using System;
using System.Text.Json.Serialization;

namespace AiAssistant.Companionship
{
    /// <summary>
    /// 時間帯の区分
    /// </summary>
    public enum TimeOfDay
    {
        /// <summary>早朝（5:00-7:59）</summary>
        EarlyMorning,

        /// <summary>朝（8:00-11:59）</summary>
        Morning,

        /// <summary>昼（12:00-14:59）</summary>
        Afternoon,

        /// <summary>夕方（15:00-17:59）</summary>
        Evening,

        /// <summary>夜（18:00-22:59）</summary>
        Night,

        /// <summary>深夜（23:00-4:59）</summary>
        LateNight
    }

    /// <summary>
    /// コンパニオンの状態コンテキスト
    /// 絆レベル、感情、環境情報などを統合して管理
    /// </summary>
    public sealed class CompanionContext
    {
        // === ペット情報 ===

        /// <summary>ペットの種類</summary>
        public PetType PetType { get; set; } = PetType.Dragon;

        // === 絆情報 ===

        /// <summary>現在の絆ポイント</summary>
        public int BondPoints { get; set; } = 0;

        /// <summary>現在の絆レベル</summary>
        [JsonIgnore]
        public BondLevel BondLevel => BondLevelInfo.CalculateLevel(BondPoints);

        /// <summary>絆レベルの日本語名</summary>
        [JsonIgnore]
        public string BondLevelName => BondLevelInfo.GetInfo(BondLevel).Name;

        /// <summary>次のレベルまでのポイント</summary>
        [JsonIgnore]
        public int PointsToNextLevel => BondLevelInfo.GetPointsToNextLevel(BondPoints);

        // === 感情情報 ===

        /// <summary>現在の感情タイプ（永続化用）</summary>
        public EmotionType CurrentEmotionType { get; set; } = EmotionType.Peaceful;

        /// <summary>感情の強度</summary>
        public double EmotionIntensity { get; set; } = 1.0;

        /// <summary>現在の感情状態</summary>
        [JsonIgnore]
        public EmotionState Emotion
        {
            get => new EmotionState(CurrentEmotionType, EmotionIntensity);
            set
            {
                CurrentEmotionType = value.Type;
                EmotionIntensity = value.Intensity;
            }
        }

        // === 時間情報 ===

        /// <summary>最後の交流時刻</summary>
        public DateTime LastInteraction { get; set; } = DateTime.Now;

        /// <summary>最後の交流からの経過時間</summary>
        [JsonIgnore]
        public TimeSpan TimeSinceLastInteraction => DateTime.Now - LastInteraction;

        /// <summary>現在の時間帯</summary>
        [JsonIgnore]
        public TimeOfDay TimeOfDay => GetTimeOfDay(DateTime.Now);

        /// <summary>時間帯の日本語名</summary>
        [JsonIgnore]
        public string TimeOfDayName => GetTimeOfDayName(TimeOfDay);

        // === 環境情報 ===

        /// <summary>天気情報（WeatherServiceから取得）</summary>
        [JsonIgnore]
        public string? Weather { get; set; }

        /// <summary>ユーザーが作業中かどうか</summary>
        [JsonIgnore]
        public bool UserIsWorking { get; set; } = false;

        // === 統計情報 ===

        /// <summary>総会話数</summary>
        public int TotalConversations { get; set; } = 0;

        /// <summary>完了したポモドーロ数</summary>
        public int PomodorosCompleted { get; set; } = 0;

        /// <summary>達成したゴール数</summary>
        public int GoalsAchieved { get; set; } = 0;

        /// <summary>アクティブ日数</summary>
        public int DaysActive { get; set; } = 0;

        /// <summary>最後のアクティブ日</summary>
        public DateTime LastActiveDate { get; set; } = DateTime.Today;

        // === メソッド ===

        /// <summary>
        /// 交流を記録
        /// </summary>
        public void RecordInteraction()
        {
            LastInteraction = DateTime.Now;
            TotalConversations++;

            // 日付が変わっていたらアクティブ日数を増やす
            if (LastActiveDate.Date != DateTime.Today)
            {
                DaysActive++;
                LastActiveDate = DateTime.Today;
            }
        }

        /// <summary>
        /// 絆ポイントを追加
        /// </summary>
        /// <returns>レベルアップした場合はtrue</returns>
        public bool AddBondPoints(int points)
        {
            var previousLevel = BondLevel;
            BondPoints += points;
            return BondLevel > previousLevel;
        }

        /// <summary>
        /// 時刻から時間帯を取得
        /// </summary>
        private static TimeOfDay GetTimeOfDay(DateTime time)
        {
            var hour = time.Hour;
            return hour switch
            {
                >= 5 and < 8 => TimeOfDay.EarlyMorning,
                >= 8 and < 12 => TimeOfDay.Morning,
                >= 12 and < 15 => TimeOfDay.Afternoon,
                >= 15 and < 18 => TimeOfDay.Evening,
                >= 18 and < 23 => TimeOfDay.Night,
                _ => TimeOfDay.LateNight
            };
        }

        /// <summary>
        /// 時間帯の日本語名を取得
        /// </summary>
        private static string GetTimeOfDayName(TimeOfDay timeOfDay)
        {
            return timeOfDay switch
            {
                TimeOfDay.EarlyMorning => "早朝",
                TimeOfDay.Morning => "朝",
                TimeOfDay.Afternoon => "昼",
                TimeOfDay.Evening => "夕方",
                TimeOfDay.Night => "夜",
                TimeOfDay.LateNight => "深夜",
                _ => "不明"
            };
        }

        /// <summary>
        /// デフォルトのコンテキストを作成
        /// </summary>
        public static CompanionContext CreateDefault()
        {
            return new CompanionContext
            {
                PetType = PetType.Dragon,
                BondPoints = 0,
                CurrentEmotionType = EmotionType.Peaceful,
                EmotionIntensity = 1.0,
                LastInteraction = DateTime.Now,
                TotalConversations = 0,
                PomodorosCompleted = 0,
                GoalsAchieved = 0,
                DaysActive = 1,
                LastActiveDate = DateTime.Today
            };
        }
    }
}
