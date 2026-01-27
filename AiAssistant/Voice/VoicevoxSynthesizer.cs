#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using NAudio.Wave;

namespace AiAssistant.Voice
{
    /// <summary>
    /// VOICEVOX音声合成サービス
    /// ローカルで動作するVOICEVOXエンジンと連携
    /// </summary>
    public sealed class VoicevoxSynthesizer : IVoiceSynthesisService
    {
        private readonly HttpClient _httpClient;
        private readonly LipSyncAnalyzer _lipSyncAnalyzer;
        private readonly string _baseUrl;

        private List<VoiceInfo> _availableVoices = new();
        private WaveOutEvent? _waveOut;
        private RawSourceWaveStream? _audioStream;
        private System.Timers.Timer? _lipSyncTimer;
        private float[]? _currentLipSyncData;
        private int _lipSyncIndex;
        private DateTime _playbackStartTime;

        private bool _isDisposed;

        /// <inheritdoc/>
        public bool IsAvailable { get; private set; }

        /// <inheritdoc/>
        public IReadOnlyList<VoiceInfo> AvailableVoices => _availableVoices;

        /// <inheritdoc/>
        public VoiceInfo? CurrentVoice { get; set; }

        /// <inheritdoc/>
        public bool IsSpeaking => _waveOut?.PlaybackState == PlaybackState.Playing;

        /// <inheritdoc/>
        public event EventHandler<LipSyncEventArgs>? LipSyncUpdate;

        /// <inheritdoc/>
        public event EventHandler? SpeakCompleted;

        /// <inheritdoc/>
        public event EventHandler<VoiceErrorEventArgs>? ErrorOccurred;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="baseUrl">VOICEVOXエンジンのURL（デフォルト: http://localhost:50021）</param>
        public VoicevoxSynthesizer(string baseUrl = "http://localhost:50021")
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            _lipSyncAnalyzer = new LipSyncAnalyzer();
        }

        /// <inheritdoc/>
        public async Task InitializeAsync(CancellationToken ct = default)
        {
            try
            {
                // VOICEVOXエンジンの起動確認
                var versionResponse = await _httpClient.GetAsync($"{_baseUrl}/version", ct);
                if (!versionResponse.IsSuccessStatusCode)
                {
                    Debug.WriteLine("[VOICEVOX] エンジンに接続できません");
                    IsAvailable = false;
                    return;
                }

                var version = await versionResponse.Content.ReadAsStringAsync(ct);
                Debug.WriteLine($"[VOICEVOX] エンジン版本: {version}");

                // 利用可能な話者を取得
                await LoadSpeakersAsync(ct);

                // デフォルト話者を設定
                if (_availableVoices.Count > 0 && CurrentVoice == null)
                {
                    CurrentVoice = _availableVoices[0];
                }

                IsAvailable = true;
                Debug.WriteLine($"[VOICEVOX] 初期化完了 - {_availableVoices.Count}人の話者が利用可能");
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"[VOICEVOX] 接続エラー: {ex.Message}");
                Debug.WriteLine("[VOICEVOX] VOICEVOXが起動しているか確認してください");
                IsAvailable = false;
                ErrorOccurred?.Invoke(this, new VoiceErrorEventArgs(
                    "VOICEVOXエンジンに接続できません。VOICEVOXを起動してください。", ex));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VOICEVOX] 初期化エラー: {ex.Message}");
                IsAvailable = false;
                ErrorOccurred?.Invoke(this, new VoiceErrorEventArgs("初期化に失敗しました", ex));
            }
        }

        /// <summary>
        /// 利用可能な話者を読み込み
        /// </summary>
        private async Task LoadSpeakersAsync(CancellationToken ct)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/speakers", ct);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);

                _availableVoices.Clear();

                foreach (var speaker in doc.RootElement.EnumerateArray())
                {
                    var speakerName = speaker.GetProperty("name").GetString() ?? "Unknown";
                    var styles = speaker.GetProperty("styles");

                    foreach (var style in styles.EnumerateArray())
                    {
                        var styleId = style.GetProperty("id").GetInt32();
                        var styleName = style.GetProperty("name").GetString() ?? "ノーマル";

                        _availableVoices.Add(new VoiceInfo(
                            Id: styleId.ToString(),
                            Name: $"{speakerName}（{styleName}）",
                            Description: null,
                            SynthesizerType: VoiceSynthesizerType.Voicevox
                        ));
                    }
                }

                Debug.WriteLine($"[VOICEVOX] {_availableVoices.Count}スタイルを読み込み");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VOICEVOX] 話者読み込みエラー: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<VoiceSynthesisResult> SynthesizeAsync(string text, CancellationToken ct = default)
        {
            if (!IsAvailable || CurrentVoice == null)
            {
                throw new InvalidOperationException("VOICEVOXが利用できません");
            }

            var speakerId = int.Parse(CurrentVoice.Id);

            try
            {
                // 1. 音声クエリを作成
                var queryUrl = $"{_baseUrl}/audio_query?text={Uri.EscapeDataString(text)}&speaker={speakerId}";
                var queryResponse = await _httpClient.PostAsync(queryUrl, null, ct);
                queryResponse.EnsureSuccessStatusCode();
                var queryJson = await queryResponse.Content.ReadAsStringAsync(ct);

                // 2. 音声合成
                var synthesisUrl = $"{_baseUrl}/synthesis?speaker={speakerId}";
                var synthesisResponse = await _httpClient.PostAsync(
                    synthesisUrl,
                    new StringContent(queryJson, Encoding.UTF8, "application/json"),
                    ct);
                synthesisResponse.EnsureSuccessStatusCode();

                var audioData = await synthesisResponse.Content.ReadAsByteArrayAsync(ct);

                // 3. リップシンクデータを生成
                var lipSyncData = _lipSyncAnalyzer.AnalyzeWavData(audioData);

                // 4. 音声長を計算
                var duration = _lipSyncAnalyzer.GetAudioDuration(audioData);

                Debug.WriteLine($"[VOICEVOX] 合成完了: {text.Length}文字 -> {duration.TotalSeconds:F2}秒");

                return new VoiceSynthesisResult(
                    AudioData: audioData,
                    Duration: duration,
                    SampleRate: 24000, // VOICEVOXのデフォルト
                    LipSyncData: lipSyncData
                );
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VOICEVOX] 合成エラー: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task SpeakAsync(string text, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            try
            {
                // 既存の再生を停止
                Stop();

                // 音声を合成
                var result = await SynthesizeAsync(text, ct);

                // リップシンクデータを保存
                _currentLipSyncData = result.LipSyncData;
                _lipSyncIndex = 0;

                // NAudioで再生
                _audioStream = new RawSourceWaveStream(
                    new System.IO.MemoryStream(result.AudioData),
                    new WaveFormat(result.SampleRate, 16, 1));

                _waveOut = new WaveOutEvent();
                _waveOut.Init(_audioStream);
                _waveOut.PlaybackStopped += OnPlaybackStopped;

                // リップシンクタイマーを開始（50ms間隔）
                _lipSyncTimer = new System.Timers.Timer(50);
                _lipSyncTimer.Elapsed += OnLipSyncTimerElapsed;
                _playbackStartTime = DateTime.Now;

                _waveOut.Play();
                _lipSyncTimer.Start();

                Debug.WriteLine($"[VOICEVOX] 再生開始: {text}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VOICEVOX] 再生エラー: {ex.Message}");
                ErrorOccurred?.Invoke(this, new VoiceErrorEventArgs("再生に失敗しました", ex));
            }
        }

        /// <summary>
        /// リップシンクタイマーイベント
        /// </summary>
        private void OnLipSyncTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            if (_currentLipSyncData == null || _lipSyncIndex >= _currentLipSyncData.Length)
            {
                LipSyncUpdate?.Invoke(this, new LipSyncEventArgs(0f, TimeSpan.Zero));
                return;
            }

            var volume = _currentLipSyncData[_lipSyncIndex];
            var position = DateTime.Now - _playbackStartTime;

            LipSyncUpdate?.Invoke(this, new LipSyncEventArgs(volume, position));
            _lipSyncIndex++;
        }

        /// <summary>
        /// 再生完了イベント
        /// </summary>
        private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            _lipSyncTimer?.Stop();

            // 口を閉じる
            LipSyncUpdate?.Invoke(this, new LipSyncEventArgs(0f, TimeSpan.Zero));

            SpeakCompleted?.Invoke(this, EventArgs.Empty);
            Debug.WriteLine("[VOICEVOX] 再生完了");
        }

        /// <inheritdoc/>
        public void Stop()
        {
            _lipSyncTimer?.Stop();
            _lipSyncTimer?.Dispose();
            _lipSyncTimer = null;

            if (_waveOut != null)
            {
                _waveOut.PlaybackStopped -= OnPlaybackStopped;
                _waveOut.Stop();
                _waveOut.Dispose();
                _waveOut = null;
            }

            _audioStream?.Dispose();
            _audioStream = null;

            _currentLipSyncData = null;
            _lipSyncIndex = 0;

            // 口を閉じる
            LipSyncUpdate?.Invoke(this, new LipSyncEventArgs(0f, TimeSpan.Zero));
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_isDisposed) return;

            Stop();
            _httpClient.Dispose();

            _isDisposed = true;
            Debug.WriteLine("[VOICEVOX] サービスを破棄");
        }
    }
}
