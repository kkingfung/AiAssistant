using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace AiAssistant
{
    /// <summary>
    /// チャット履歴サービスの実装
    /// 会話履歴をJSONファイルに保存・読み込みします
    /// </summary>
    public sealed class ChatHistoryService : IChatHistoryService
    {
        private readonly string _historyFolder;
        private readonly ChatHistorySettings _settings;
        private ChatSession? _currentSession;

        public bool IsEnabled => _settings.SaveHistory;
        public ChatSession? CurrentSession => _currentSession;

        public ChatHistoryService()
        {
            _historyFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "chat_history");
            _settings = ChatHistorySettings.Load();

            // 設定ファイルが存在しない場合は作成
            var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "chat_history_settings.json");
            if (!File.Exists(settingsPath))
            {
                _settings.Save();
                Console.WriteLine("[ChatHistory] デフォルト設定ファイルを作成しました");
            }

            // 履歴フォルダを作成
            if (!Directory.Exists(_historyFolder))
            {
                Directory.CreateDirectory(_historyFolder);
                Console.WriteLine($"[ChatHistory] 履歴フォルダを作成しました: {_historyFolder}");
            }

            Console.WriteLine($"[ChatHistory] 履歴フォルダ: {_historyFolder}");
            Console.WriteLine($"[ChatHistory] 履歴保存: {(_settings.SaveHistory ? "有効" : "無効")}");
        }

        /// <summary>
        /// 新しいセッションを開始します
        /// </summary>
        public ChatSession StartNewSession(string? title = null)
        {
            // 現在のセッションを保存
            SaveCurrentSession();

            _currentSession = new ChatSession
            {
                Title = title ?? $"会話 {DateTime.Now:yyyy/MM/dd HH:mm}"
            };

            Console.WriteLine($"[ChatHistory] 新しいセッションを開始: {_currentSession.Id}");
            return _currentSession;
        }

        /// <summary>
        /// メッセージを追加します
        /// </summary>
        public void AddMessage(string role, string content)
        {
            if (!IsEnabled) return;

            if (_currentSession == null)
            {
                StartNewSession();
            }

            var message = new ChatHistoryMessage(role, content);
            _currentSession!.Messages.Add(message);
            _currentSession.UpdatedAt = DateTime.Now;

            // 最初のユーザーメッセージをタイトルに設定
            if (role == "user" && _currentSession.Messages.Count == 1)
            {
                _currentSession.Title = TruncateText(content, 30);
            }

            // メッセージ数が上限を超えたら古いものを削除
            while (_currentSession.Messages.Count > _settings.MaxMessages)
            {
                _currentSession.Messages.RemoveAt(0);
            }

            // 自動保存
            SaveCurrentSession();

            Console.WriteLine($"[ChatHistory] メッセージ追加: {role} ({content.Length}文字)");
        }

        /// <summary>
        /// 現在のセッションを保存します
        /// </summary>
        public void SaveCurrentSession()
        {
            if (!IsEnabled || _currentSession == null || _currentSession.Messages.Count == 0)
            {
                return;
            }

            try
            {
                var filePath = GetSessionFilePath(_currentSession.Id);
                var json = JsonSerializer.Serialize(_currentSession, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
                File.WriteAllText(filePath, json);

                // 古いセッションを削除
                CleanupOldSessions();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChatHistory] セッション保存エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// すべてのセッションを取得します
        /// </summary>
        public IReadOnlyList<ChatSession> GetAllSessions()
        {
            var sessions = new List<ChatSession>();

            try
            {
                if (!Directory.Exists(_historyFolder))
                {
                    return sessions;
                }

                var files = Directory.GetFiles(_historyFolder, "*.json")
                    .OrderByDescending(f => File.GetLastWriteTime(f));

                foreach (var file in files)
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var session = JsonSerializer.Deserialize<ChatSession>(json);
                        if (session != null)
                        {
                            sessions.Add(session);
                        }
                    }
                    catch
                    {
                        // 読み込みエラーは無視
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChatHistory] セッション一覧取得エラー: {ex.Message}");
            }

            return sessions;
        }

        /// <summary>
        /// セッションを読み込みます
        /// </summary>
        public ChatSession? LoadSession(string sessionId)
        {
            try
            {
                var filePath = GetSessionFilePath(sessionId);
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    var session = JsonSerializer.Deserialize<ChatSession>(json);
                    if (session != null)
                    {
                        _currentSession = session;
                        Console.WriteLine($"[ChatHistory] セッションを読み込み: {sessionId}");
                        return session;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChatHistory] セッション読み込みエラー: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// セッションを削除します
        /// </summary>
        public void DeleteSession(string sessionId)
        {
            try
            {
                var filePath = GetSessionFilePath(sessionId);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Console.WriteLine($"[ChatHistory] セッションを削除: {sessionId}");
                }

                if (_currentSession?.Id == sessionId)
                {
                    _currentSession = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChatHistory] セッション削除エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// すべての履歴をクリアします
        /// </summary>
        public void ClearHistory()
        {
            try
            {
                if (Directory.Exists(_historyFolder))
                {
                    foreach (var file in Directory.GetFiles(_historyFolder, "*.json"))
                    {
                        File.Delete(file);
                    }
                }
                _currentSession = null;
                Console.WriteLine("[ChatHistory] すべての履歴をクリアしました");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChatHistory] 履歴クリアエラー: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 直前のセッションを復元します
        /// </summary>
        public ChatSession? RestoreLastSession()
        {
            try
            {
                var sessions = GetAllSessions();
                if (sessions.Count > 0)
                {
                    var lastSession = sessions[0];
                    return LoadSession(lastSession.Id);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChatHistory] セッション復元エラー: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// セッションファイルのパスを取得します
        /// </summary>
        private string GetSessionFilePath(string sessionId)
        {
            // 無効な文字を除去
            var safeId = string.Join("_", sessionId.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(_historyFolder, $"{safeId}.json");
        }

        /// <summary>
        /// 古いセッションを削除します
        /// </summary>
        private void CleanupOldSessions()
        {
            try
            {
                var files = Directory.GetFiles(_historyFolder, "*.json")
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .Skip(_settings.MaxSessions)
                    .ToList();

                foreach (var file in files)
                {
                    File.Delete(file);
                    Console.WriteLine($"[ChatHistory] 古いセッションを削除: {Path.GetFileName(file)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChatHistory] クリーンアップエラー: {ex.Message}");
            }
        }

        /// <summary>
        /// テキストを指定長で切り詰めます
        /// </summary>
        private static string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return "新しい会話";
            // 改行を除去
            text = text.Replace("\n", " ").Replace("\r", "").Trim();
            if (text.Length <= maxLength) return text;
            return text[..(maxLength - 1)] + "…";
        }
    }
}
