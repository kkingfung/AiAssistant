#nullable enable
using System;
using System.Threading.Tasks;
using AiAssistant.Companionship;

namespace AiAssistant.Character
{
    /// <summary>
    /// 対話表示イベントの引数
    /// </summary>
    public sealed class DialogueEventArgs : EventArgs
    {
        /// <summary>表示するセリフ</summary>
        public string Text { get; }

        /// <summary>キャラクター名</summary>
        public string CharacterName { get; }

        /// <summary>対話のトリガー</summary>
        public DialogueTrigger Trigger { get; }

        /// <summary>表示時間（ミリ秒）、0の場合はクリックまで表示</summary>
        public int DisplayDurationMs { get; }

        /// <summary>感情（表情制御用）</summary>
        public EmotionType Emotion { get; }

        public DialogueEventArgs(
            string text,
            string characterName,
            DialogueTrigger trigger,
            int displayDurationMs,
            EmotionType emotion)
        {
            Text = text;
            CharacterName = characterName;
            Trigger = trigger;
            DisplayDurationMs = displayDurationMs;
            Emotion = emotion;
        }
    }

    /// <summary>
    /// キャラクター対話システムのインターフェース
    /// キャラクターのセリフ生成と表示を管理
    /// </summary>
    public interface ICharacterDialogueService : IDisposable
    {
        /// <summary>現在のキャラクター性格</summary>
        CharacterPersonality CurrentPersonality { get; }

        /// <summary>対話が進行中かどうか</summary>
        bool IsDialogueActive { get; }

        /// <summary>
        /// キャラクターの性格を設定
        /// </summary>
        /// <param name="personality">性格</param>
        void SetPersonality(CharacterPersonality personality);

        /// <summary>
        /// 対話を生成して表示
        /// </summary>
        /// <param name="context">対話コンテキスト</param>
        /// <returns>生成されたセリフ</returns>
        Task<string> TriggerDialogueAsync(DialogueContext context);

        /// <summary>
        /// 挨拶を表示（アプリ起動時）
        /// </summary>
        Task ShowGreetingAsync();

        /// <summary>
        /// アイドル時のランダム発言
        /// </summary>
        Task ShowIdleChatAsync();

        /// <summary>
        /// ユーザータッチへの反応
        /// </summary>
        Task ShowTouchReactionAsync();

        /// <summary>
        /// ポモドーロ関連の発言
        /// </summary>
        /// <param name="isComplete">完了時はtrue、開始時はfalse</param>
        /// <param name="sessionCount">セッション回数</param>
        Task ShowPomodoroDialogueAsync(bool isComplete, int sessionCount = 0);

        /// <summary>
        /// 絆レベルアップの祝福
        /// </summary>
        /// <param name="newLevel">新しいレベル</param>
        Task ShowBondLevelUpAsync(BondLevel newLevel);

        /// <summary>
        /// 長時間放置後の反応
        /// </summary>
        /// <param name="absentDuration">放置時間</param>
        Task ShowLongAbsenceReactionAsync(TimeSpan absentDuration);

        /// <summary>
        /// 現在の対話を閉じる
        /// </summary>
        void DismissDialogue();

        /// <summary>
        /// アイドル対話タイマーを開始
        /// </summary>
        /// <param name="intervalMinutes">発言間隔（分）</param>
        void StartIdleDialogueTimer(int intervalMinutes = 5);

        /// <summary>
        /// アイドル対話タイマーを停止
        /// </summary>
        void StopIdleDialogueTimer();

        /// <summary>
        /// 対話表示時に発火するイベント
        /// </summary>
        event EventHandler<DialogueEventArgs>? DialogueRequested;

        /// <summary>
        /// 対話終了時に発火するイベント
        /// </summary>
        event EventHandler? DialogueDismissed;
    }
}
