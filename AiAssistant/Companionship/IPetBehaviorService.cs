#nullable enable
using System;

namespace AiAssistant.Companionship
{
    /// <summary>
    /// アニメーション変更イベントの引数
    /// </summary>
    public sealed class AnimationChangedEventArgs : EventArgs
    {
        /// <summary>新しいアニメーション</summary>
        public PetAnimationType Animation { get; }

        /// <summary>優先度</summary>
        public AnimationPriority Priority { get; }

        public AnimationChangedEventArgs(PetAnimationType animation, AnimationPriority priority)
        {
            Animation = animation;
            Priority = priority;
        }
    }

    /// <summary>
    /// サウンド再生イベントの引数
    /// </summary>
    public sealed class SoundRequestedEventArgs : EventArgs
    {
        /// <summary>再生するサウンド</summary>
        public PetSoundType Sound { get; }

        /// <summary>音量（0.0-1.0）</summary>
        public double Volume { get; }

        public SoundRequestedEventArgs(PetSoundType sound, double volume = 1.0)
        {
            Sound = sound;
            Volume = volume;
        }
    }

    /// <summary>
    /// ペット行動サービスのインターフェース
    /// ペットの自律行動、アニメーション、サウンドを制御
    /// </summary>
    public interface IPetBehaviorService
    {
        /// <summary>現在のアニメーション</summary>
        PetAnimationType CurrentAnimation { get; }

        /// <summary>サウンド設定</summary>
        PetSoundSettings SoundSettings { get; }

        /// <summary>ユーザーが作業中かどうか</summary>
        bool UserIsWorking { get; set; }

        /// <summary>
        /// 行動を更新（定期的に呼び出す）
        /// </summary>
        void Update();

        /// <summary>
        /// 感情変化に応じた行動を実行
        /// </summary>
        /// <param name="emotion">新しい感情</param>
        void OnEmotionChanged(EmotionType emotion);

        /// <summary>
        /// イベントに応じた行動を実行
        /// </summary>
        /// <param name="eventType">イベントタイプ</param>
        void TriggerEvent(PetEventType eventType);

        /// <summary>
        /// アニメーション変更時に発火するイベント
        /// </summary>
        event EventHandler<AnimationChangedEventArgs>? AnimationChanged;

        /// <summary>
        /// サウンド再生要求時に発火するイベント
        /// </summary>
        event EventHandler<SoundRequestedEventArgs>? SoundRequested;
    }

    /// <summary>
    /// ペットイベントの種類
    /// </summary>
    public enum PetEventType
    {
        /// <summary>アプリ起動</summary>
        AppStarted,

        /// <summary>ユーザー復帰（長時間放置後）</summary>
        UserReturned,

        /// <summary>ポモドーロ完了</summary>
        PomodoroComplete,

        /// <summary>ゴール達成</summary>
        GoalAchieved,

        /// <summary>絆レベルアップ</summary>
        BondLevelUp,

        /// <summary>時間帯変化</summary>
        TimeOfDayChanged,

        /// <summary>長時間アイドル状態</summary>
        LongIdle
    }
}
