using System;
using System.Globalization;
using System.Threading.Tasks;

#if WINDOWS
using System.Speech.Recognition;
#endif

namespace AiAssistant
{
    /// <summary>
    /// 音声認識の状態
    /// </summary>
    public enum VoiceInputState
    {
        Stopped,
        Listening,
        Processing
    }

    /// <summary>
    /// 音声入力サービス
    /// Windows標準の音声認識機能を使用します
    /// </summary>
    public sealed class VoiceInputService : IDisposable
    {
#if WINDOWS
        private SpeechRecognitionEngine? _recognizer;
#endif
        private VoiceInputState _state = VoiceInputState.Stopped;
        private bool _isAvailable;

        public VoiceInputState State => _state;
        public bool IsAvailable => _isAvailable;

        /// <summary>
        /// 音声認識が完了したときに発火
        /// </summary>
        public event EventHandler<VoiceRecognizedEventArgs>? Recognized;

        /// <summary>
        /// 音声認識の状態が変化したときに発火
        /// </summary>
        public event EventHandler<VoiceInputState>? StateChanged;

        /// <summary>
        /// エラーが発生したときに発火
        /// </summary>
        public event EventHandler<string>? Error;

        public VoiceInputService()
        {
            Initialize();
        }

        /// <summary>
        /// 音声認識エンジンを初期化します
        /// </summary>
        private void Initialize()
        {
#if WINDOWS
            try
            {
                // 日本語の音声認識エンジンを検索
                RecognizerInfo? recognizerInfo = null;

                foreach (var info in SpeechRecognitionEngine.InstalledRecognizers())
                {
                    // 日本語を優先
                    if (info.Culture.Name.StartsWith("ja"))
                    {
                        recognizerInfo = info;
                        break;
                    }
                }

                // 日本語がなければデフォルトを使用
                if (recognizerInfo == null)
                {
                    var defaultInfo = SpeechRecognitionEngine.InstalledRecognizers();
                    if (defaultInfo.Count > 0)
                    {
                        recognizerInfo = defaultInfo[0];
                    }
                }

                if (recognizerInfo == null)
                {
                    System.Diagnostics.Debug.WriteLine("[VoiceInput] 音声認識エンジンが見つかりません");
                    _isAvailable = false;
                    return;
                }

                _recognizer = new SpeechRecognitionEngine(recognizerInfo);

                // 自由発話用の文法をロード（ディクテーション）
                _recognizer.LoadGrammar(new DictationGrammar());

                // イベントハンドラを設定
                _recognizer.SpeechRecognized += OnSpeechRecognized;
                _recognizer.SpeechRecognitionRejected += OnSpeechRejected;
                _recognizer.RecognizeCompleted += OnRecognizeCompleted;

                // デフォルトのオーディオデバイスを使用
                _recognizer.SetInputToDefaultAudioDevice();

                _isAvailable = true;
                System.Diagnostics.Debug.WriteLine($"[VoiceInput] 初期化完了: {recognizerInfo.Culture.DisplayName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VoiceInput] 初期化エラー: {ex.Message}");
                _isAvailable = false;
            }
#else
            _isAvailable = false;
            System.Diagnostics.Debug.WriteLine("[VoiceInput] Windowsプラットフォーム以外では利用できません");
#endif
        }

        /// <summary>
        /// 音声認識を開始します（一度だけ認識）
        /// </summary>
        public void StartListening()
        {
#if WINDOWS
            if (!_isAvailable || _recognizer == null)
            {
                Error?.Invoke(this, "音声認識が利用できません");
                return;
            }

            if (_state == VoiceInputState.Listening)
            {
                return;
            }

            try
            {
                SetState(VoiceInputState.Listening);
                _recognizer.RecognizeAsync(RecognizeMode.Single);
                System.Diagnostics.Debug.WriteLine("[VoiceInput] 認識開始");
            }
            catch (Exception ex)
            {
                SetState(VoiceInputState.Stopped);
                Error?.Invoke(this, $"音声認識開始エラー: {ex.Message}");
            }
#else
            Error?.Invoke(this, "音声認識はWindowsでのみ利用可能です");
#endif
        }

        /// <summary>
        /// 音声認識を停止します
        /// </summary>
        public void StopListening()
        {
#if WINDOWS
            if (_recognizer == null || _state == VoiceInputState.Stopped)
            {
                return;
            }

            try
            {
                _recognizer.RecognizeAsyncCancel();
                SetState(VoiceInputState.Stopped);
                System.Diagnostics.Debug.WriteLine("[VoiceInput] 認識停止");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VoiceInput] 停止エラー: {ex.Message}");
            }
#endif
        }

#if WINDOWS
        private void OnSpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
        {
            SetState(VoiceInputState.Processing);

            var text = e.Result.Text;
            var confidence = e.Result.Confidence;

            System.Diagnostics.Debug.WriteLine($"[VoiceInput] 認識成功: {text} (信頼度: {confidence:P0})");

            Recognized?.Invoke(this, new VoiceRecognizedEventArgs(text, confidence));
        }

        private void OnSpeechRejected(object? sender, SpeechRecognitionRejectedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[VoiceInput] 認識失敗");
            Error?.Invoke(this, "音声を認識できませんでした。もう一度お試しください。");
        }

        private void OnRecognizeCompleted(object? sender, RecognizeCompletedEventArgs e)
        {
            SetState(VoiceInputState.Stopped);

            if (e.Error != null)
            {
                Error?.Invoke(this, $"認識エラー: {e.Error.Message}");
            }
        }
#endif

        private void SetState(VoiceInputState newState)
        {
            _state = newState;
            StateChanged?.Invoke(this, newState);
        }

        /// <summary>
        /// 状態を表す文字列を取得します
        /// </summary>
        public string GetStateDisplayText()
        {
            return _state switch
            {
                VoiceInputState.Listening => "🎤 聞いています...",
                VoiceInputState.Processing => "🔄 処理中...",
                VoiceInputState.Stopped => "🎤 音声入力",
                _ => "不明"
            };
        }

        public void Dispose()
        {
#if WINDOWS
            StopListening();
            _recognizer?.Dispose();
            _recognizer = null;
#endif
        }
    }

    /// <summary>
    /// 音声認識完了イベント引数
    /// </summary>
    public class VoiceRecognizedEventArgs : EventArgs
    {
        public string Text { get; }
        public float Confidence { get; }

        public VoiceRecognizedEventArgs(string text, float confidence)
        {
            Text = text;
            Confidence = confidence;
        }
    }
}
