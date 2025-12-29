using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;

namespace AiAssistant
{
    /// <summary>
    /// Ollama を使用したローカルLLMサービス実装
    /// OllamaSharpライブラリを使用してローカルでLLMを実行します
    /// </summary>
    public sealed class OllamaAiService : IAiService
    {
        private readonly OllamaApiClient _client;
        private readonly string _model;
        private Chat? _currentChat;

        public OllamaAiService(string endpoint = "http://localhost:11434", string model = "phi3:mini")
        {
            _client = new OllamaApiClient(new Uri(endpoint));
            _model = model;
            _client.SelectedModel = _model;
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
                var chat = new Chat(_client);
                var response = new StringBuilder();

                await foreach (var token in chat.SendAsync(prompt, cancellationToken))
                {
                    response.Append(token);
                }

                return response.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ollama APIエラー: {ex.Message}");
                return $"エラーが発生しました: {ex.Message}";
            }
        }

        /// <summary>
        /// ストリーミングレスポンスを取得します（トークンごとに返す）
        /// 会話コンテキストを保持します
        /// </summary>
        public async IAsyncEnumerable<string> StreamResponseAsync(
            string prompt,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                yield break;
            }

            // 会話コンテキストを保持するため、チャットインスタンスを再利用
            _currentChat ??= new Chat(_client);

            await foreach (var token in _currentChat.SendAsync(prompt, cancellationToken))
            {
                yield return token;
            }
        }

        /// <summary>
        /// 会話履歴をクリアします
        /// </summary>
        public void ClearHistory()
        {
            _currentChat = new Chat(_client);
        }

        /// <summary>
        /// Ollamaが利用可能かチェックします
        /// </summary>
        public static async Task<bool> IsOllamaAvailableAsync(string endpoint = "http://localhost:11434")
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                var response = await client.GetAsync($"{endpoint}/api/tags");
                Console.WriteLine($"[Ollama] 接続成功: {response.StatusCode}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Ollama] 接続失敗: {ex.GetType().Name} - {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 指定されたモデルがダウンロード済みかチェックします
        /// </summary>
        public static async Task<bool> IsModelAvailableAsync(string endpoint, string modelName)
        {
            try
            {
                var client = new OllamaApiClient(new Uri(endpoint));
                var models = await client.ListLocalModelsAsync();

                System.Diagnostics.Debug.WriteLine($"[Ollama] 検索対象モデル: {modelName}");

                var modelList = models.ToList();
                System.Diagnostics.Debug.WriteLine($"[Ollama] 利用可能なモデル数: {modelList.Count}");

                foreach (var model in modelList)
                {
                    System.Diagnostics.Debug.WriteLine($"[Ollama] チェック中: {model.Name}");

                    if (model.Name.Equals(modelName, StringComparison.OrdinalIgnoreCase) ||
                        model.Name.StartsWith(modelName + ":", StringComparison.OrdinalIgnoreCase))
                    {
                        System.Diagnostics.Debug.WriteLine($"[Ollama] モデル一致: {model.Name}");
                        return true;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[Ollama] モデル '{modelName}' が見つかりませんでした");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Ollama] エラー: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 使用可能なモデル一覧を取得します
        /// </summary>
        public static async Task<List<string>> GetAvailableModelsAsync(string endpoint = "http://localhost:11434")
        {
            var modelNames = new List<string>();

            try
            {
                var client = new OllamaApiClient(new Uri(endpoint));
                var models = await client.ListLocalModelsAsync();

                foreach (var model in models)
                {
                    modelNames.Add(model.Name);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"モデル一覧取得エラー: {ex.Message}");
            }

            return modelNames;
        }
    }
}
