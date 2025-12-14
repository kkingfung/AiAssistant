using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace AiAssistant
{
    public static class WindowInteropHelpers
    {
        private const int GWL_EXSTYLE = -20;
        private const long WS_EX_TRANSPARENT = 0x00000020L;
        private const long WS_EX_LAYERED = 0x00080000L;
        private const long WS_EX_NOACTIVATE = 0x08000000L;

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_FRAMECHANGED = 0x0020;

        public static void SetClickThrough(Window window, bool enable)
        {
            var hwnd = GetHwnd(window);
            var ex = (long)GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();

            if (enable)
                ex |= WS_EX_TRANSPARENT | WS_EX_LAYERED;
            else
                ex &= ~WS_EX_TRANSPARENT;

            SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(ex));
            // 變更 style 後通知系統，不要激活視窗
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED | SWP_NOACTIVATE);
        }

        public static void SetNoActivate(Window window, bool enable)
        {
            var hwnd = GetHwnd(window);
            var ex = (long)GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();

            if (enable) ex |= WS_EX_NOACTIVATE;
            else ex &= ~WS_EX_NOACTIVATE;

            SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(ex));
            // 更新 style 時不激活
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED | SWP_NOACTIVATE);
        }

        // 把視窗設定為 topmost，但不激活（避免搶焦）
        public static void SetTopMostNoActivate(Window window, bool topmost)
        {
            var hwnd = GetHwnd(window);
            var insert = topmost ? HWND_TOPMOST : HWND_NOTOPMOST;
            SetWindowPos(hwnd, insert, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
        }

        private static IntPtr GetHwnd(Window w)
        {
            var helper = new WindowInteropHelper(w);
            if (helper.Handle == IntPtr.Zero) throw new InvalidOperationException("Call after SourceInitialized");
            return helper.Handle;
        }

        #region Win32 wrappers (32/64-bit safe)
        private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8) return GetWindowLongPtr64(hWnd, nIndex);
            return new IntPtr(GetWindowLong32(hWnd, nIndex));
        }

        private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8) return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
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
        #endregion
    }
}