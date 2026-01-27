#nullable enable
using System.Collections.Generic;

namespace AiAssistant.Companionship
{
    /// <summary>
    /// ペットアニメーションの種類
    /// </summary>
    public enum PetAnimationType
    {
        /// <summary>待機（通常）</summary>
        Idle,

        /// <summary>待機（嬉しい）</summary>
        IdleHappy,

        /// <summary>待機（眠い）</summary>
        IdleSleepy,

        /// <summary>待機（寂しい）</summary>
        IdleSad,

        /// <summary>挨拶モーション</summary>
        Greeting,

        /// <summary>お祝い（目標達成時）</summary>
        Celebrate,

        /// <summary>慰めモーション</summary>
        Comfort,

        /// <summary>一人遊び（ユーザー作業中）</summary>
        Playing
    }

    /// <summary>
    /// アニメーション優先度
    /// 高い優先度のアニメーションは低い優先度を上書きする
    /// </summary>
    public enum AnimationPriority
    {
        /// <summary>背景アニメーション（常時ループ）</summary>
        Background = 0,

        /// <summary>状態変化アニメーション</summary>
        StateChange = 1,

        /// <summary>イベントアニメーション（一時的）</summary>
        Event = 2,

        /// <summary>特別イベントアニメーション</summary>
        Special = 3
    }

    /// <summary>
    /// 感情からアニメーションへのマッピング
    /// </summary>
    public static class EmotionAnimationMapper
    {
        /// <summary>
        /// 感情状態に適したアニメーションを取得
        /// </summary>
        public static PetAnimationType GetAnimationForEmotion(EmotionType emotion)
        {
            return emotion switch
            {
                EmotionType.Peaceful => PetAnimationType.Idle,
                EmotionType.Happy => PetAnimationType.IdleHappy,
                EmotionType.Lonely => PetAnimationType.IdleSad,
                EmotionType.Sleepy => PetAnimationType.IdleSleepy,
                EmotionType.Excited => PetAnimationType.Celebrate,
                _ => PetAnimationType.Idle
            };
        }

        /// <summary>
        /// 作業中（非邪魔モード）用のアニメーションを取得
        /// </summary>
        public static IEnumerable<PetAnimationType> GetQuietAnimations()
        {
            yield return PetAnimationType.Idle;
            yield return PetAnimationType.IdleSleepy;
            yield return PetAnimationType.Playing;
        }

        /// <summary>
        /// アクティブ時のアニメーションを取得
        /// </summary>
        public static IEnumerable<PetAnimationType> GetActiveAnimations()
        {
            yield return PetAnimationType.Idle;
            yield return PetAnimationType.IdleHappy;
            yield return PetAnimationType.Playing;
            yield return PetAnimationType.Greeting;
        }
    }
}
