using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AiAssistant
{
    /// <summary>
    /// 模擬的 AI 服務（非阻塞、可取消）。
    /// GetResponseAsync 會在假延遲後回傳完整回應。
    /// StreamResponseAsync 會模擬逐段（token-like）回傳，適合在 UI 上逐步顯示。
    /// </summary>
    public sealed class MockAiService : IAiService
    {
        private readonly Random _rnd = new();

        public MockAiService()
        {
        }

        public async Task<string> GetResponseAsync(string prompt, CancellationToken cancellationToken = default)
        {
            // 模擬思考時間（500ms ~ 1500ms）
            var delay = _rnd.Next(500, 1500);
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            // 建立假回應（可替換為更複雜邏輯）
            var response = $"(mock) Received: \"{Shorten(prompt)}\" — response generated at {DateTime.Now:T}";
            return response;
        }

        public async IAsyncEnumerable<string> StreamResponseAsync(string prompt, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // 模擬一段要回傳的完整字串，然後分割成若干片段逐步回傳
            var full = $"(mock streaming) Answer for: {Shorten(prompt)}. This is a simulated streaming response generated at {DateTime.Now:T}.";
            var parts = ChunkIntoWords(full, maxWordsPerChunk: 4);

            foreach (var part in parts)
            {
                // 每段之間等待一下以模擬 token arrival
                await Task.Delay(_rnd.Next(80, 220), cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                yield return part;
            }

            // 最後再等一小段以模擬收尾
            await Task.Delay(120, cancellationToken).ConfigureAwait(false);
        }

        // Helper: 把字串切成每片數個單字，方便模擬分段回傳
        private static IEnumerable<string> ChunkIntoWords(string text, int maxWordsPerChunk)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                yield break;
            }

            var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < words.Length; i += maxWordsPerChunk)
            {
                yield return string.Join(' ', words.Skip(i).Take(maxWordsPerChunk));
            }
        }

        // Helper: 縮短 prompt 用於 mock 回應（避免過長）
        private static string Shorten(string s, int max = 60)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Length <= max ? s : s.Substring(0, max - 3) + "...";
        }
    }
}