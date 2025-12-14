using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Controls.Primitives;

namespace AiAssistant
{
    public partial class MainWindow : Window
    {
        private bool _isClickThrough;

        // 用於平滑 resize：記住 drag 開始時的寬高
        private double _resizeStartWidth;
        private double _resizeStartHeight;

        private const int HOTKEY_ID = 0xB001;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const int WM_HOTKEY = 0x0312;
        private HwndSource? _hwndSource;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new AssistantViewModel(new MockAiService());

            SourceInitialized += OnSourceInitialized;
            Loaded += OnLoaded;
            Closed += OnClosed;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            // 不做縮放調整：UI 保持固定大小並置中
        }

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            var helper = new WindowInteropHelper(this);
            var hwnd = helper.Handle;
            if (hwnd == IntPtr.Zero) return;

            _hwndSource = HwndSource.FromHwnd(hwnd);
            if (_hwndSource != null)
                _hwndSource.AddHook(WndProc);

            // 建議視窗不搶焦（可選）
            ClickThroughHelper.SetNoActivate(this, true);

            // 初始 click-through 關閉
            _isClickThrough = false;
            ClickThroughHelper.SetClickThrough(this, _isClickThrough);

            // 註冊全域熱鍵 Ctrl+Alt+T 用來切換 click-through
            var vk = (uint)KeyInterop.VirtualKeyFromKey(Key.T);
            _ = RegisterHotKey(hwnd, HOTKEY_ID, MOD_CONTROL | MOD_ALT, vk);
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            if (_hwndSource != null)
                _hwndSource.RemoveHook(WndProc);

            var helper = new WindowInteropHelper(this);
            UnregisterHotKey(helper.Handle, HOTKEY_ID);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_isClickThrough) return;
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                try { DragMove(); }
                catch { }
            }
        }

        private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ToggleClickThrough();
        }

        private void ToggleClickThrough()
        {
            _isClickThrough = !_isClickThrough;
            ClickThroughHelper.SetClickThrough(this, _isClickThrough);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                ToggleClickThrough();
                handled = true;
            }
            return IntPtr.Zero;
        }

        // 記住 drag 開始的寬高
        private void ResizeThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            _resizeStartWidth = Width;
            _resizeStartHeight = Height;
        }

        // 自由比例 resize（起始寬高 + delta）
        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double minW = MinWidth;
            double minH = MinHeight;

            double newW = Math.Max(minW, _resizeStartWidth + e.HorizontalChange);
            double newH = Math.Max(minH, _resizeStartHeight + e.VerticalChange);

            Width = newW;
            Height = newH;
        }

        private void ResizeThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            // 可在此儲存最終尺寸或做其他後處理
        }

        // 測試按鈕處理：顯示暫態文字 3 秒（UI 不阻塞）
        private async void OnTestButtonClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                TransientMessage.Text = "UI click detected";
                TransientBorder.Visibility = Visibility.Visible;

                // 顯示 3 秒
                await Task.Delay(3000).ConfigureAwait(true);

                TransientBorder.Visibility = Visibility.Collapsed;
            }
            catch
            {
                // 忽略取消或例外，保證 UI 清理
                TransientBorder.Visibility = Visibility.Collapsed;
            }
        }

        #region Win32 HotKey interop

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        #endregion
    }

    // ClickThroughHelper（Win32 interop, 保留既有功能）
    public static class ClickThroughHelper
    {
        private const int GWL_EXSTYLE = -20;
        private const long WS_EX_TRANSPARENT = 0x00000020L;
        private const long WS_EX_LAYERED = 0x00080000L;
        private const long WS_EX_NOACTIVATE = 0x08000000L;

        public static void SetClickThrough(Window window, bool enable)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));
            var helper = new WindowInteropHelper(window);
            var hwnd = helper.Handle;
            if (hwnd == IntPtr.Zero)
                throw new InvalidOperationException("Window handle not created yet. Call after SourceInitialized.");

            var exStylePtr = GetWindowLongPtr(hwnd, GWL_EXSTYLE);
            long exStyle = exStylePtr.ToInt64();

            if (enable)
                exStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT;
            else
                exStyle &= ~WS_EX_TRANSPARENT;

            SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(exStyle));
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
        }

        public static void SetNoActivate(Window window, bool enable)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));
            var helper = new WindowInteropHelper(window);
            var hwnd = helper.Handle;
            if (hwnd == IntPtr.Zero)
                throw new InvalidOperationException("Window handle not created yet. Call after SourceInitialized.");

            var exStylePtr = GetWindowLongPtr(hwnd, GWL_EXSTYLE);
            long exStyle = exStylePtr.ToInt64();

            if (enable)
                exStyle |= WS_EX_NOACTIVATE;
            else
                exStyle &= ~WS_EX_NOACTIVATE;

            SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(exStyle));
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);
        }

        #region Win32 interop (32/64-bit safe)

        private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
                return GetWindowLongPtr64(hWnd, nIndex);
            else
                return new IntPtr(GetWindowLong32(hWnd, nIndex));
        }

        private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            else
                return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
        private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_FRAMECHANGED = 0x0020;

        #endregion
    }
}