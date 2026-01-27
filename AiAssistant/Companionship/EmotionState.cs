#nullable enable
using System;

namespace AiAssistant.Companionship
{
    /// <summary>
    /// ペットの感情状態
    /// </summary>
    public enum EmotionType
    {
        /// <summary>穏やか - 通常状態、作業見守り中</summary>
        Peaceful,

        /// <summary>嬉しい - ユーザーとの交流時、目標達成時</summary>
        Happy,

        /// <summary>寂しい - 長時間放置後</summary>
        Lonely,

        /// <summary>眠い - 深夜帯</summary>
        Sleepy,

        /// <summary>興奮 - 特別なイベント時</summary>
        Excited
    }

    /// <summary>
    /// 感情状態の詳細情報
    /// </summary>
    public sealed class EmotionState
    {
        /// <summary>感情タイプ</summary>
        public EmotionType Type { get; private set; }

        /// <summary>感情の強度（0.0 - 1.0）</summary>
        public double Intensity { get; private set; }

        /// <summary>この感情になった時刻</summary>
        public DateTime StartedAt { get; private set; }

        /// <summary>感情の持続時間</summary>
        public TimeSpan Duration => DateTime.Now - StartedAt;

        /// <summary>
        /// 新しい感情状態を作成
        /// </summary>
        public EmotionState(EmotionType type, double intensity = 1.0)
        {
            Type = type;
            Intensity = Math.Clamp(intensity, 0.0, 1.0);
            StartedAt = DateTime.Now;
        }

        /// <summary>
        /// 感情を変更
        /// </summary>
        public void ChangeTo(EmotionType newType, double intensity = 1.0)
        {
            Type = newType;
            Intensity = Math.Clamp(intensity, 0.0, 1.0);
            StartedAt = DateTime.Now;
        }

        /// <summary>
        /// 強度を調整
        /// </summary>
        public void AdjustIntensity(double delta)
        {
            Intensity = Math.Clamp(Intensity + delta, 0.0, 1.0);
        }

        /// <summary>
        /// 感情の日本語名を取得
        /// </summary>
        public string GetJapaneseName()
        {
            return Type switch
            {
                EmotionType.Peaceful => "穏やか",
                EmotionType.Happy => "嬉しい",
                EmotionType.Lonely => "寂しい",
                EmotionType.Sleepy => "眠い",
                EmotionType.Excited => "興奮",
                _ => "不明"
            };
        }

        /// <summary>
        /// 感情の絵文字を取得
        /// </summary>
        public string GetEmoji()
        {
            return Type switch
            {
                EmotionType.Peaceful => "😌",
                EmotionType.Happy => "😊",
                EmotionType.Lonely => "😢",
                EmotionType.Sleepy => "😴",
                EmotionType.Excited => "🎉",
                _ => "😐"
            };
        }

        /// <summary>
        /// デフォルトの感情状態（穏やか）を作成
        /// </summary>
        public static EmotionState CreateDefault()
        {
            return new EmotionState(EmotionType.Peaceful);
        }
    }
}
