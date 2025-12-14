using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AiAssistant
{
    /// <summary>
    /// 簡單 AI 服務介面。
    /// 提供一次性回應與可串流（逐段回傳）兩種非同步 API。
    /// </summary>
    public interface IAiService
    {
        /// <summary>
        /// 取得完整回應（一次性回傳）。
        /// 非同步且可取消，呼叫端應以 await 使用以免阻塞 UI。
        /// </summary>
        Task<string> GetResponseAsync(string prompt, CancellationToken cancellationToken = default);

        /// <summary>
        /// 以字串片段逐步回傳（streaming），適合用於在 UI 顯示逐字/逐段回應。
        /// 每次迭代可視為一個「部分回應」。
        /// </summary>
        IAsyncEnumerable<string> StreamResponseAsync(string prompt, CancellationToken cancellationToken = default);
    }
}