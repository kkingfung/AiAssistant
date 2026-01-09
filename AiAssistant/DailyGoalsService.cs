using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiAssistant
{
    /// <summary>
    /// 目標/タスクアイテム
    /// </summary>
    public class GoalItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }
        public int Streak { get; set; } // 連続達成日数
        public bool IsDaily { get; set; } // 毎日リセットするか
    }

    /// <summary>
    /// 日次データ
    /// </summary>
    public class DailyData
    {
        public DateTime Date { get; set; }
        public List<string> CompletedGoalIds { get; set; } = new();
    }

    /// <summary>
    /// 保存データ
    /// </summary>
    public class GoalsData
    {
        public List<GoalItem> Goals { get; set; } = new();
        public List<DailyData> DailyHistory { get; set; } = new();
        public DateTime LastResetDate { get; set; }
    }

    /// <summary>
    /// デイリーゴール/習慣トラッカーサービス
    /// </summary>
    public sealed class DailyGoalsService
    {
        private GoalsData _data = new();
        private readonly string _savePath;

        /// <summary>
        /// 現在の目標一覧
        /// </summary>
        public IReadOnlyList<GoalItem> Goals => _data.Goals.AsReadOnly();

        /// <summary>
        /// 今日完了した目標数
        /// </summary>
        public int TodayCompletedCount => _data.Goals.Count(g => g.IsCompleted);

        /// <summary>
        /// 今日の目標総数
        /// </summary>
        public int TodayTotalCount => _data.Goals.Count;

        /// <summary>
        /// 目標が更新されたときに発火
        /// </summary>
        public event EventHandler? GoalsUpdated;

        public DailyGoalsService()
        {
            _savePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "daily_goals.json");
            Load();
            CheckDailyReset();
        }

        /// <summary>
        /// 新しい目標を追加
        /// </summary>
        public GoalItem AddGoal(string title, bool isDaily = true)
        {
            var goal = new GoalItem
            {
                Title = title,
                IsDaily = isDaily
            };

            _data.Goals.Add(goal);
            Save();
            GoalsUpdated?.Invoke(this, EventArgs.Empty);
            return goal;
        }

        /// <summary>
        /// 目標を完了/未完了に切り替え
        /// </summary>
        public bool ToggleGoal(string id)
        {
            var goal = _data.Goals.Find(g => g.Id == id);
            if (goal == null)
            {
                return false;
            }

            goal.IsCompleted = !goal.IsCompleted;

            if (goal.IsCompleted)
            {
                goal.CompletedAt = DateTime.Now;
                goal.Streak++;
            }
            else
            {
                goal.CompletedAt = null;
            }

            Save();
            GoalsUpdated?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// 目標を削除
        /// </summary>
        public bool DeleteGoal(string id)
        {
            var goal = _data.Goals.Find(g => g.Id == id);
            if (goal == null)
            {
                return false;
            }

            _data.Goals.Remove(goal);
            Save();
            GoalsUpdated?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// 日次リセットをチェック
        /// </summary>
        private void CheckDailyReset()
        {
            var today = DateTime.Today;

            if (_data.LastResetDate.Date < today)
            {
                // 昨日のデータを履歴に保存
                if (_data.Goals.Any(g => g.IsCompleted))
                {
                    var dailyData = new DailyData
                    {
                        Date = _data.LastResetDate.Date,
                        CompletedGoalIds = _data.Goals.Where(g => g.IsCompleted).Select(g => g.Id).ToList()
                    };
                    _data.DailyHistory.Add(dailyData);

                    // 30日分だけ保持
                    while (_data.DailyHistory.Count > 30)
                    {
                        _data.DailyHistory.RemoveAt(0);
                    }
                }

                // デイリー目標をリセット
                foreach (var goal in _data.Goals.Where(g => g.IsDaily))
                {
                    if (!goal.IsCompleted)
                    {
                        // 未完了だった場合、ストリークをリセット
                        goal.Streak = 0;
                    }
                    goal.IsCompleted = false;
                    goal.CompletedAt = null;
                }

                _data.LastResetDate = today;
                Save();
                GoalsUpdated?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 進捗テキストを取得
        /// </summary>
        public string GetProgressText()
        {
            if (_data.Goals.Count == 0)
            {
                return "目標がありません";
            }

            var completed = TodayCompletedCount;
            var total = TodayTotalCount;
            var percent = total > 0 ? (double)completed / total * 100 : 0;

            return $"今日の進捗: {completed}/{total} ({percent:F0}%)";
        }

        /// <summary>
        /// モチベーションメッセージを取得
        /// </summary>
        public string GetMotivationMessage()
        {
            var completed = TodayCompletedCount;
            var total = TodayTotalCount;

            if (total == 0)
            {
                return "今日の目標を設定してみよう！";
            }

            var percent = (double)completed / total * 100;

            return percent switch
            {
                100 => "🎉 素晴らしい！今日の目標を全て達成しました！",
                >= 75 => "💪 あと少し！頑張って！",
                >= 50 => "👍 いい調子！半分以上達成したよ！",
                >= 25 => "🌱 順調に進んでいるね！",
                > 0 => "✨ いいスタート！続けていこう！",
                _ => "🚀 さあ、始めよう！"
            };
        }

        /// <summary>
        /// 最長ストリークを取得
        /// </summary>
        public int GetLongestStreak()
        {
            return _data.Goals.Count > 0 ? _data.Goals.Max(g => g.Streak) : 0;
        }

        /// <summary>
        /// 保存
        /// </summary>
        private void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_savePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DailyGoals] 保存エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// 読み込み
        /// </summary>
        private void Load()
        {
            try
            {
                if (!File.Exists(_savePath))
                {
                    _data.LastResetDate = DateTime.Today;
                    return;
                }

                var json = File.ReadAllText(_savePath);
                var data = JsonSerializer.Deserialize<GoalsData>(json);
                if (data != null)
                {
                    _data = data;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DailyGoals] 読み込みエラー: {ex.Message}");
                _data.LastResetDate = DateTime.Today;
            }
        }
    }
}
