using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AiAssistant
{
    /// <summary>
    /// クイックノートアイテム
    /// </summary>
    public class NoteItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime ModifiedAt { get; set; } = DateTime.Now;
        public bool IsPinned { get; set; }

        public string Preview => Content.Length > 30
            ? Content.Replace("\n", " ")[..Math.Min(30, Content.Length)] + "..."
            : Content.Replace("\n", " ");
    }

    /// <summary>
    /// クイックノートサービス
    /// 簡単なメモを管理し、永続化します
    /// </summary>
    public sealed class QuickNotesService
    {
        private readonly List<NoteItem> _notes = new();
        private readonly string _savePath;

        /// <summary>
        /// 現在のノート一覧
        /// </summary>
        public IReadOnlyList<NoteItem> Notes => _notes.AsReadOnly();

        /// <summary>
        /// ノートが更新されたときに発火
        /// </summary>
        public event EventHandler? NotesUpdated;

        public QuickNotesService()
        {
            _savePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "quick_notes.json");
            Load();
        }

        /// <summary>
        /// 新しいノートを追加
        /// </summary>
        public NoteItem AddNote(string content)
        {
            var note = new NoteItem
            {
                Content = content
            };

            _notes.Insert(0, note);
            Save();
            NotesUpdated?.Invoke(this, EventArgs.Empty);
            return note;
        }

        /// <summary>
        /// ノートを更新
        /// </summary>
        public bool UpdateNote(string id, string newContent)
        {
            var note = _notes.Find(n => n.Id == id);
            if (note == null)
            {
                return false;
            }

            note.Content = newContent;
            note.ModifiedAt = DateTime.Now;
            Save();
            NotesUpdated?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// ノートを削除
        /// </summary>
        public bool DeleteNote(string id)
        {
            var note = _notes.Find(n => n.Id == id);
            if (note == null)
            {
                return false;
            }

            _notes.Remove(note);
            Save();
            NotesUpdated?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// ノートをピン留め/解除
        /// </summary>
        public bool TogglePin(string id)
        {
            var note = _notes.Find(n => n.Id == id);
            if (note == null)
            {
                return false;
            }

            note.IsPinned = !note.IsPinned;

            // ピン留めされたノートを上に移動
            SortNotes();
            Save();
            NotesUpdated?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// ノートをソート（ピン留め優先、その後は更新日時順）
        /// </summary>
        private void SortNotes()
        {
            _notes.Sort((a, b) =>
            {
                if (a.IsPinned != b.IsPinned)
                {
                    return b.IsPinned.CompareTo(a.IsPinned);
                }
                return b.ModifiedAt.CompareTo(a.ModifiedAt);
            });
        }

        /// <summary>
        /// すべてのノートをクリア
        /// </summary>
        public void ClearAll()
        {
            _notes.Clear();
            Save();
            NotesUpdated?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// ノートを保存
        /// </summary>
        private void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(_notes, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_savePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[QuickNotes] 保存エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// ノートを読み込み
        /// </summary>
        private void Load()
        {
            try
            {
                if (!File.Exists(_savePath))
                {
                    return;
                }

                var json = File.ReadAllText(_savePath);
                var notes = JsonSerializer.Deserialize<List<NoteItem>>(json);
                if (notes != null)
                {
                    _notes.Clear();
                    _notes.AddRange(notes);
                    SortNotes();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[QuickNotes] 読み込みエラー: {ex.Message}");
            }
        }

        /// <summary>
        /// ノート件数
        /// </summary>
        public int Count => _notes.Count;
    }
}
