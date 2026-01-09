using System;
using System.Timers;
using System.Windows;
using System.Media;
using Timer = System.Timers.Timer;

namespace AiAssistant
{
    /// <summary>
    /// ポモドーロタイマーの状態
    /// </summary>
    public enum PomodoroState
    {
        Stopped,
        Working,
        ShortBreak,
        LongBreak,
        Paused
    }

    /// <summary>
    /// ポモドーロタイマーサービス
    /// 25分作業 → 5分休憩のサイクルを管理します
    /// </summary>
    public sealed class PomodoroService : IDisposable
    {
        private Timer? _timer;
        private int _remainingSeconds;
        private int _completedPomodoros;
        private PomodoroState _state = PomodoroState.Stopped;
        private PomodoroState _stateBeforePause = PomodoroState.Stopped;

        // デフォルト設定（分）
        public int WorkDuration { get; set; } = 25;
        public int ShortBreakDuration { get; set; } = 5;
        public int LongBreakDuration { get; set; } = 15;
        public int PomodorosUntilLongBreak { get; set; } = 4;

        public PomodoroState State => _state;
        public int RemainingSeconds => _remainingSeconds;
        public int CompletedPomodoros => _completedPomodoros;
        public TimeSpan RemainingTime => TimeSpan.FromSeconds(_remainingSeconds);

        /// <summary>
        /// タイマーが更新されたときに発火
        /// </summary>
        public event EventHandler<PomodoroTickEventArgs>? Tick;

        /// <summary>
        /// 状態が変化したときに発火
        /// </summary>
        public event EventHandler<PomodoroStateChangedEventArgs>? StateChanged;

        /// <summary>
        /// ポモドーロが完了したときに発火
        /// </summary>
        public event EventHandler<PomodoroCompletedEventArgs>? PomodoroCompleted;

        public PomodoroService()
        {
            _timer = new Timer(1000); // 1秒ごと
            _timer.Elapsed += OnTimerElapsed;
        }

        /// <summary>
        /// 作業タイマーを開始します
        /// </summary>
        public void StartWork()
        {
            _remainingSeconds = WorkDuration * 60;
            SetState(PomodoroState.Working);
            _timer?.Start();
        }

        /// <summary>
        /// 休憩タイマーを開始します
        /// </summary>
        public void StartBreak()
        {
            bool isLongBreak = _completedPomodoros > 0 && _completedPomodoros % PomodorosUntilLongBreak == 0;
            _remainingSeconds = (isLongBreak ? LongBreakDuration : ShortBreakDuration) * 60;
            SetState(isLongBreak ? PomodoroState.LongBreak : PomodoroState.ShortBreak);
            _timer?.Start();
        }

        /// <summary>
        /// タイマーを一時停止します
        /// </summary>
        public void Pause()
        {
            if (_state == PomodoroState.Working || _state == PomodoroState.ShortBreak || _state == PomodoroState.LongBreak)
            {
                _stateBeforePause = _state;
                _timer?.Stop();
                SetState(PomodoroState.Paused);
            }
        }

        /// <summary>
        /// タイマーを再開します
        /// </summary>
        public void Resume()
        {
            if (_state == PomodoroState.Paused)
            {
                SetState(_stateBeforePause);
                _timer?.Start();
            }
        }

        /// <summary>
        /// タイマーを停止してリセットします
        /// </summary>
        public void Stop()
        {
            _timer?.Stop();
            _remainingSeconds = 0;
            SetState(PomodoroState.Stopped);
        }

        /// <summary>
        /// 完了したポモドーロ数をリセットします
        /// </summary>
        public void ResetCount()
        {
            _completedPomodoros = 0;
        }

        /// <summary>
        /// カスタムリマインダーを設定します
        /// </summary>
        public void SetReminder(int minutes, string message, Action<string> callback)
        {
            var reminderTimer = new Timer(minutes * 60 * 1000);
            reminderTimer.AutoReset = false;
            reminderTimer.Elapsed += (s, e) =>
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    callback(message);
                });
                reminderTimer.Dispose();
            };
            reminderTimer.Start();
        }

        private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            _remainingSeconds--;

            Application.Current?.Dispatcher.Invoke(() =>
            {
                Tick?.Invoke(this, new PomodoroTickEventArgs(_remainingSeconds, _state));
            });

            if (_remainingSeconds <= 0)
            {
                _timer?.Stop();
                OnTimerCompleted();
            }
        }

        private void OnTimerCompleted()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var previousState = _state;

                if (previousState == PomodoroState.Working)
                {
                    _completedPomodoros++;
                    PomodoroCompleted?.Invoke(this, new PomodoroCompletedEventArgs(_completedPomodoros, true));

                    // 通知音を鳴らす
                    PlayNotificationSound();
                }
                else if (previousState == PomodoroState.ShortBreak || previousState == PomodoroState.LongBreak)
                {
                    PomodoroCompleted?.Invoke(this, new PomodoroCompletedEventArgs(_completedPomodoros, false));

                    // 通知音を鳴らす
                    PlayNotificationSound();
                }

                SetState(PomodoroState.Stopped);
            });
        }

        private void SetState(PomodoroState newState)
        {
            var oldState = _state;
            _state = newState;
            StateChanged?.Invoke(this, new PomodoroStateChangedEventArgs(oldState, newState));
        }

        private void PlayNotificationSound()
        {
            try
            {
                SystemSounds.Exclamation.Play();
            }
            catch
            {
                // 音声再生に失敗しても無視
            }
        }

        /// <summary>
        /// 状態を表す文字列を取得します
        /// </summary>
        public string GetStateDisplayText()
        {
            return _state switch
            {
                PomodoroState.Working => "🍅 作業中",
                PomodoroState.ShortBreak => "☕ 小休憩",
                PomodoroState.LongBreak => "🌴 大休憩",
                PomodoroState.Paused => "⏸️ 一時停止",
                PomodoroState.Stopped => "⏹️ 停止",
                _ => "不明"
            };
        }

        /// <summary>
        /// 残り時間を表示用文字列で取得します
        /// </summary>
        public string GetTimeDisplayText()
        {
            var time = TimeSpan.FromSeconds(_remainingSeconds);
            return $"{time.Minutes:D2}:{time.Seconds:D2}";
        }

        public void Dispose()
        {
            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;
        }
    }

    /// <summary>
    /// タイマー更新イベント引数
    /// </summary>
    public class PomodoroTickEventArgs : EventArgs
    {
        public int RemainingSeconds { get; }
        public PomodoroState State { get; }

        public PomodoroTickEventArgs(int remainingSeconds, PomodoroState state)
        {
            RemainingSeconds = remainingSeconds;
            State = state;
        }
    }

    /// <summary>
    /// 状態変化イベント引数
    /// </summary>
    public class PomodoroStateChangedEventArgs : EventArgs
    {
        public PomodoroState OldState { get; }
        public PomodoroState NewState { get; }

        public PomodoroStateChangedEventArgs(PomodoroState oldState, PomodoroState newState)
        {
            OldState = oldState;
            NewState = newState;
        }
    }

    /// <summary>
    /// ポモドーロ完了イベント引数
    /// </summary>
    public class PomodoroCompletedEventArgs : EventArgs
    {
        public int TotalCompleted { get; }
        public bool WasWorkSession { get; }

        public PomodoroCompletedEventArgs(int totalCompleted, bool wasWorkSession)
        {
            TotalCompleted = totalCompleted;
            WasWorkSession = wasWorkSession;
        }
    }
}
