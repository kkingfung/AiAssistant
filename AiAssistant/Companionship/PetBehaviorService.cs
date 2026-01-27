#nullable enable
using System;
using System.Diagnostics;

namespace AiAssistant.Companionship
{
    /// <summary>
    /// ペット行動サービスの実装
    /// ペットの自律行動、アニメーション、サウンドを制御
    /// </summary>
    public sealed class PetBehaviorService : IPetBehaviorService
    {
        private readonly CompanionContext _context;
        private readonly Random _random = new();

        private PetAnimationType _currentAnimation = PetAnimationType.Idle;
        private DateTime _lastSoundTime = DateTime.MinValue;
        private TimeOfDay _lastTimeOfDay;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="context">コンパニオンコンテキスト</param>
        public PetBehaviorService(CompanionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _lastTimeOfDay = context.TimeOfDay;
            SoundSettings = new PetSoundSettings();
        }

        /// <inheritdoc/>
        public PetAnimationType CurrentAnimation => _currentAnimation;

        /// <inheritdoc/>
        public PetSoundSettings SoundSettings { get; }

        /// <inheritdoc/>
        public bool UserIsWorking
        {
            get => _context.UserIsWorking;
            set => _context.UserIsWorking = value;
        }

        /// <inheritdoc/>
        public event EventHandler<AnimationChangedEventArgs>? AnimationChanged;

        /// <inheritdoc/>
        public event EventHandler<SoundRequestedEventArgs>? SoundRequested;

        /// <inheritdoc/>
        public void Update()
        {
            // 時間帯変化をチェック
            var currentTimeOfDay = _context.TimeOfDay;
            if (currentTimeOfDay != _lastTimeOfDay)
            {
                _lastTimeOfDay = currentTimeOfDay;
                TriggerEvent(PetEventType.TimeOfDayChanged);
            }

            // 作業中かどうかでアニメーションを選択
            if (UserIsWorking)
            {
                UpdateQuietBehavior();
            }
            else
            {
                UpdateActiveBehavior();
            }
        }

        /// <inheritdoc/>
        public void OnEmotionChanged(EmotionType emotion)
        {
            var animation = EmotionAnimationMapper.GetAnimationForEmotion(emotion);
            SetAnimation(animation, AnimationPriority.StateChange);

            // 感情に応じたサウンドを再生（控えめに）
            if (ShouldPlaySound())
            {
                var sound = EmotionSoundMapper.GetSoundForEmotion(emotion);
                if (sound.HasValue)
                {
                    RequestSound(sound.Value, 0.3);
                }
            }
        }

        /// <inheritdoc/>
        public void TriggerEvent(PetEventType eventType)
        {
            Debug.WriteLine($"[PetBehaviorService] Event triggered: {eventType}");

            switch (eventType)
            {
                case PetEventType.AppStarted:
                    SetAnimation(PetAnimationType.Greeting, AnimationPriority.Event);
                    RequestSound(PetSoundType.WelcomeSound, 0.5);
                    break;

                case PetEventType.UserReturned:
                    SetAnimation(PetAnimationType.Greeting, AnimationPriority.Event);
                    RequestSound(PetSoundType.WelcomeSound, 0.4);
                    break;

                case PetEventType.PomodoroComplete:
                    SetAnimation(PetAnimationType.Celebrate, AnimationPriority.Event);
                    RequestSound(PetSoundType.CongratSound, 0.6);
                    break;

                case PetEventType.GoalAchieved:
                    SetAnimation(PetAnimationType.Celebrate, AnimationPriority.Event);
                    RequestSound(PetSoundType.CongratSound, 0.7);
                    break;

                case PetEventType.BondLevelUp:
                    SetAnimation(PetAnimationType.Celebrate, AnimationPriority.Special);
                    RequestSound(PetSoundType.ExcitedSound, 0.8);
                    break;

                case PetEventType.TimeOfDayChanged:
                    // 時間帯変化時は穏やかに更新
                    var animation = GetAnimationForTimeOfDay(_context.TimeOfDay);
                    SetAnimation(animation, AnimationPriority.Background);
                    break;
            }
        }

        /// <summary>
        /// 作業中の静かな行動
        /// </summary>
        private void UpdateQuietBehavior()
        {
            // 作業中は静かなアニメーションのみ
            var emotion = _context.CurrentEmotionType;

            var animation = emotion switch
            {
                EmotionType.Sleepy => PetAnimationType.IdleSleepy,
                EmotionType.Peaceful => ChooseRandomQuietAnimation(),
                _ => PetAnimationType.Idle
            };

            SetAnimation(animation, AnimationPriority.Background);

            // 作業中はサウンドを控える
            if (SoundSettings.MuteWhileWorking)
            {
                return;
            }

            // 控えめな環境音のみ
            if (ShouldPlayAmbientSound())
            {
                RequestSound(PetSoundType.Purr, 0.1);
            }
        }

        /// <summary>
        /// アクティブ時の行動
        /// </summary>
        private void UpdateActiveBehavior()
        {
            var emotion = _context.CurrentEmotionType;
            var animation = EmotionAnimationMapper.GetAnimationForEmotion(emotion);

            // たまにランダムな行動
            if (_random.NextDouble() < 0.1) // 10%の確率
            {
                animation = ChooseRandomActiveAnimation();
            }

            SetAnimation(animation, AnimationPriority.Background);
        }

        /// <summary>
        /// 時間帯に応じたアニメーションを取得
        /// </summary>
        private static PetAnimationType GetAnimationForTimeOfDay(TimeOfDay timeOfDay)
        {
            return timeOfDay switch
            {
                TimeOfDay.LateNight => PetAnimationType.IdleSleepy,
                TimeOfDay.EarlyMorning => PetAnimationType.IdleSleepy,
                _ => PetAnimationType.Idle
            };
        }

        /// <summary>
        /// ランダムな静かなアニメーションを選択
        /// </summary>
        private PetAnimationType ChooseRandomQuietAnimation()
        {
            var options = new[] { PetAnimationType.Idle, PetAnimationType.IdleSleepy, PetAnimationType.Playing };
            return options[_random.Next(options.Length)];
        }

        /// <summary>
        /// ランダムなアクティブアニメーションを選択
        /// </summary>
        private PetAnimationType ChooseRandomActiveAnimation()
        {
            var options = new[] { PetAnimationType.Idle, PetAnimationType.IdleHappy, PetAnimationType.Playing };
            return options[_random.Next(options.Length)];
        }

        /// <summary>
        /// アニメーションを設定
        /// </summary>
        private void SetAnimation(PetAnimationType animation, AnimationPriority priority)
        {
            if (_currentAnimation == animation)
            {
                return;
            }

            _currentAnimation = animation;
            Debug.WriteLine($"[PetBehaviorService] Animation changed to: {animation}");

            AnimationChanged?.Invoke(this, new AnimationChangedEventArgs(animation, priority));
        }

        /// <summary>
        /// サウンドを再生要求
        /// </summary>
        private void RequestSound(PetSoundType sound, double volume)
        {
            if (!SoundSettings.Enabled)
            {
                return;
            }

            var adjustedVolume = volume * SoundSettings.MasterVolume;
            Debug.WriteLine($"[PetBehaviorService] Sound requested: {sound} (volume: {adjustedVolume:F2})");

            _lastSoundTime = DateTime.Now;
            SoundRequested?.Invoke(this, new SoundRequestedEventArgs(sound, adjustedVolume));
        }

        /// <summary>
        /// サウンドを再生すべきかどうか
        /// </summary>
        private bool ShouldPlaySound()
        {
            if (!SoundSettings.Enabled || !SoundSettings.EventSoundsEnabled)
            {
                return false;
            }

            if (UserIsWorking && SoundSettings.MuteWhileWorking)
            {
                return false;
            }

            var timeSinceLastSound = DateTime.Now - _lastSoundTime;
            return timeSinceLastSound.TotalSeconds >= SoundSettings.MinIntervalSeconds;
        }

        /// <summary>
        /// 環境音を再生すべきかどうか
        /// </summary>
        private bool ShouldPlayAmbientSound()
        {
            if (!SoundSettings.Enabled || !SoundSettings.AmbientSoundsEnabled)
            {
                return false;
            }

            var timeSinceLastSound = DateTime.Now - _lastSoundTime;
            return timeSinceLastSound.TotalSeconds >= SoundSettings.MinIntervalSeconds * 2;
        }
    }
}
