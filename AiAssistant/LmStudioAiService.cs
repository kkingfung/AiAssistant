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
    /// LM Studio（OpenAI互換API）を使用したローカルLLMサービス実装
    /// LM StudioはOpenAI API形式のローカルサーバーを提供します
    /// </summary>
    public sealed class LmStudioAiService : IAiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _model;
        private readonly int _maxTokens;
        private readonly float _temperature;
        private readonly List<LmStudioMessage> _conversationHistory;
        private readonly int _maxHistoryMessages;

        public LmStudioAiService(string? endpoint = null, string? model = null)
        {
            var settings = AppSettings.Instance.LmStudio;

            var baseUrl = endpoint ?? settings.Endpoint;
            _model = model ?? settings.Model;
            _maxTokens = settings.MaxTokens;
            _temperature = (float)settings.Temperature;
            _maxHistoryMessages = 20;

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromMinutes(5) // ローカルLLMは時間がかかる場合がある
            };

            _conversationHistory = new List<LmStudioMessage>();

            // システムプロンプトを追加
            _conversationHistory.Add(new LmStudioMessage
            {
                Role = "system",
                Content = "あなたは親切で役に立つデスクトップアシスタントです。" +
                         "ユーザーの質問に簡潔かつ正確に答え、必要に応じてタスクをサポートしてください。"
            });
        }

        /// <summary>
        /// 会話履歴をクリアします
        /// </summary>
        public void ClearHistory()
        {
            _conversationHistory.Clear();

            // システムプロンプトを再追加
            _conversationHistory.Add(new LmStudioMessage
            {
                Role = "system",
                Content = "あなたは親切で役に立つデスクトップアシスタントです。" +
                         "ユーザーの質問に簡潔かつ正確に答え、必要に応じてタスクをサポートしてください。"
            });
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
                _conversationHistory.Add(new LmStudioMessage
                {
                    Role = "user",
                    Content = prompt
                });

                // 履歴が長すぎる場合は古いメッセージを削除
                TrimHistory();

                // リクエストを作成
                var request = new LmStudioRequest
                {
                    Model = _model,
                    Messages = _conversationHistory,
                    MaxTokens = _maxTokens,
                    Temperature = _temperature,
                    Stream = false
                };

                // APIを呼び出し
                var response = await _httpClient.PostAsJsonAsync(
                    "/v1/chat/completions",
                    request,
                    cancellationToken).ConfigureAwait(false);

                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<LmStudioResponse>(
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                var responseText = result?.Choices?[0]?.Message?.Content ?? string.Empty;

                // アシスタントの応答を履歴に追加
                _conversationHistory.Add(new LmStudioMessage
                {
                    Role = "assistant",
                    Content = responseText
                });

                return responseText;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LM Studio APIエラー: {ex.Message}");
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
            _conversationHistory.Add(new LmStudioMessage
            {
                Role = "user",
                Content = prompt
            });

            // 履歴が長すぎる場合は古いメッセージを削除
            TrimHistory();

            var responseBuilder = new StringBuilder();

            // リクエストを作成
            var request = new LmStudioRequest
            {
                Model = _model,
                Messages = _conversationHistory,
                MaxTokens = _maxTokens,
                Temperature = _temperature,
                Stream = true
            };

            var requestContent = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
            {
                Content = requestContent
            };

            using var response = await _httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var reader = new System.IO.StreamReader(stream);

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);

                if (string.IsNullOrEmpty(line) || !line.StartsWith("data: "))
                {
                    continue;
                }

                var data = line[6..]; // "data: "を除去

                if (data == "[DONE]")
                {
                    break;
                }

                LmStudioStreamResponse? chunk = null;
                try
                {
                    chunk = JsonSerializer.Deserialize<LmStudioStreamResponse>(data);
                }
                catch (JsonException)
                {
                    // 不正なJSONは無視
                    continue;
                }

                var content = chunk?.Choices?[0]?.Delta?.Content;

                if (!string.IsNullOrEmpty(content))
                {
                    responseBuilder.Append(content);
                    yield return content;
                }
            }

            // アシスタントの完全な応答を履歴に追加
            var fullResponse = responseBuilder.ToString();
            if (!string.IsNullOrEmpty(fullResponse))
            {
                _conversationHistory.Add(new LmStudioMessage
                {
                    Role = "assistant",
                    Content = fullResponse
                });
            }
        }

        /// <summary>
        /// 履歴が長すぎる場合、古いメッセージを削除します（システムプロンプトは保持）
        /// </summary>
        private void TrimHistory()
        {
            if (_conversationHistory.Count <= _maxHistoryMessages)
            {
                return;
            }

            // システムプロンプト（最初のメッセージ）を保持
            var systemPrompt = _conversationHistory[0];
            var recentMessages = _conversationHistory
                .Skip(_conversationHistory.Count - _maxHistoryMessages + 1)
                .ToList();

            _conversationHistory.Clear();
            _conversationHistory.Add(systemPrompt);
            _conversationHistory.AddRange(recentMessages);
        }

        /// <summary>
        /// LM Studioが利用可能かチェックします
        /// </summary>
        public static async Task<bool> IsLmStudioAvailableAsync(string? endpoint = null)
        {
            try
            {
                var settings = AppSettings.Instance.LmStudio;
                var baseUrl = endpoint ?? settings.Endpoint;

                using var client = new HttpClient
                {
                    BaseAddress = new Uri(baseUrl),
                    Timeout = TimeSpan.FromSeconds(5)
                };

                var response = await client.GetAsync("/v1/models").ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LM Studio接続チェック失敗: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 利用可能なモデル一覧を取得します
        /// </summary>
        public static async Task<List<string>> GetAvailableModelsAsync(string? endpoint = null)
        {
            var modelNames = new List<string>();

            try
            {
                var settings = AppSettings.Instance.LmStudio;
                var baseUrl = endpoint ?? settings.Endpoint;

                using var client = new HttpClient
                {
                    BaseAddress = new Uri(baseUrl),
                    Timeout = TimeSpan.FromSeconds(10)
                };

                var response = await client.GetAsync("/v1/models").ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<LmStudioModelsResponse>().ConfigureAwait(false);

                if (result?.Data != null)
                {
                    foreach (var model in result.Data)
                    {
                        if (!string.IsNullOrEmpty(model.Id))
                        {
                            modelNames.Add(model.Id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LM Studioモデル一覧取得エラー: {ex.Message}");
            }

            return modelNames;
        }

        /// <summary>
        /// 会話履歴のメッセージ数を取得します
        /// </summary>
        public int GetHistoryCount() => _conversationHistory.Count;
    }

    #region LM Studio API Models

    internal class LmStudioMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    internal class LmStudioRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<LmStudioMessage> Messages { get; set; } = new();

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; } = 1024;

        [JsonPropertyName("temperature")]
        public float Temperature { get; set; } = 0.7f;

        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;
    }

    internal class LmStudioResponse
    {
        [JsonPropertyName("choices")]
        public List<LmStudioChoice>? Choices { get; set; }
    }

    internal class LmStudioChoice
    {
        [JsonPropertyName("message")]
        public LmStudioMessage? Message { get; set; }

        [JsonPropertyName("delta")]
        public LmStudioDelta? Delta { get; set; }
    }

    internal class LmStudioDelta
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    internal class LmStudioStreamResponse
    {
        [JsonPropertyName("choices")]
        public List<LmStudioChoice>? Choices { get; set; }
    }

    internal class LmStudioModelsResponse
    {
        [JsonPropertyName("data")]
        public List<LmStudioModel>? Data { get; set; }
    }

    internal class LmStudioModel
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }

    #endregion
}
