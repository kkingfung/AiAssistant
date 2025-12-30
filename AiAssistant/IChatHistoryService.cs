using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiAssistant
{
    /// <summary>
    /// チャット履歴メッセージを表すクラス
    /// </summary>
    public sealed class ChatHistoryMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "user"; // "user", "assistant", "system"

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public ChatHistoryMessage() { }

        public ChatHistoryMessage(string role, string content)
        {
            Role = role;
            Content = content;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// 会話セッションを表すクラス
    /// </summary>
    public sealed class ChatSession
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [JsonPropertyName("messages")]
        public List<ChatHistoryMessage> Messages { get; set; } = new();
    }

    /// <summary>
    /// 会話履歴の設定
    /// </summary>
    public sealed class ChatHistorySettings
    {
        private static readonly string SettingsPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "chat_history_settings.json"
        );

        [JsonPropertyName("saveHistory")]
        public bool SaveHistory { get; set; } = true;

        [JsonPropertyName("maxMessages")]
        public int MaxMessages { get; set; } = 100;

        [JsonPropertyName("maxSessions")]
        public int MaxSessions { get; set; } = 50;

        public static ChatHistorySettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<ChatHistorySettings>(json) ?? new ChatHistorySettings();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChatHistory] 設定読み込みエラー: {ex.Message}");
            }
            return new ChatHistorySettings();
        }

        public void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChatHistory] 設定保存エラー: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// チャット履歴サービスのインターフェース
    /// </summary>
    public interface IChatHistoryService
    {
        /// <summary>
        /// 履歴保存が有効かどうか
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// 現在のセッション
        /// </summary>
        ChatSession? CurrentSession { get; }

        /// <summary>
        /// 新しいセッションを開始します
        /// </summary>
        ChatSession StartNewSession(string? title = null);

        /// <summary>
        /// メッセージを追加します
        /// </summary>
        void AddMessage(string role, string content);

        /// <summary>
        /// 現在のセッションを保存します
        /// </summary>
        void SaveCurrentSession();

        /// <summary>
        /// すべてのセッションを取得します
        /// </summary>
        IReadOnlyList<ChatSession> GetAllSessions();

        /// <summary>
        /// セッションを読み込みます
        /// </summary>
        ChatSession? LoadSession(string sessionId);

        /// <summary>
        /// セッションを削除します
        /// </summary>
        void DeleteSession(string sessionId);

        /// <summary>
        /// すべての履歴をクリアします
        /// </summary>
        void ClearHistory();

        /// <summary>
        /// 直前のセッションを復元します
        /// </summary>
        ChatSession? RestoreLastSession();
    }
}
