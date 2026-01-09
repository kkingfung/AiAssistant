using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace AiAssistant
{
    /// <summary>
    /// スクリーンショット＆OCRサービス
    /// 画面キャプチャとテキスト抽出を提供します
    /// </summary>
    public sealed class ScreenshotOcrService : IDisposable
    {
        /// <summary>
        /// スクリーンショットが撮影されたときに発火
        /// </summary>
        public event EventHandler<ScreenshotEventArgs>? ScreenshotTaken;

        /// <summary>
        /// OCR結果が取得されたときに発火
        /// </summary>
        public event EventHandler<OcrResultEventArgs>? OcrCompleted;

        /// <summary>
        /// 全画面をキャプチャ
        /// </summary>
        public Bitmap? CaptureFullScreen()
        {
            try
            {
                var screenWidth = (int)SystemParameters.VirtualScreenWidth;
                var screenHeight = (int)SystemParameters.VirtualScreenHeight;
                var screenLeft = (int)SystemParameters.VirtualScreenLeft;
                var screenTop = (int)SystemParameters.VirtualScreenTop;

                var bitmap = new Bitmap(screenWidth, screenHeight, PixelFormat.Format32bppArgb);
                using var graphics = Graphics.FromImage(bitmap);
                graphics.CopyFromScreen(screenLeft, screenTop, 0, 0, new System.Drawing.Size(screenWidth, screenHeight));

                ScreenshotTaken?.Invoke(this, new ScreenshotEventArgs(bitmap));
                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Screenshot] キャプチャエラー: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// アクティブウィンドウをキャプチャ
        /// </summary>
        public Bitmap? CaptureActiveWindow()
        {
            try
            {
                var hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero)
                {
                    return null;
                }

                GetWindowRect(hwnd, out RECT rect);
                var width = rect.Right - rect.Left;
                var height = rect.Bottom - rect.Top;

                if (width <= 0 || height <= 0)
                {
                    return null;
                }

                var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                using var graphics = Graphics.FromImage(bitmap);
                graphics.CopyFromScreen(rect.Left, rect.Top, 0, 0, new System.Drawing.Size(width, height));

                ScreenshotTaken?.Invoke(this, new ScreenshotEventArgs(bitmap));
                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Screenshot] ウィンドウキャプチャエラー: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// スクリーンショットをクリップボードにコピー
        /// </summary>
        public bool CopyToClipboard(Bitmap bitmap)
        {
            try
            {
                Clipboard.SetImage(ConvertToBitmapSource(bitmap));
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Screenshot] クリップボードコピーエラー: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// スクリーンショットをファイルに保存
        /// </summary>
        public string? SaveToFile(Bitmap bitmap, string? folderPath = null)
        {
            try
            {
                var folder = folderPath ?? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                    "AiAssistant_Screenshots"
                );

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                var fileName = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                var filePath = Path.Combine(folder, fileName);

                bitmap.Save(filePath, ImageFormat.Png);
                return filePath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Screenshot] 保存エラー: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 簡易OCR（Windows OCR APIを使用）
        /// 注意: Windows 10以降でWindows.Media.Ocrが必要
        /// この実装ではクリップボードにコピーした画像の説明をAIに依頼する形式
        /// </summary>
        public string? ExtractTextSimple(Bitmap bitmap)
        {
            // 注意: 本格的なOCRにはWindows.Media.Ocrまたは外部ライブラリが必要
            // ここでは画像をBase64に変換してAIに送る準備をする
            try
            {
                using var ms = new MemoryStream();
                bitmap.Save(ms, ImageFormat.Png);
                var base64 = Convert.ToBase64String(ms.ToArray());
                return base64; // Base64エンコードされた画像データを返す
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OCR] 変換エラー: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// BitmapをBitmapSourceに変換
        /// </summary>
        private BitmapSource ConvertToBitmapSource(Bitmap bitmap)
        {
            var handle = bitmap.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(
                    handle,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions()
                );
            }
            finally
            {
                DeleteObject(handle);
            }
        }

        public void Dispose()
        {
            // クリーンアップ
        }

        #region Win32 API

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        #endregion
    }

    /// <summary>
    /// スクリーンショットイベント引数
    /// </summary>
    public class ScreenshotEventArgs : EventArgs
    {
        public Bitmap Screenshot { get; }

        public ScreenshotEventArgs(Bitmap screenshot)
        {
            Screenshot = screenshot;
        }
    }

    /// <summary>
    /// OCR結果イベント引数
    /// </summary>
    public class OcrResultEventArgs : EventArgs
    {
        public string Text { get; }

        public OcrResultEventArgs(string text)
        {
            Text = text;
        }
    }
}
