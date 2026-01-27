#nullable enable
using System;
using System.Diagnostics;

namespace AiAssistant.Companionship
{
    /// <summary>
    /// 感情システムの実装
    /// ペットの感情状態を管理
    /// </summary>
    public sealed class EmotionService : IEmotionService
    {
        private readonly CompanionContext _context;

        // 放置判定の閾値
        private static readonly TimeSpan LonelyThreshold = TimeSpan.FromHours(2);
        private static readonly TimeSpan VeryLonelyThreshold = TimeSpan.FromHours(6);

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="context">コンパニオンコンテキスト</param>
        public EmotionService(CompanionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc/>
        public EmotionState CurrentEmotion => _context.Emotion;

        /// <inheritdoc/>
        public EmotionType CurrentEmotionType => _context.CurrentEmotionType;

        /// <inheritdoc/>
        public event EventHandler<EmotionChangedEventArgs>? EmotionChanged;

        /// <inheritdoc/>
        public void UpdateEmotion()
        {
            var newEmotion = CalculateEmotion();

            if (newEmotion != CurrentEmotionType)
            {
                SetEmotionInternal(newEmotion, "自動更新", 1.0);
            }
        }

        /// <inheritdoc/>
        public void SetEmotion(EmotionType emotion, string reason, double intensity = 1.0)
        {
            SetEmotionInternal(emotion, reason, intensity);
        }

        /// <inheritdoc/>
        public void OnUserInteraction()
        {
            _context.RecordInteraction();

            // 交流時は嬉しい感情に
            if (CurrentEmotionType == EmotionType.Lonely)
            {
                // 寂しかった状態からの復帰
                SetEmotionInternal(EmotionType.Happy, "ユーザー復帰", 1.0);
            }
            else if (CurrentEmotionType != EmotionType.Excited)
            {
                // 通常は嬉しい状態に
                SetEmotionInternal(EmotionType.Happy, "ユーザー交流", 0.8);
            }
        }

        /// <inheritdoc/>
        public void OnPomodoroComplete()
        {
            // ポモドーロ完了時は興奮
            SetEmotionInternal(EmotionType.Excited, "ポモドーロ完了", 1.0);
            _context.PomodorosCompleted++;
        }

        /// <inheritdoc/>
        public void OnGoalAchieved()
        {
            // ゴール達成時は興奮
            SetEmotionInternal(EmotionType.Excited, "ゴール達成", 1.0);
            _context.GoalsAchieved++;
        }

        /// <summary>
        /// 現在の状態から感情を計算
        /// </summary>
        private EmotionType CalculateEmotion()
        {
            // 深夜は眠い
            if (_context.TimeOfDay == TimeOfDay.LateNight)
            {
                return EmotionType.Sleepy;
            }

            // 長時間放置されている場合は寂しい
            var timeSinceInteraction = _context.TimeSinceLastInteraction;

            if (timeSinceInteraction > VeryLonelyThreshold)
            {
                return EmotionType.Lonely;
            }

            if (timeSinceInteraction > LonelyThreshold)
            {
                // 2-6時間放置：軽い寂しさ
                return EmotionType.Lonely;
            }

            // 早朝は眠い
            if (_context.TimeOfDay == TimeOfDay.EarlyMorning)
            {
                return EmotionType.Sleepy;
            }

            // それ以外は穏やか
            return EmotionType.Peaceful;
        }

        /// <summary>
        /// 感情変更の内部処理
        /// </summary>
        private void SetEmotionInternal(EmotionType newEmotion, string reason, double intensity)
        {
            var previousEmotion = CurrentEmotionType;

            if (previousEmotion == newEmotion)
            {
                // 同じ感情の場合は強度のみ更新
                _context.EmotionIntensity = Math.Max(_context.EmotionIntensity, intensity);
                return;
            }

            _context.CurrentEmotionType = newEmotion;
            _context.EmotionIntensity = intensity;

            Debug.WriteLine($"[EmotionService] Emotion changed: {previousEmotion} -> {newEmotion} ({reason})");

            // イベント発火
            var args = new EmotionChangedEventArgs(previousEmotion, newEmotion, reason);
            EmotionChanged?.Invoke(this, args);
        }
    }
}
