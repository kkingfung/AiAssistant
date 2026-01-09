using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiAssistant
{
    /// <summary>
    /// GitHub通知サービスのインターフェース
    /// </summary>
    public interface IGitHubService
    {
        /// <summary>
        /// 未読通知を取得します
        /// </summary>
        Task<IReadOnlyList<GitHubNotification>> GetNotificationsAsync();

        /// <summary>
        /// ユーザーのPRレビュー待ち一覧を取得します
        /// </summary>
        Task<IReadOnlyList<GitHubPullRequest>> GetPullRequestsAwaitingReviewAsync();

        /// <summary>
        /// 最近のワークフロー実行状況を取得します
        /// </summary>
        Task<IReadOnlyList<GitHubWorkflowRun>> GetRecentWorkflowRunsAsync(string owner, string repo);

        /// <summary>
        /// 通知を既読にします
        /// </summary>
        Task MarkNotificationAsReadAsync(string threadId);

        /// <summary>
        /// すべての通知を既読にします
        /// </summary>
        Task MarkAllNotificationsAsReadAsync();

        /// <summary>
        /// 未読通知数を取得します
        /// </summary>
        Task<int> GetUnreadNotificationCountAsync();
    }

    /// <summary>
    /// GitHub通知
    /// </summary>
    public class GitHubNotification
    {
        public string Id { get; set; } = string.Empty;
        public string Repository { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // Issue, PullRequest, etc.
        public string Reason { get; set; } = string.Empty; // assign, mention, review_requested, etc.
        public DateTime UpdatedAt { get; set; }
        public bool Unread { get; set; }
        public string Url { get; set; } = string.Empty;
    }

    /// <summary>
    /// GitHubプルリクエスト
    /// </summary>
    public class GitHubPullRequest
    {
        public int Number { get; set; }
        public string Repository { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string State { get; set; } = string.Empty; // open, closed, merged
        public bool IsDraft { get; set; }
        public string Url { get; set; } = string.Empty;
        public int Additions { get; set; }
        public int Deletions { get; set; }
        public int Comments { get; set; }
    }

    /// <summary>
    /// GitHubワークフロー実行
    /// </summary>
    public class GitHubWorkflowRun
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // queued, in_progress, completed
        public string Conclusion { get; set; } = string.Empty; // success, failure, cancelled, etc.
        public string Branch { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Url { get; set; } = string.Empty;
    }
}
