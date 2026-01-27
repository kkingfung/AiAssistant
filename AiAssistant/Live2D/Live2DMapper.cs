#nullable enable
using AiAssistant.Companionship;

namespace AiAssistant.Live2D
{
    /// <summary>
    /// 感情タイプからLive2D表情名へのマッピング
    /// </summary>
    public static class Live2DExpressionMapper
    {
        /// <summary>
        /// 感情タイプに対応するLive2D表情名を取得
        /// </summary>
        /// <param name="emotion">感情タイプ</param>
        /// <returns>Live2D表情名</returns>
        public static string GetExpression(EmotionType emotion) => emotion switch
        {
            EmotionType.Happy => "happy",
            EmotionType.Peaceful => "neutral",
            EmotionType.Lonely => "sad",
            EmotionType.Sleepy => "sleepy",
            EmotionType.Excited => "excited",
            _ => "neutral"
        };

        /// <summary>
        /// 感情タイプに対応する日本語名を取得
        /// </summary>
        public static string GetJapaneseName(EmotionType emotion) => emotion switch
        {
            EmotionType.Happy => "嬉しい",
            EmotionType.Peaceful => "穏やか",
            EmotionType.Lonely => "寂しい",
            EmotionType.Sleepy => "眠い",
            EmotionType.Excited => "興奮",
            _ => "穏やか"
        };
    }

    /// <summary>
    /// 行動タイプからLive2Dモーションへのマッピング
    /// </summary>
    public static class Live2DMotionMapper
    {
        /// <summary>
        /// 感情タイプに対応するアイドルモーションを取得
        /// </summary>
        /// <param name="emotion">感情タイプ</param>
        /// <returns>モーショングループ名とインデックス</returns>
        public static (string group, int index) GetIdleMotion(EmotionType emotion) => emotion switch
        {
            EmotionType.Happy => ("Idle", 1),      // 嬉しそうなアイドル
            EmotionType.Sleepy => ("Idle", 2),     // 眠そうなアイドル
            EmotionType.Lonely => ("Idle", 3),     // 寂しそうなアイドル
            EmotionType.Excited => ("Idle", 4),    // 興奮したアイドル
            _ => ("Idle", 0)                       // 通常アイドル
        };

        /// <summary>
        /// ペットアニメーションタイプに対応するモーションを取得
        /// </summary>
        /// <param name="animation">ペットアニメーションタイプ</param>
        /// <returns>モーショングループ名とインデックス</returns>
        public static (string group, int index) GetMotion(PetAnimationType animation) => animation switch
        {
            PetAnimationType.Idle => ("Idle", 0),
            PetAnimationType.IdleHappy => ("Idle", 1),
            PetAnimationType.IdleSleepy => ("Idle", 2),
            PetAnimationType.IdleSad => ("Idle", 3),
            PetAnimationType.Greeting => ("Greeting", 0),
            PetAnimationType.Celebrate => ("Celebrate", 0),
            PetAnimationType.Comfort => ("Comfort", 0),
            PetAnimationType.Playing => ("Playing", 0),
            _ => ("Idle", 0)
        };

        /// <summary>
        /// イベントタイプに対応するモーションを取得
        /// </summary>
        /// <param name="eventType">ペットイベントタイプ</param>
        /// <returns>モーショングループ名とインデックス、優先度</returns>
        public static (string group, int index, MotionPriority priority) GetEventMotion(PetEventType eventType) => eventType switch
        {
            PetEventType.AppStarted => ("Greeting", 0, MotionPriority.Normal),
            PetEventType.UserReturned => ("Greeting", 1, MotionPriority.Normal),
            PetEventType.PomodoroComplete => ("Celebrate", 0, MotionPriority.Force),
            PetEventType.GoalAchieved => ("Celebrate", 1, MotionPriority.Force),
            PetEventType.BondLevelUp => ("Celebrate", 2, MotionPriority.Force),
            PetEventType.LongIdle => ("Idle", 5, MotionPriority.Idle),
            _ => ("Idle", 0, MotionPriority.Idle)
        };
    }

    /// <summary>
    /// Live2Dモデルの標準パラメータID
    /// </summary>
    public static class Live2DParameterIds
    {
        // 頭の回転
        public const string AngleX = "ParamAngleX";
        public const string AngleY = "ParamAngleY";
        public const string AngleZ = "ParamAngleZ";

        // 体の回転
        public const string BodyAngleX = "ParamBodyAngleX";
        public const string BodyAngleY = "ParamBodyAngleY";
        public const string BodyAngleZ = "ParamBodyAngleZ";

        // 目
        public const string EyeLOpen = "ParamEyeLOpen";
        public const string EyeROpen = "ParamEyeROpen";
        public const string EyeBallX = "ParamEyeBallX";
        public const string EyeBallY = "ParamEyeBallY";

        // 眉
        public const string BrowLY = "ParamBrowLY";
        public const string BrowRY = "ParamBrowRY";
        public const string BrowLAngle = "ParamBrowLAngle";
        public const string BrowRAngle = "ParamBrowRAngle";

        // 口
        public const string MouthOpenY = "ParamMouthOpenY";
        public const string MouthForm = "ParamMouthForm";

        // 呼吸
        public const string Breath = "ParamBreath";
    }
}
