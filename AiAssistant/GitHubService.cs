using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AiAssistant
{
    /// <summary>
    /// GitHubサービスの実装
    /// Personal Access Tokenを使用してGitHub APIにアクセスします
    /// </summary>
    public sealed class GitHubService : IGitHubService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private const string ApiBaseUrl = "https://api.github.com";

        public GitHubService(string? token = null)
        {
            var settings = AppSettings.Instance.GitHub;
            var pat = token ?? settings.PersonalAccessToken;

            if (string.IsNullOrWhiteSpace(pat) || pat == "YOUR_GITHUB_TOKEN_HERE")
            {
                throw new InvalidOperationException(
                    "GitHub Personal Access Tokenが設定されていません。appsettings.jsonに設定してください。");
            }

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(ApiBaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };

            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {pat}");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AiAssistant-Desktop");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
            _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        }

        /// <summary>
        /// 未読通知を取得します
        /// </summary>
        public async Task<IReadOnlyList<GitHubNotification>> GetNotificationsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/notifications?all=false").ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var apiNotifications = await response.Content.ReadFromJsonAsync<List<GitHubApiNotification>>().ConfigureAwait(false);

                var notifications = new List<GitHubNotification>();
                if (apiNotifications != null)
                {
                    foreach (var n in apiNotifications)
                    {
                        notifications.Add(new GitHubNotification
                        {
                            Id = n.Id ?? string.Empty,
                            Repository = n.Repository?.FullName ?? string.Empty,
                            Title = n.Subject?.Title ?? string.Empty,
                            Type = n.Subject?.Type ?? string.Empty,
                            Reason = n.Reason ?? string.Empty,
                            UpdatedAt = n.UpdatedAt,
                            Unread = n.Unread,
                            Url = n.Subject?.Url ?? string.Empty
                        });
                    }
                }

                return notifications;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GitHub通知取得エラー: {ex.Message}");
                return new List<GitHubNotification>();
            }
        }

        /// <summary>
        /// PRレビュー待ち一覧を取得します
        /// </summary>
        public async Task<IReadOnlyList<GitHubPullRequest>> GetPullRequestsAwaitingReviewAsync()
        {
            try
            {
                // レビュー依頼されたPRを検索
                var query = "is:pr is:open review-requested:@me";
                var encodedQuery = Uri.EscapeDataString(query);
                var response = await _httpClient.GetAsync($"/search/issues?q={encodedQuery}").ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var searchResult = await response.Content.ReadFromJsonAsync<GitHubSearchResult>().ConfigureAwait(false);

                var pullRequests = new List<GitHubPullRequest>();
                if (searchResult?.Items != null)
                {
                    foreach (var item in searchResult.Items)
                    {
                        var repoName = ExtractRepoFromUrl(item.RepositoryUrl ?? string.Empty);
                        pullRequests.Add(new GitHubPullRequest
                        {
                            Number = item.Number,
                            Repository = repoName,
                            Title = item.Title ?? string.Empty,
                            Author = item.User?.Login ?? string.Empty,
                            CreatedAt = item.CreatedAt,
                            UpdatedAt = item.UpdatedAt,
                            State = item.State ?? "open",
                            IsDraft = item.Draft,
                            Url = item.HtmlUrl ?? string.Empty,
                            Comments = item.Comments
                        });
                    }
                }

                return pullRequests;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GitHubレビュー待ちPR取得エラー: {ex.Message}");
                return new List<GitHubPullRequest>();
            }
        }

        /// <summary>
        /// 最近のワークフロー実行状況を取得します
        /// </summary>
        public async Task<IReadOnlyList<GitHubWorkflowRun>> GetRecentWorkflowRunsAsync(string owner, string repo)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/repos/{owner}/{repo}/actions/runs?per_page=10").ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<GitHubWorkflowRunsResult>().ConfigureAwait(false);

                var runs = new List<GitHubWorkflowRun>();
                if (result?.WorkflowRuns != null)
                {
                    foreach (var run in result.WorkflowRuns)
                    {
                        runs.Add(new GitHubWorkflowRun
                        {
                            Id = run.Id,
                            Name = run.Name ?? string.Empty,
                            Status = run.Status ?? string.Empty,
                            Conclusion = run.Conclusion ?? string.Empty,
                            Branch = run.HeadBranch ?? string.Empty,
                            CreatedAt = run.CreatedAt,
                            CompletedAt = run.UpdatedAt,
                            Url = run.HtmlUrl ?? string.Empty
                        });
                    }
                }

                return runs;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GitHubワークフロー取得エラー: {ex.Message}");
                return new List<GitHubWorkflowRun>();
            }
        }

        /// <summary>
        /// 通知を既読にします
        /// </summary>
        public async Task MarkNotificationAsReadAsync(string threadId)
        {
            try
            {
                var response = await _httpClient.PatchAsync($"/notifications/threads/{threadId}", null).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GitHub通知既読化エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// すべての通知を既読にします
        /// </summary>
        public async Task MarkAllNotificationsAsReadAsync()
        {
            try
            {
                var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync("/notifications", content).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GitHub全通知既読化エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// 未読通知数を取得します
        /// </summary>
        public async Task<int> GetUnreadNotificationCountAsync()
        {
            var notifications = await GetNotificationsAsync().ConfigureAwait(false);
            return notifications.Count;
        }

        /// <summary>
        /// リポジトリURLからリポジトリ名を抽出します
        /// </summary>
        private static string ExtractRepoFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return string.Empty;

            // https://api.github.com/repos/owner/repo
            var parts = url.Split('/');
            if (parts.Length >= 2)
            {
                return $"{parts[^2]}/{parts[^1]}";
            }
            return url;
        }

        /// <summary>
        /// GitHub APIが利用可能かチェックします
        /// </summary>
        public static async Task<bool> IsGitHubAvailableAsync(string? token = null)
        {
            try
            {
                var settings = AppSettings.Instance.GitHub;
                var pat = token ?? settings.PersonalAccessToken;

                if (string.IsNullOrWhiteSpace(pat) || pat == "YOUR_GITHUB_TOKEN_HERE")
                {
                    return false;
                }

                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {pat}");
                client.DefaultRequestHeaders.Add("User-Agent", "AiAssistant-Desktop");
                client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");

                var response = await client.GetAsync("https://api.github.com/user").ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GitHub接続チェック失敗: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }

    #region GitHub API Models

    internal class GitHubApiNotification
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("repository")]
        public GitHubApiRepository? Repository { get; set; }

        [JsonPropertyName("subject")]
        public GitHubApiSubject? Subject { get; set; }

        [JsonPropertyName("reason")]
        public string? Reason { get; set; }

        [JsonPropertyName("unread")]
        public bool Unread { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    internal class GitHubApiRepository
    {
        [JsonPropertyName("full_name")]
        public string? FullName { get; set; }
    }

    internal class GitHubApiSubject
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }

    internal class GitHubSearchResult
    {
        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }

        [JsonPropertyName("items")]
        public List<GitHubSearchItem>? Items { get; set; }
    }

    internal class GitHubSearchItem
    {
        [JsonPropertyName("number")]
        public int Number { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("user")]
        public GitHubApiUser? User { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("draft")]
        public bool Draft { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }

        [JsonPropertyName("repository_url")]
        public string? RepositoryUrl { get; set; }

        [JsonPropertyName("comments")]
        public int Comments { get; set; }
    }

    internal class GitHubApiUser
    {
        [JsonPropertyName("login")]
        public string? Login { get; set; }
    }

    internal class GitHubWorkflowRunsResult
    {
        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }

        [JsonPropertyName("workflow_runs")]
        public List<GitHubApiWorkflowRun>? WorkflowRuns { get; set; }
    }

    internal class GitHubApiWorkflowRun
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("conclusion")]
        public string? Conclusion { get; set; }

        [JsonPropertyName("head_branch")]
        public string? HeadBranch { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }
    }

    #endregion
}
