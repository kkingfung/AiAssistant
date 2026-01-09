using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace AiAssistant
{
    /// <summary>
    /// Anthropic Claude APIを使用したAIサービス実装
    /// 会話コンテキストを保持し、ストリーミングレスポンスをサポートします
    /// </summary>
    public sealed class ClaudeAiService : IAiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _model;
        private readonly int _maxTokens;
        private readonly List<ClaudeMessage> _conversationHistory;
        private readonly string _systemPrompt;
        private readonly int _maxHistoryMessages;

        private const string ApiBaseUrl = "https://api.anthropic.com/v1/messages";

        public ClaudeAiService(string? apiKey = null)
        {
            var settings = AppSettings.Instance.Claude;

            // APIキーの取得（引数 > 設定ファイル）
            var key = apiKey ?? settings.ApiKey;

            if (string.IsNullOrWhiteSpace(key) || key == "YOUR_CLAUDE_API_KEY_HERE")
            {
                throw new InvalidOperationException(
                    "Claude APIキーが設定されていません。appsettings.jsonにAPIキーを設定してください。");
            }

            _model = settings.Model;
            _maxTokens = settings.MaxTokens;
            _maxHistoryMessages = 20;

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5)
            };
            _httpClient.DefaultRequestHeaders.Add("x-api-key", key);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            _conversationHistory = new List<ClaudeMessage>();

            _systemPrompt = "あなたは親切で役に立つデスクトップアシスタントです。" +
                           "ユーザーの質問に簡潔かつ正確に答え、必要に応じてタスクをサポートしてください。";
        }

        /// <summary>
        /// 会話履歴をクリアします
        /// </summary>
        public void ClearHistory()
        {
            _conversationHistory.Clear();
        }

        /// <summary>
        /// 完全なレスポンスを一度に取得します
        /// </summary>
        public async Task<string> GetResponseAsync(string prompt, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                return string.Empty;
            }

            try
            {
                // ユーザーメッセージを履歴に追加
                _conversationHistory.Add(new ClaudeMessage
                {
                    Role = "user",
                    Content = prompt
                });

                // 履歴が長すぎる場合は古いメッセージを削除
                TrimHistory();

                // リクエストを作成
                var request = new ClaudeRequest
                {
                    Model = _model,
                    MaxTokens = _maxTokens,
                    System = _systemPrompt,
                    Messages = _conversationHistory
                };

                // APIを呼び出し
                var response = await _httpClient.PostAsJsonAsync(
                    ApiBaseUrl,
                    request,
                    cancellationToken).ConfigureAwait(false);

                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<ClaudeResponse>(
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                var responseText = result?.Content?[0]?.Text ?? string.Empty;

                // アシスタントの応答を履歴に追加
                _conversationHistory.Add(new ClaudeMessage
                {
                    Role = "assistant",
                    Content = responseText
                });

                return responseText;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Claude APIエラー: {ex.Message}");
                return $"エラーが発生しました: {ex.Message}";
            }
        }

        /// <summary>
        /// ストリーミングレスポンスを取得します（トークンごとに返す）
        /// </summary>
        public async IAsyncEnumerable<string> StreamResponseAsync(
            string prompt,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                yield break;
            }

            // ユーザーメッセージを履歴に追加
            _conversationHistory.Add(new ClaudeMessage
            {
                Role = "user",
                Content = prompt
            });

            // 履歴が長すぎる場合は古いメッセージを削除
            TrimHistory();

            var responseBuilder = new StringBuilder();

            // リクエストを作成
            var request = new ClaudeRequest
            {
                Model = _model,
                MaxTokens = _maxTokens,
                System = _systemPrompt,
                Messages = _conversationHistory,
                Stream = true
            };

            var requestContent = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, ApiBaseUrl)
            {
                Content = requestContent
            };

            HttpResponseMessage? response = null;
            System.IO.Stream? stream = null;
            System.IO.StreamReader? reader = null;

            try
            {
                response = await _httpClient.SendAsync(
                    httpRequest,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken).ConfigureAwait(false);

                response.EnsureSuccessStatusCode();

                stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                reader = new System.IO.StreamReader(stream);

                while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);

                    if (string.IsNullOrEmpty(line) || !line.StartsWith("data: "))
                    {
                        continue;
                    }

                    var data = line[6..]; // "data: "を除去

                    if (data == "[DONE]" || data.Contains("message_stop"))
                    {
                        break;
                    }

                    ClaudeStreamEvent? streamEvent = null;
                    try
                    {
                        streamEvent = JsonSerializer.Deserialize<ClaudeStreamEvent>(data);
                    }
                    catch (JsonException)
                    {
                        continue;
                    }

                    if (streamEvent?.Type == "content_block_delta" && streamEvent.Delta?.Text != null)
                    {
                        var text = streamEvent.Delta.Text;
                        responseBuilder.Append(text);
                        yield return text;
                    }
                }
            }
            finally
            {
                reader?.Dispose();
                stream?.Dispose();
                response?.Dispose();
            }

            // アシスタントの完全な応答を履歴に追加
            var fullResponse = responseBuilder.ToString();
            if (!string.IsNullOrEmpty(fullResponse))
            {
                _conversationHistory.Add(new ClaudeMessage
                {
                    Role = "assistant",
                    Content = fullResponse
                });
            }
        }

        /// <summary>
        /// 履歴が長すぎる場合、古いメッセージを削除します
        /// </summary>
        private void TrimHistory()
        {
            if (_conversationHistory.Count <= _maxHistoryMessages)
            {
                return;
            }

            // 古いメッセージを削除
            var recentMessages = _conversationHistory
                .Skip(_conversationHistory.Count - _maxHistoryMessages)
                .ToList();

            _conversationHistory.Clear();
            _conversationHistory.AddRange(recentMessages);
        }

        /// <summary>
        /// 会話履歴のメッセージ数を取得します
        /// </summary>
        public int GetHistoryCount() => _conversationHistory.Count;

        /// <summary>
        /// Claudeが利用可能かチェックします
        /// </summary>
        public static async Task<bool> IsClaudeAvailableAsync(string? apiKey = null)
        {
            try
            {
                var settings = AppSettings.Instance.Claude;
                var key = apiKey ?? settings.ApiKey;

                if (string.IsNullOrWhiteSpace(key) || key == "YOUR_CLAUDE_API_KEY_HERE")
                {
                    return false;
                }

                // 簡単なAPIテスト
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                client.DefaultRequestHeaders.Add("x-api-key", key);
                client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

                var request = new ClaudeRequest
                {
                    Model = settings.Model,
                    MaxTokens = 10,
                    Messages = new List<ClaudeMessage>
                    {
                        new ClaudeMessage { Role = "user", Content = "Hi" }
                    }
                };

                var response = await client.PostAsJsonAsync(ApiBaseUrl, request).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Claude接続チェック失敗: {ex.Message}");
                return false;
            }
        }
    }

    #region Claude API Models

    internal class ClaudeMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    internal class ClaudeRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; } = 1024;

        [JsonPropertyName("system")]
        public string? System { get; set; }

        [JsonPropertyName("messages")]
        public List<ClaudeMessage> Messages { get; set; } = new();

        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;
    }

    internal class ClaudeResponse
    {
        [JsonPropertyName("content")]
        public List<ClaudeContentBlock>? Content { get; set; }
    }

    internal class ClaudeContentBlock
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    internal class ClaudeStreamEvent
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("delta")]
        public ClaudeDelta? Delta { get; set; }
    }

    internal class ClaudeDelta
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    #endregion
}
