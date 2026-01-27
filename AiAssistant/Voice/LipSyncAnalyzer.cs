#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace AiAssistant.Voice
{
    /// <summary>
    /// 音声データからリップシンク用のデータを生成
    /// WAV形式の音声データを分析し、口の開き具合を時系列で出力
    /// </summary>
    public sealed class LipSyncAnalyzer
    {
        /// <summary>
        /// 分析のフレーム間隔（ミリ秒）
        /// </summary>
        public int FrameIntervalMs { get; set; } = 50;

        /// <summary>
        /// 音量のスムージング係数（0〜1、大きいほど滑らか）
        /// </summary>
        public float SmoothingFactor { get; set; } = 0.3f;

        /// <summary>
        /// 最小音量閾値（これ以下は0として扱う）
        /// </summary>
        public float MinVolumeThreshold { get; set; } = 0.05f;

        /// <summary>
        /// WAVデータからリップシンクデータを生成
        /// </summary>
        /// <param name="wavData">WAV形式のバイトデータ</param>
        /// <returns>各フレームの口の開き具合（0.0〜1.0）の配列</returns>
        public float[] AnalyzeWavData(byte[] wavData)
        {
            try
            {
                // WAVヘッダーを解析
                if (!TryParseWavHeader(wavData, out var sampleRate, out var bitsPerSample, out var channels, out var dataOffset))
                {
                    Debug.WriteLine("[LipSync] WAVヘッダーの解析に失敗");
                    return Array.Empty<float>();
                }

                // サンプルデータを抽出
                var samples = ExtractSamples(wavData, dataOffset, bitsPerSample, channels);
                if (samples.Length == 0)
                {
                    return Array.Empty<float>();
                }

                // フレームごとの音量を計算
                var samplesPerFrame = (int)(sampleRate * FrameIntervalMs / 1000.0);
                var frameCount = samples.Length / samplesPerFrame;

                if (frameCount == 0)
                {
                    return Array.Empty<float>();
                }

                var volumes = new List<float>(frameCount);
                float prevVolume = 0f;

                for (int i = 0; i < frameCount; i++)
                {
                    var startIndex = i * samplesPerFrame;
                    var endIndex = Math.Min(startIndex + samplesPerFrame, samples.Length);

                    // RMS（二乗平均平方根）で音量を計算
                    var rms = CalculateRms(samples, startIndex, endIndex);

                    // 正規化（0〜1）
                    var normalizedVolume = NormalizeVolume(rms);

                    // 閾値以下は0に
                    if (normalizedVolume < MinVolumeThreshold)
                    {
                        normalizedVolume = 0f;
                    }

                    // スムージング
                    var smoothedVolume = prevVolume * SmoothingFactor + normalizedVolume * (1 - SmoothingFactor);
                    prevVolume = smoothedVolume;

                    volumes.Add(smoothedVolume);
                }

                Debug.WriteLine($"[LipSync] 分析完了: {frameCount}フレーム");
                return volumes.ToArray();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LipSync] 分析エラー: {ex.Message}");
                return Array.Empty<float>();
            }
        }

        /// <summary>
        /// WAVデータから音声の長さを取得
        /// </summary>
        public TimeSpan GetAudioDuration(byte[] wavData)
        {
            try
            {
                if (!TryParseWavHeader(wavData, out var sampleRate, out var bitsPerSample, out var channels, out var dataOffset))
                {
                    return TimeSpan.Zero;
                }

                var dataLength = wavData.Length - dataOffset;
                var bytesPerSample = bitsPerSample / 8;
                var totalSamples = dataLength / (bytesPerSample * channels);
                var durationSeconds = (double)totalSamples / sampleRate;

                return TimeSpan.FromSeconds(durationSeconds);
            }
            catch
            {
                return TimeSpan.Zero;
            }
        }

        /// <summary>
        /// WAVヘッダーを解析
        /// </summary>
        private static bool TryParseWavHeader(byte[] data, out int sampleRate, out int bitsPerSample, out int channels, out int dataOffset)
        {
            sampleRate = 0;
            bitsPerSample = 0;
            channels = 0;
            dataOffset = 0;

            if (data.Length < 44)
            {
                return false;
            }

            // RIFFヘッダーチェック
            if (data[0] != 'R' || data[1] != 'I' || data[2] != 'F' || data[3] != 'F')
            {
                return false;
            }

            // WAVEフォーマットチェック
            if (data[8] != 'W' || data[9] != 'A' || data[10] != 'V' || data[11] != 'E')
            {
                return false;
            }

            // fmtチャンクを探す
            int pos = 12;
            while (pos < data.Length - 8)
            {
                var chunkId = System.Text.Encoding.ASCII.GetString(data, pos, 4);
                var chunkSize = BitConverter.ToInt32(data, pos + 4);

                if (chunkId == "fmt ")
                {
                    channels = BitConverter.ToInt16(data, pos + 10);
                    sampleRate = BitConverter.ToInt32(data, pos + 12);
                    bitsPerSample = BitConverter.ToInt16(data, pos + 22);
                }
                else if (chunkId == "data")
                {
                    dataOffset = pos + 8;
                    return sampleRate > 0 && bitsPerSample > 0 && channels > 0;
                }

                pos += 8 + chunkSize;
            }

            return false;
        }

        /// <summary>
        /// WAVデータからサンプルを抽出（モノラル、float正規化）
        /// </summary>
        private static float[] ExtractSamples(byte[] wavData, int dataOffset, int bitsPerSample, int channels)
        {
            var bytesPerSample = bitsPerSample / 8;
            var totalBytes = wavData.Length - dataOffset;
            var sampleCount = totalBytes / (bytesPerSample * channels);

            var samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                var pos = dataOffset + i * bytesPerSample * channels;

                // 16ビットサンプルを想定
                if (bitsPerSample == 16 && pos + 1 < wavData.Length)
                {
                    var sample = BitConverter.ToInt16(wavData, pos);
                    samples[i] = sample / 32768f; // -1.0 〜 1.0 に正規化
                }
                else if (bitsPerSample == 8 && pos < wavData.Length)
                {
                    samples[i] = (wavData[pos] - 128) / 128f;
                }
            }

            return samples;
        }

        /// <summary>
        /// RMS（二乗平均平方根）を計算
        /// </summary>
        private static float CalculateRms(float[] samples, int start, int end)
        {
            if (end <= start) return 0f;

            double sumSquares = 0;
            for (int i = start; i < end; i++)
            {
                sumSquares += samples[i] * samples[i];
            }

            return (float)Math.Sqrt(sumSquares / (end - start));
        }

        /// <summary>
        /// 音量を0〜1の範囲に正規化
        /// </summary>
        private static float NormalizeVolume(float rms)
        {
            // RMSは通常0〜0.3程度なので、適当な係数で拡大
            var normalized = rms * 3.0f;
            return Math.Clamp(normalized, 0f, 1f);
        }
    }
}
