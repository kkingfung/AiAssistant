#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AiAssistant.Voice
{
    /// <summary>
    /// 音声合成サービスのインターフェース
    /// </summary>
    public interface IVoiceSynthesisService : IDisposable
    {
        /// <summary>
        /// サービスが利用可能かどうか
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// 利用可能な音声一覧
        /// </summary>
        IReadOnlyList<VoiceInfo> AvailableVoices { get; }

        /// <summary>
        /// 現在選択中の音声
        /// </summary>
        VoiceInfo? CurrentVoice { get; set; }

        /// <summary>
        /// 再生中かどうか
        /// </summary>
        bool IsSpeaking { get; }

        /// <summary>
        /// サービスを初期化
        /// </summary>
        Task InitializeAsync(CancellationToken ct = default);

        /// <summary>
        /// テキストを音声に変換（データのみ取得）
        /// </summary>
        /// <param name="text">読み上げるテキスト</param>
        /// <param name="ct">キャンセルトークン</param>
        /// <returns>合成結果</returns>
        Task<VoiceSynthesisResult> SynthesizeAsync(string text, CancellationToken ct = default);

        /// <summary>
        /// テキストを音声で再生（リップシンク連携あり）
        /// </summary>
        /// <param name="text">読み上げるテキスト</param>
        /// <param name="ct">キャンセルトークン</param>
        Task SpeakAsync(string text, CancellationToken ct = default);

        /// <summary>
        /// 再生を停止
        /// </summary>
        void Stop();

        /// <summary>
        /// リップシンク用音量更新イベント（再生中に定期的に発火）
        /// </summary>
        event EventHandler<LipSyncEventArgs>? LipSyncUpdate;

        /// <summary>
        /// 再生完了イベント
        /// </summary>
        event EventHandler? SpeakCompleted;

        /// <summary>
        /// エラーイベント
        /// </summary>
        event EventHandler<VoiceErrorEventArgs>? ErrorOccurred;
    }

    /// <summary>
    /// 音声情報
    /// </summary>
    public record VoiceInfo(
        string Id,
        string Name,
        string? Description,
        VoiceSynthesizerType SynthesizerType,
        string? SampleText = null
    );

    /// <summary>
    /// 音声合成エンジンの種類
    /// </summary>
    public enum VoiceSynthesizerType
    {
        /// <summary>VOICEVOX</summary>
        Voicevox,

        /// <summary>COEIROINK</summary>
        Coeiroink,

        /// <summary>Windows TTS</summary>
        WindowsTts,

        /// <summary>その他</summary>
        Other
    }

    /// <summary>
    /// 音声合成結果
    /// </summary>
    public record VoiceSynthesisResult(
        byte[] AudioData,
        TimeSpan Duration,
        int SampleRate,
        float[]? LipSyncData = null
    );

    /// <summary>
    /// リップシンクイベント引数
    /// </summary>
    public class LipSyncEventArgs : EventArgs
    {
        /// <summary>
        /// 口の開き具合（0.0〜1.0）
        /// </summary>
        public float Volume { get; }

        /// <summary>
        /// 現在の再生位置
        /// </summary>
        public TimeSpan Position { get; }

        public LipSyncEventArgs(float volume, TimeSpan position)
        {
            Volume = Math.Clamp(volume, 0f, 1f);
            Position = position;
        }
    }

    /// <summary>
    /// 音声エラーイベント引数
    /// </summary>
    public class VoiceErrorEventArgs : EventArgs
    {
        /// <summary>エラーメッセージ</summary>
        public string Message { get; }

        /// <summary>例外</summary>
        public Exception? Exception { get; }

        public VoiceErrorEventArgs(string message, Exception? exception = null)
        {
            Message = message;
            Exception = exception;
        }
    }
}
