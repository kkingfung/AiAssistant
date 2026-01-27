#nullable enable

namespace AiAssistant.Companionship
{
    /// <summary>
    /// ペットの鳴き声・効果音の種類
    /// </summary>
    public enum PetSoundType
    {
        // === 汎用 ===

        /// <summary>ゴロゴロ（満足）</summary>
        Purr,

        /// <summary>短い鳴き声（注目）</summary>
        Chirp,

        /// <summary>あくび（眠い）</summary>
        Yawn,

        /// <summary>寂しい鳴き声</summary>
        Whimper,

        // === 感情別 ===

        /// <summary>嬉しい時の音</summary>
        HappySound,

        /// <summary>寂しい時の音</summary>
        SadSound,

        /// <summary>興奮時の音</summary>
        ExcitedSound,

        // === イベント ===

        /// <summary>起動時・復帰時の挨拶音</summary>
        WelcomeSound,

        /// <summary>目標達成時のお祝い音</summary>
        CongratSound,

        /// <summary>注意を引く音（控えめ）</summary>
        AttentionSound
    }

    /// <summary>
    /// サウンド再生設定
    /// </summary>
    public sealed class PetSoundSettings
    {
        /// <summary>サウンド有効</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>マスター音量（0.0 - 1.0）</summary>
        public double MasterVolume { get; set; } = 0.5;

        /// <summary>環境音有効</summary>
        public bool AmbientSoundsEnabled { get; set; } = true;

        /// <summary>イベント音有効</summary>
        public bool EventSoundsEnabled { get; set; } = true;

        /// <summary>サウンド再生の最小間隔（秒）</summary>
        public int MinIntervalSeconds { get; set; } = 60;

        /// <summary>作業中はミュート</summary>
        public bool MuteWhileWorking { get; set; } = true;
    }

    /// <summary>
    /// 感情からサウンドへのマッピング
    /// </summary>
    public static class EmotionSoundMapper
    {
        /// <summary>
        /// 感情状態に適したサウンドを取得
        /// </summary>
        public static PetSoundType? GetSoundForEmotion(EmotionType emotion)
        {
            return emotion switch
            {
                EmotionType.Peaceful => PetSoundType.Purr,
                EmotionType.Happy => PetSoundType.HappySound,
                EmotionType.Lonely => PetSoundType.Whimper,
                EmotionType.Sleepy => PetSoundType.Yawn,
                EmotionType.Excited => PetSoundType.ExcitedSound,
                _ => null
            };
        }
    }
}
