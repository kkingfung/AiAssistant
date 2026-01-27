#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NAudio.Wave;

namespace AiAssistant.Audio
{
    /// <summary>
    /// BGM再生サービス（NAudio使用）
    /// </summary>
    public sealed class BgmService : IBgmService
    {
        private readonly List<BgmTrack> _tracks = new();
        private WaveOutEvent? _waveOut;
        private AudioFileReader? _audioReader;
        private int _currentIndex = -1;
        private float _volume = 0.3f;
        private bool _isDisposed;

        /// <inheritdoc/>
        public bool IsPlaying => _waveOut?.PlaybackState == PlaybackState.Playing;

        /// <inheritdoc/>
        public float Volume
        {
            get => _volume;
            set
            {
                _volume = Math.Clamp(value, 0f, 1f);
                if (_waveOut != null)
                {
                    _waveOut.Volume = _volume;
                }
            }
        }

        /// <inheritdoc/>
        public bool Loop { get; set; } = true;

        /// <inheritdoc/>
        public IReadOnlyList<BgmTrack> AvailableTracks => _tracks;

        /// <inheritdoc/>
        public BgmTrack? CurrentTrack => _currentIndex >= 0 && _currentIndex < _tracks.Count
            ? _tracks[_currentIndex]
            : null;

        /// <inheritdoc/>
        public event EventHandler<BgmTrack>? TrackChanged;

        /// <inheritdoc/>
        public event EventHandler<bool>? PlayStateChanged;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="bgmFolder">BGMフォルダのパス（オプション）</param>
        public BgmService(string? bgmFolder = null)
        {
            LoadTracks(bgmFolder);
        }

        /// <summary>
        /// BGMフォルダからトラックを読み込み
        /// </summary>
        private void LoadTracks(string? bgmFolder)
        {
            // デフォルトのBGMフォルダ
            var folder = bgmFolder ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BGM");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
                Debug.WriteLine($"[BGM] フォルダを作成: {folder}");
                return;
            }

            // mp3, wav, ogg, flac ファイルを検索
            var extensions = new[] { "*.mp3", "*.wav", "*.ogg", "*.flac" };
            foreach (var ext in extensions)
            {
                foreach (var file in Directory.GetFiles(folder, ext))
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    _tracks.Add(new BgmTrack(name, file));
                }
            }

            Debug.WriteLine($"[BGM] {_tracks.Count}曲を読み込み");
        }

        /// <summary>
        /// トラックを手動で追加
        /// </summary>
        public void AddTrack(BgmTrack track)
        {
            if (File.Exists(track.FilePath))
            {
                _tracks.Add(track);
            }
        }

        /// <inheritdoc/>
        public void Play(BgmTrack track)
        {
            var index = _tracks.IndexOf(track);
            if (index >= 0)
            {
                Play(index);
            }
        }

        /// <inheritdoc/>
        public void Play(int index)
        {
            if (index < 0 || index >= _tracks.Count) return;

            try
            {
                Stop();

                _currentIndex = index;
                var track = _tracks[index];

                _audioReader = new AudioFileReader(track.FilePath);
                _waveOut = new WaveOutEvent();
                _waveOut.Volume = _volume;
                _waveOut.Init(_audioReader);
                _waveOut.PlaybackStopped += OnPlaybackStopped;
                _waveOut.Play();

                TrackChanged?.Invoke(this, track);
                PlayStateChanged?.Invoke(this, true);

                Debug.WriteLine($"[BGM] 再生開始: {track.Name}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BGM] 再生エラー: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public void Pause()
        {
            if (_waveOut?.PlaybackState == PlaybackState.Playing)
            {
                _waveOut.Pause();
                PlayStateChanged?.Invoke(this, false);
                Debug.WriteLine("[BGM] 一時停止");
            }
        }

        /// <inheritdoc/>
        public void Resume()
        {
            if (_waveOut?.PlaybackState == PlaybackState.Paused)
            {
                _waveOut.Play();
                PlayStateChanged?.Invoke(this, true);
                Debug.WriteLine("[BGM] 再開");
            }
        }

        /// <inheritdoc/>
        public void Stop()
        {
            if (_waveOut != null)
            {
                _waveOut.PlaybackStopped -= OnPlaybackStopped;
                _waveOut.Stop();
                _waveOut.Dispose();
                _waveOut = null;
            }

            _audioReader?.Dispose();
            _audioReader = null;

            PlayStateChanged?.Invoke(this, false);
        }

        /// <inheritdoc/>
        public void Next()
        {
            if (_tracks.Count == 0) return;

            var nextIndex = (_currentIndex + 1) % _tracks.Count;
            Play(nextIndex);
        }

        /// <inheritdoc/>
        public void Previous()
        {
            if (_tracks.Count == 0) return;

            var prevIndex = _currentIndex - 1;
            if (prevIndex < 0) prevIndex = _tracks.Count - 1;
            Play(prevIndex);
        }

        /// <summary>
        /// 再生完了時のハンドラ
        /// </summary>
        private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
            {
                Debug.WriteLine($"[BGM] 再生エラー: {e.Exception.Message}");
                return;
            }

            // ループ再生の場合、次のトラックへ
            if (Loop && _tracks.Count > 0)
            {
                Next();
            }
            else
            {
                PlayStateChanged?.Invoke(this, false);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_isDisposed) return;

            Stop();
            _isDisposed = true;

            Debug.WriteLine("[BGM] サービスを破棄");
        }
    }
}
