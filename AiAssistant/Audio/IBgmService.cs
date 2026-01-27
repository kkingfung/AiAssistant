#nullable enable
using System;
using System.Collections.Generic;

namespace AiAssistant.Audio
{
    /// <summary>
    /// BGMサービスのインターフェース
    /// </summary>
    public interface IBgmService : IDisposable
    {
        /// <summary>再生中かどうか</summary>
        bool IsPlaying { get; }

        /// <summary>現在の音量（0.0-1.0）</summary>
        float Volume { get; set; }

        /// <summary>ループ再生するかどうか</summary>
        bool Loop { get; set; }

        /// <summary>利用可能なトラック一覧</summary>
        IReadOnlyList<BgmTrack> AvailableTracks { get; }

        /// <summary>現在のトラック</summary>
        BgmTrack? CurrentTrack { get; }

        /// <summary>
        /// 指定トラックを再生
        /// </summary>
        void Play(BgmTrack track);

        /// <summary>
        /// 指定インデックスのトラックを再生
        /// </summary>
        void Play(int index);

        /// <summary>
        /// 再生を一時停止
        /// </summary>
        void Pause();

        /// <summary>
        /// 再生を再開
        /// </summary>
        void Resume();

        /// <summary>
        /// 再生を停止
        /// </summary>
        void Stop();

        /// <summary>
        /// 次のトラックへ
        /// </summary>
        void Next();

        /// <summary>
        /// 前のトラックへ
        /// </summary>
        void Previous();

        /// <summary>
        /// トラック変更時のイベント
        /// </summary>
        event EventHandler<BgmTrack>? TrackChanged;

        /// <summary>
        /// 再生状態変更時のイベント
        /// </summary>
        event EventHandler<bool>? PlayStateChanged;
    }

    /// <summary>
    /// BGMトラック情報
    /// </summary>
    public sealed class BgmTrack
    {
        /// <summary>トラック名</summary>
        public string Name { get; }

        /// <summary>ファイルパス</summary>
        public string FilePath { get; }

        /// <summary>アーティスト名（オプション）</summary>
        public string? Artist { get; }

        public BgmTrack(string name, string filePath, string? artist = null)
        {
            Name = name;
            FilePath = filePath;
            Artist = artist;
        }

        public override string ToString() => Artist != null ? $"{Name} - {Artist}" : Name;
    }
}
