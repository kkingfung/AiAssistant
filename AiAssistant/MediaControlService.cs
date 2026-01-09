using System;
using System.Runtime.InteropServices;

namespace AiAssistant
{
    /// <summary>
    /// メディアコントロールサービス
    /// システムのメディアキーを使用して音楽プレイヤーを制御します
    /// </summary>
    public sealed class MediaControlService
    {
        // Virtual Key Codes for media keys
        private const int VK_MEDIA_PLAY_PAUSE = 0xB3;
        private const int VK_MEDIA_NEXT_TRACK = 0xB0;
        private const int VK_MEDIA_PREV_TRACK = 0xB1;
        private const int VK_MEDIA_STOP = 0xB2;
        private const int VK_VOLUME_MUTE = 0xAD;
        private const int VK_VOLUME_DOWN = 0xAE;
        private const int VK_VOLUME_UP = 0xAF;

        private const int KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const int KEYEVENTF_KEYUP = 0x0002;

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        /// <summary>
        /// 再生/一時停止を切り替え
        /// </summary>
        public void PlayPause()
        {
            SendMediaKey(VK_MEDIA_PLAY_PAUSE);
            System.Diagnostics.Debug.WriteLine("[MediaControl] Play/Pause");
        }

        /// <summary>
        /// 次の曲へ
        /// </summary>
        public void NextTrack()
        {
            SendMediaKey(VK_MEDIA_NEXT_TRACK);
            System.Diagnostics.Debug.WriteLine("[MediaControl] Next Track");
        }

        /// <summary>
        /// 前の曲へ
        /// </summary>
        public void PreviousTrack()
        {
            SendMediaKey(VK_MEDIA_PREV_TRACK);
            System.Diagnostics.Debug.WriteLine("[MediaControl] Previous Track");
        }

        /// <summary>
        /// 停止
        /// </summary>
        public void Stop()
        {
            SendMediaKey(VK_MEDIA_STOP);
            System.Diagnostics.Debug.WriteLine("[MediaControl] Stop");
        }

        /// <summary>
        /// ミュート切り替え
        /// </summary>
        public void ToggleMute()
        {
            SendMediaKey(VK_VOLUME_MUTE);
            System.Diagnostics.Debug.WriteLine("[MediaControl] Toggle Mute");
        }

        /// <summary>
        /// 音量を上げる
        /// </summary>
        public void VolumeUp()
        {
            SendMediaKey(VK_VOLUME_UP);
            System.Diagnostics.Debug.WriteLine("[MediaControl] Volume Up");
        }

        /// <summary>
        /// 音量を下げる
        /// </summary>
        public void VolumeDown()
        {
            SendMediaKey(VK_VOLUME_DOWN);
            System.Diagnostics.Debug.WriteLine("[MediaControl] Volume Down");
        }

        /// <summary>
        /// メディアキーを送信
        /// </summary>
        private void SendMediaKey(int keyCode)
        {
            try
            {
                // Key down
                keybd_event((byte)keyCode, 0, KEYEVENTF_EXTENDEDKEY, UIntPtr.Zero);
                // Key up
                keybd_event((byte)keyCode, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MediaControl] キー送信エラー: {ex.Message}");
            }
        }
    }
}
