using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;

namespace AiAssistant
{
    /// <summary>
    /// 音声出力（テキスト読み上げ）サービス
    /// Windows標準の音声合成機能を使用します
    /// </summary>
    public sealed class VoiceOutputService : IDisposable
    {
        private SpeechSynthesizer? _synthesizer;
        private bool _isAvailable;
        private bool _isSpeaking;

        public bool IsAvailable => _isAvailable;
        public bool IsSpeaking => _isSpeaking;

        /// <summary>
        /// 利用可能な音声のリスト
        /// </summary>
        public List<VoiceInfo> AvailableVoices { get; private set; } = new();

        /// <summary>
        /// 現在選択されている音声
        /// </summary>
        public string? CurrentVoice { get; private set; }

        /// <summary>
        /// 読み上げ速度 (-10 ~ 10)
        /// </summary>
        public int Rate
        {
            get => _synthesizer?.Rate ?? 0;
            set
            {
                if (_synthesizer != null)
                {
                    _synthesizer.Rate = Math.Clamp(value, -10, 10);
                }
            }
        }

        /// <summary>
        /// 音量 (0 ~ 100)
        /// </summary>
        public int Volume
        {
            get => _synthesizer?.Volume ?? 100;
            set
            {
                if (_synthesizer != null)
                {
                    _synthesizer.Volume = Math.Clamp(value, 0, 100);
                }
            }
        }

        /// <summary>
        /// 読み上げが開始されたときに発火
        /// </summary>
        public event EventHandler? SpeakStarted;

        /// <summary>
        /// 読み上げが完了したときに発火
        /// </summary>
        public event EventHandler? SpeakCompleted;

        public VoiceOutputService()
        {
            Initialize();
        }

        /// <summary>
        /// 音声合成エンジンを初期化します
        /// </summary>
        private void Initialize()
        {
            try
            {
                _synthesizer = new SpeechSynthesizer();

                // 利用可能な音声を取得
                foreach (var voice in _synthesizer.GetInstalledVoices())
                {
                    if (voice.Enabled)
                    {
                        AvailableVoices.Add(voice.VoiceInfo);
                        System.Diagnostics.Debug.WriteLine($"[VoiceOutput] 音声: {voice.VoiceInfo.Name} ({voice.VoiceInfo.Culture.DisplayName})");
                    }
                }

                // 日本語音声を優先的に選択
                var japaneseVoice = AvailableVoices.FirstOrDefault(v => v.Culture.Name.StartsWith("ja"));
                if (japaneseVoice != null)
                {
                    _synthesizer.SelectVoice(japaneseVoice.Name);
                    CurrentVoice = japaneseVoice.Name;
                }
                else if (AvailableVoices.Count > 0)
                {
                    CurrentVoice = AvailableVoices[0].Name;
                }

                // イベントハンドラを設定
                _synthesizer.SpeakStarted += OnSpeakStarted;
                _synthesizer.SpeakCompleted += OnSpeakCompleted;

                _isAvailable = AvailableVoices.Count > 0;
                System.Diagnostics.Debug.WriteLine($"[VoiceOutput] 初期化完了: {AvailableVoices.Count}個の音声が利用可能");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VoiceOutput] 初期化エラー: {ex.Message}");
                _isAvailable = false;
            }
        }

        /// <summary>
        /// テキストを読み上げます（非同期）
        /// </summary>
        public void SpeakAsync(string text)
        {
            if (!_isAvailable || _synthesizer == null || string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            try
            {
                // 前の読み上げをキャンセル
                if (_isSpeaking)
                {
                    _synthesizer.SpeakAsyncCancelAll();
                }

                _synthesizer.SpeakAsync(text);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VoiceOutput] 読み上げエラー: {ex.Message}");
            }
        }

        /// <summary>
        /// テキストを読み上げます（同期）
        /// </summary>
        public void Speak(string text)
        {
            if (!_isAvailable || _synthesizer == null || string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            try
            {
                _synthesizer.Speak(text);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VoiceOutput] 読み上げエラー: {ex.Message}");
            }
        }

        /// <summary>
        /// 読み上げを停止します
        /// </summary>
        public void Stop()
        {
            if (_synthesizer == null) return;

            try
            {
                _synthesizer.SpeakAsyncCancelAll();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VoiceOutput] 停止エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// 音声を選択します
        /// </summary>
        public bool SelectVoice(string voiceName)
        {
            if (_synthesizer == null) return false;

            try
            {
                _synthesizer.SelectVoice(voiceName);
                CurrentVoice = voiceName;
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VoiceOutput] 音声選択エラー: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 言語に基づいて音声を自動選択します
        /// </summary>
        public bool SelectVoiceByLanguage(string languageCode)
        {
            var voice = AvailableVoices.FirstOrDefault(v => v.Culture.Name.StartsWith(languageCode));
            if (voice != null)
            {
                return SelectVoice(voice.Name);
            }
            return false;
        }

        private void OnSpeakStarted(object? sender, SpeakStartedEventArgs e)
        {
            _isSpeaking = true;
            SpeakStarted?.Invoke(this, EventArgs.Empty);
        }

        private void OnSpeakCompleted(object? sender, SpeakCompletedEventArgs e)
        {
            _isSpeaking = false;
            SpeakCompleted?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 利用可能な音声の表示名リストを取得します
        /// </summary>
        public List<string> GetVoiceDisplayNames()
        {
            return AvailableVoices.Select(v => $"{v.Name} ({v.Culture.DisplayName})").ToList();
        }

        public void Dispose()
        {
            Stop();
            _synthesizer?.Dispose();
            _synthesizer = null;
        }
    }
}
