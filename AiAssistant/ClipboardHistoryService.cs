using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace AiAssistant
{
    /// <summary>
    /// クリップボード履歴アイテム
    /// </summary>
    public class ClipboardItem
    {
        public string Text { get; set; } = string.Empty;
        public DateTime CopiedAt { get; set; }
        public string Preview => Text.Length > 50 ? Text[..50] + "..." : Text;

        public ClipboardItem(string text)
        {
            Text = text;
            CopiedAt = DateTime.Now;
        }
    }

    /// <summary>
    /// クリップボード履歴サービス
    /// コピーされたテキストの履歴を管理します
    /// </summary>
    public sealed class ClipboardHistoryService
    {
        private readonly List<ClipboardItem> _history = new();
        private readonly int _maxItems;
        private string _lastClipboardText = string.Empty;

        /// <summary>
        /// 履歴の最大件数
        /// </summary>
        public int MaxItems => _maxItems;

        /// <summary>
        /// 現在の履歴
        /// </summary>
        public IReadOnlyList<ClipboardItem> History => _history.AsReadOnly();

        /// <summary>
        /// 履歴が更新されたときに発火
        /// </summary>
        public event EventHandler? HistoryUpdated;

        public ClipboardHistoryService(int maxItems = 20)
        {
            _maxItems = maxItems;
        }

        /// <summary>
        /// クリップボードをチェックして新しいテキストがあれば履歴に追加
        /// </summary>
        public bool CheckAndAddFromClipboard()
        {
            try
            {
                if (!Clipboard.ContainsText())
                {
                    return false;
                }

                var text = Clipboard.GetText();
                if (string.IsNullOrWhiteSpace(text))
                {
                    return false;
                }

                // 前回と同じテキストなら追加しない
                if (text == _lastClipboardText)
                {
                    return false;
                }

                _lastClipboardText = text;
                AddToHistory(text);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ClipboardHistory] チェックエラー: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// テキストを履歴に追加
        /// </summary>
        public void AddToHistory(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            // 既に同じテキストがあれば削除（最新に移動）
            var existing = _history.FirstOrDefault(h => h.Text == text);
            if (existing != null)
            {
                _history.Remove(existing);
            }

            // 先頭に追加
            _history.Insert(0, new ClipboardItem(text));

            // 最大件数を超えたら古いものを削除
            while (_history.Count > _maxItems)
            {
                _history.RemoveAt(_history.Count - 1);
            }

            HistoryUpdated?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 履歴からテキストをクリップボードにコピー
        /// </summary>
        public bool CopyFromHistory(int index)
        {
            if (index < 0 || index >= _history.Count)
            {
                return false;
            }

            try
            {
                var item = _history[index];
                Clipboard.SetText(item.Text);
                _lastClipboardText = item.Text;

                // 選択したアイテムを先頭に移動
                _history.RemoveAt(index);
                _history.Insert(0, item);
                item.CopiedAt = DateTime.Now;

                HistoryUpdated?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ClipboardHistory] コピーエラー: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 履歴をクリア
        /// </summary>
        public void Clear()
        {
            _history.Clear();
            _lastClipboardText = string.Empty;
            HistoryUpdated?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 特定のアイテムを削除
        /// </summary>
        public bool RemoveAt(int index)
        {
            if (index < 0 || index >= _history.Count)
            {
                return false;
            }

            _history.RemoveAt(index);
            HistoryUpdated?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// 履歴の件数を取得
        /// </summary>
        public int Count => _history.Count;
    }
}
