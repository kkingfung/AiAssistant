#nullable enable
using System;

namespace AiAssistant.Companionship
{
    /// <summary>
    /// 絆ポイント獲得のソース
    /// </summary>
    public enum BondPointSource
    {
        /// <summary>チャットメッセージ送信</summary>
        ChatMessage,

        /// <summary>長めの会話（5往復以上）ボーナス</summary>
        LongConversationBonus,

        /// <summary>毎日のログイン</summary>
        DailyLogin,

        /// <summary>ポモドーロ完了</summary>
        PomodoroComplete,

        /// <summary>デイリーゴール達成</summary>
        GoalAchieved,

        /// <summary>復帰ボーナス（長時間放置後）</summary>
        ReturnBonus
    }

    /// <summary>
    /// 絆レベルアップイベントの引数
    /// </summary>
    public sealed class BondLevelUpEventArgs : EventArgs
    {
        /// <summary>以前のレベル</summary>
        public BondLevel PreviousLevel { get; }

        /// <summary>新しいレベル</summary>
        public BondLevel NewLevel { get; }

        /// <summary>現在の総ポイント</summary>
        public int TotalPoints { get; }

        public BondLevelUpEventArgs(BondLevel previousLevel, BondLevel newLevel, int totalPoints)
        {
            PreviousLevel = previousLevel;
            NewLevel = newLevel;
            TotalPoints = totalPoints;
        }
    }

    /// <summary>
    /// 絆システムのインターフェース
    /// ユーザーとペットの絆レベルを管理
    /// </summary>
    public interface IBondService
    {
        /// <summary>現在の絆ポイント</summary>
        int CurrentPoints { get; }

        /// <summary>現在の絆レベル</summary>
        BondLevel CurrentLevel { get; }

        /// <summary>次のレベルまでのポイント</summary>
        int PointsToNextLevel { get; }

        /// <summary>現在のレベルでの進捗率（0.0 - 1.0）</summary>
        double LevelProgress { get; }

        /// <summary>
        /// 絆ポイントを追加
        /// </summary>
        /// <param name="source">ポイント獲得のソース</param>
        /// <returns>追加されたポイント数</returns>
        int AddPoints(BondPointSource source);

        /// <summary>
        /// 絆ポイントを追加（カスタム値）
        /// </summary>
        /// <param name="points">追加するポイント</param>
        /// <param name="reason">理由（ログ用）</param>
        /// <returns>追加されたポイント数</returns>
        int AddPoints(int points, string reason);

        /// <summary>
        /// 絆レベルアップ時に発火するイベント
        /// </summary>
        event EventHandler<BondLevelUpEventArgs>? LevelUp;

        /// <summary>
        /// ポイント変更時に発火するイベント
        /// </summary>
        event EventHandler<int>? PointsChanged;
    }
}
