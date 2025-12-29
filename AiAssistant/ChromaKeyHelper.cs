using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AiAssistant
{
    /// <summary>
    /// クロマキー処理を行うヘルパークラス
    /// マゼンタ色（RGB 255, 0, 255）を透明にします
    /// </summary>
    public static class ChromaKeyHelper
    {
        /// <summary>
        /// クロマキー対象色（マゼンタ）
        /// </summary>
        private static readonly byte ChromaKeyR = 255;
        private static readonly byte ChromaKeyG = 0;
        private static readonly byte ChromaKeyB = 255;

        /// <summary>
        /// 色差の許容範囲（0-255）
        /// マゼンタに近い色も透明にするための閾値
        /// </summary>
        private static readonly int ColorThreshold = 30;

        /// <summary>
        /// BitmapSourceにクロマキー処理を適用します
        /// </summary>
        /// <param name="source">元画像</param>
        /// <returns>クロマキー処理済み画像</returns>
        public static BitmapSource ApplyChromaKey(BitmapSource source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            // WriteableBitmapに変換（ピクセル操作のため）
            var writeableBitmap = new WriteableBitmap(source);

            // ピクセルバッファをロック
            writeableBitmap.Lock();

            try
            {
                unsafe
                {
                    // ピクセルデータのポインタを取得
                    var pBackBuffer = (byte*)writeableBitmap.BackBuffer;
                    int stride = writeableBitmap.BackBufferStride;
                    int bytesPerPixel = (writeableBitmap.Format.BitsPerPixel + 7) / 8;

                    // すべてのピクセルを走査
                    for (int y = 0; y < writeableBitmap.PixelHeight; y++)
                    {
                        for (int x = 0; x < writeableBitmap.PixelWidth; x++)
                        {
                            // ピクセル位置を計算
                            int offset = y * stride + x * bytesPerPixel;

                            // BGRAフォーマットで読み取り
                            byte b = pBackBuffer[offset];
                            byte g = pBackBuffer[offset + 1];
                            byte r = pBackBuffer[offset + 2];
                            byte a = bytesPerPixel > 3 ? pBackBuffer[offset + 3] : (byte)255;

                            // マゼンタ色かどうか判定
                            if (IsChromaKeyColor(r, g, b))
                            {
                                // 透明に設定
                                if (bytesPerPixel > 3)
                                {
                                    pBackBuffer[offset + 3] = 0; // Alpha = 0
                                }
                                else
                                {
                                    // アルファチャンネルがない場合は黒に設定
                                    pBackBuffer[offset] = 0;
                                    pBackBuffer[offset + 1] = 0;
                                    pBackBuffer[offset + 2] = 0;
                                }
                            }
                        }
                    }

                    // 変更を反映
                    writeableBitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight));
                }
            }
            finally
            {
                writeableBitmap.Unlock();
            }

            return writeableBitmap;
        }

        /// <summary>
        /// 指定された色がクロマキー色（マゼンタ）かどうかを判定します
        /// </summary>
        /// <param name="r">赤成分 (0-255)</param>
        /// <param name="g">緑成分 (0-255)</param>
        /// <param name="b">青成分 (0-255)</param>
        /// <returns>クロマキー色の場合 true</returns>
        private static bool IsChromaKeyColor(byte r, byte g, byte b)
        {
            // 各色成分の差分を計算
            int rDiff = Math.Abs(r - ChromaKeyR);
            int gDiff = Math.Abs(g - ChromaKeyG);
            int bDiff = Math.Abs(b - ChromaKeyB);

            // すべての成分が閾値以内ならマゼンタと判定
            return rDiff <= ColorThreshold && gDiff <= ColorThreshold && bDiff <= ColorThreshold;
        }

        /// <summary>
        /// GIFアニメーション用：各フレームにクロマキー処理を適用します
        /// </summary>
        /// <param name="gifDecoder">GIFデコーダー</param>
        /// <returns>クロマキー処理済みBitmapSource</returns>
        public static BitmapSource ApplyChromaKeyToGif(BitmapDecoder gifDecoder)
        {
            if (gifDecoder == null || gifDecoder.Frames.Count == 0)
                throw new ArgumentException("Invalid GIF decoder");

            // 最初のフレームにクロマキー適用
            // （GIFアニメーション全体の処理は複雑なため、まずは静止画として処理）
            return ApplyChromaKey(gifDecoder.Frames[0]);
        }
    }
}
