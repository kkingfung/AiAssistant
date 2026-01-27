#nullable enable
using System;
using System.Threading.Tasks;
using AiAssistant.Companionship;

namespace AiAssistant.Live2D
{
    /// <summary>
    /// Live2Dモデル制御サービスのインターフェース
    /// WebView2を使用してLive2Dモデルを表示・制御
    /// </summary>
    public interface ILive2DService : IDisposable
    {
        /// <summary>
        /// サービスが初期化済みかどうか
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// モデルが読み込まれているかどうか
        /// </summary>
        bool IsModelLoaded { get; }

        /// <summary>
        /// 現在のモデルパス
        /// </summary>
        string? CurrentModelPath { get; }

        /// <summary>
        /// サービスを初期化（WebView2の準備）
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Live2Dモデルを読み込む
        /// </summary>
        /// <param name="modelPath">モデルファイル(.model3.json)のパス</param>
        Task LoadModelAsync(string modelPath);

        /// <summary>
        /// モデルをアンロード
        /// </summary>
        Task UnloadModelAsync();

        /// <summary>
        /// 感情に対応する表情を設定
        /// </summary>
        /// <param name="emotion">感情タイプ</param>
        Task SetExpressionAsync(EmotionType emotion);

        /// <summary>
        /// 表情を名前で直接設定
        /// </summary>
        /// <param name="expressionName">表情名（例: "happy", "sad"）</param>
        Task SetExpressionByNameAsync(string expressionName);

        /// <summary>
        /// モーションを再生
        /// </summary>
        /// <param name="group">モーショングループ名（例: "Idle", "Greeting"）</param>
        /// <param name="index">グループ内のインデックス</param>
        /// <param name="priority">優先度</param>
        Task PlayMotionAsync(string group, int index = 0, MotionPriority priority = MotionPriority.Normal);

        /// <summary>
        /// リップシンク用の口の開き具合を設定
        /// </summary>
        /// <param name="value">0.0（閉）〜 1.0（開）</param>
        Task SetLipSyncAsync(float value);

        /// <summary>
        /// 視線の向きを設定（マウス追従用）
        /// </summary>
        /// <param name="x">X座標（-1.0 〜 1.0）</param>
        /// <param name="y">Y座標（-1.0 〜 1.0）</param>
        Task SetLookAtAsync(double x, double y);

        /// <summary>
        /// モデルパラメータを直接設定
        /// </summary>
        /// <param name="paramId">パラメータID（例: "ParamAngleX"）</param>
        /// <param name="value">値</param>
        Task SetParameterAsync(string paramId, float value);

        /// <summary>
        /// モデルの表示/非表示を切り替え
        /// </summary>
        /// <param name="visible">表示するかどうか</param>
        Task SetVisibilityAsync(bool visible);

        /// <summary>
        /// モデルのスケールを設定
        /// </summary>
        /// <param name="scale">スケール値（1.0 = 等倍）</param>
        Task SetScaleAsync(double scale);

        /// <summary>
        /// モデルの位置を設定
        /// </summary>
        /// <param name="x">X座標</param>
        /// <param name="y">Y座標</param>
        Task SetPositionAsync(double x, double y);

        /// <summary>
        /// モデル読み込み完了イベント
        /// </summary>
        event EventHandler? ModelLoaded;

        /// <summary>
        /// モーション再生完了イベント
        /// </summary>
        event EventHandler<MotionFinishedEventArgs>? MotionFinished;

        /// <summary>
        /// エラー発生イベント
        /// </summary>
        event EventHandler<Live2DErrorEventArgs>? ErrorOccurred;
    }

    /// <summary>
    /// モーション優先度
    /// </summary>
    public enum MotionPriority
    {
        /// <summary>アイドルモーション（最低優先度）</summary>
        Idle = 0,

        /// <summary>通常モーション</summary>
        Normal = 1,

        /// <summary>強制モーション（割り込み可能）</summary>
        Force = 2
    }

    /// <summary>
    /// モーション完了イベント引数
    /// </summary>
    public class MotionFinishedEventArgs : EventArgs
    {
        /// <summary>モーショングループ名</summary>
        public string Group { get; }

        /// <summary>モーションインデックス</summary>
        public int Index { get; }

        public MotionFinishedEventArgs(string group, int index)
        {
            Group = group;
            Index = index;
        }
    }

    /// <summary>
    /// Live2Dエラーイベント引数
    /// </summary>
    public class Live2DErrorEventArgs : EventArgs
    {
        /// <summary>エラーメッセージ</summary>
        public string Message { get; }

        /// <summary>例外（存在する場合）</summary>
        public Exception? Exception { get; }

        public Live2DErrorEventArgs(string message, Exception? exception = null)
        {
            Message = message;
            Exception = exception;
        }
    }
}
