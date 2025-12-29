using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using OpenAI.Chat;

namespace AiAssistant
{
    /// <summary>
    /// OpenAI ChatGPT APIを使用したAIサービス実装
    /// 会話コンテキストを保持し、ストリーミングレスポンスをサポートします
    /// </summary>
    public sealed class ChatGptService : IAiService
    {
        private readonly ChatClient _client;
        private readonly List<ChatMessage> _conversationHistory;
        private readonly string _model;
        private readonly int _maxTokens;
        private readonly float _temperature;
        private readonly int _maxHistoryMessages;

        public ChatGptService(string? apiKey = null)
        {
            var settings = AppSettings.Instance.OpenAI;

            // APIキーの取得（引数 > 設定ファイル）
            var key = apiKey ?? settings.ApiKey;

            if (string.IsNullOrWhiteSpace(key) || key == "YOUR_OPENAI_API_KEY_HERE")
            {
                throw new InvalidOperationException(
                    "OpenAI APIキーが設定されていません。appsettings.jsonにAPIキーを設定してください。");
            }

            _model = settings.Model;
            _maxTokens = settings.MaxTokens;
            _temperature = (float)settings.Temperature;
            _maxHistoryMessages = 20; // 最大履歴メッセージ数

            _client = new ChatClient(_model, key);
            _conversationHistory = new List<ChatMessage>();

            // システムプロンプトを追加
            _conversationHistory.Add(new SystemChatMessage(
                "あなたは親切で役に立つデスクトップアシスタントです。" +
                "ユーザーの質問に簡潔かつ正確に答え、必要に応じてタスクをサポートしてください。"));
        }

        /// <summary>
        /// 会話履歴をクリアします
        /// </summary>
        public void ClearHistory()
        {
            _conversationHistory.Clear();

            // システムプロンプトを再追加
            _conversationHistory.Add(new SystemChatMessage(
                "あなたは親切で役に立つデスクトップアシスタントです。" +
                "ユーザーの質問に簡潔かつ正確に答え、必要に応じてタスクをサポートしてください。"));
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
                _conversationHistory.Add(new UserChatMessage(prompt));

                // 履歴が長すぎる場合は古いメッセージを削除（システムプロンプトは保持）
                TrimHistory();

                // ChatGPT APIを呼び出し
                var completion = await _client.CompleteChatAsync(
                    _conversationHistory,
                    new ChatCompletionOptions
                    {
                        MaxOutputTokenCount = _maxTokens,
                        Temperature = _temperature
                    },
                    cancellationToken).ConfigureAwait(false);

                var responseText = completion.Value.Content[0].Text;

                // アシスタントの応答を履歴に追加
                _conversationHistory.Add(new AssistantChatMessage(responseText));

                return responseText;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ChatGPT APIエラー: {ex.Message}");
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
            _conversationHistory.Add(new UserChatMessage(prompt));

            // 履歴が長すぎる場合は古いメッセージを削除
            TrimHistory();

            var responseBuilder = new System.Text.StringBuilder();

            // ChatGPT APIをストリーミングで呼び出し
            var streamingUpdates = _client.CompleteChatStreamingAsync(
                _conversationHistory,
                new ChatCompletionOptions
                {
                    MaxOutputTokenCount = _maxTokens,
                    Temperature = _temperature
                },
                cancellationToken);

            await foreach (var update in streamingUpdates.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                foreach (var contentPart in update.ContentUpdate)
                {
                    var text = contentPart.Text;
                    if (!string.IsNullOrEmpty(text))
                    {
                        responseBuilder.Append(text);
                        yield return text;
                    }
                }
            }

            // アシスタントの完全な応答を履歴に追加
            var fullResponse = responseBuilder.ToString();
            if (!string.IsNullOrEmpty(fullResponse))
            {
                _conversationHistory.Add(new AssistantChatMessage(fullResponse));
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

            // システムプロンプト（最初のメッセージ）を保持し、古いメッセージを削除
            var systemPrompt = _conversationHistory.First();
            var recentMessages = _conversationHistory
                .Skip(_conversationHistory.Count - _maxHistoryMessages + 1)
                .ToList();

            _conversationHistory.Clear();
            _conversationHistory.Add(systemPrompt);
            _conversationHistory.AddRange(recentMessages);
        }

        /// <summary>
        /// 会話履歴のメッセージ数を取得します
        /// </summary>
        public int GetHistoryCount() => _conversationHistory.Count;
    }
}
