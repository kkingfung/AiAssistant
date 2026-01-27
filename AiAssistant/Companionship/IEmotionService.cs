#nullable enable
using System;

namespace AiAssistant.Companionship
{
    /// <summary>
    /// 感情変化イベントの引数
    /// </summary>
    public sealed class EmotionChangedEventArgs : EventArgs
    {
        /// <summary>以前の感情</summary>
        public EmotionType PreviousEmotion { get; }

        /// <summary>新しい感情</summary>
        public EmotionType NewEmotion { get; }

        /// <summary>変化の理由</summary>
        public string Reason { get; }

        public EmotionChangedEventArgs(EmotionType previousEmotion, EmotionType newEmotion, string reason)
        {
            PreviousEmotion = previousEmotion;
            NewEmotion = newEmotion;
            Reason = reason;
        }
    }

    /// <summary>
    /// 感情システムのインターフェース
    /// ペットの感情状態を管理
    /// </summary>
    public interface IEmotionService
    {
        /// <summary>現在の感情状態</summary>
        EmotionState CurrentEmotion { get; }

        /// <summary>現在の感情タイプ</summary>
        EmotionType CurrentEmotionType { get; }

        /// <summary>
        /// 感情を更新（時間帯、放置時間などを考慮）
        /// </summary>
        void UpdateEmotion();

        /// <summary>
        /// 特定の感情に変更
        /// </summary>
        /// <param name="emotion">新しい感情</param>
        /// <param name="reason">変化の理由</param>
        /// <param name="intensity">強度（0.0-1.0）</param>
        void SetEmotion(EmotionType emotion, string reason, double intensity = 1.0);

        /// <summary>
        /// ユーザーとの交流による感情変化
        /// </summary>
        void OnUserInteraction();

        /// <summary>
        /// ポモドーロ完了による感情変化
        /// </summary>
        void OnPomodoroComplete();

        /// <summary>
        /// ゴール達成による感情変化
        /// </summary>
        void OnGoalAchieved();

        /// <summary>
        /// 感情変化時に発火するイベント
        /// </summary>
        event EventHandler<EmotionChangedEventArgs>? EmotionChanged;
    }
}
