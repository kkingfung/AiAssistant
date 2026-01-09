using System.Threading;
using System.Threading.Tasks;

namespace AiAssistant
{
    /// <summary>
    /// 翻訳サービスのインターフェース
    /// テキストの翻訳と言語検出機能を提供します
    /// </summary>
    public interface ITranslationService
    {
        /// <summary>
        /// テキストを指定した言語に翻訳します
        /// </summary>
        /// <param name="text">翻訳するテキスト</param>
        /// <param name="targetLanguage">翻訳先の言語（例: "Japanese", "English", "Korean", "Chinese"）</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        /// <returns>翻訳されたテキスト</returns>
        Task<TranslationResult> TranslateAsync(string text, string targetLanguage, CancellationToken cancellationToken = default);

        /// <summary>
        /// テキストの言語を検出し、適切な言語に自動翻訳します
        /// 日本語 → 英語、その他 → 日本語
        /// </summary>
        /// <param name="text">翻訳するテキスト</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        /// <returns>翻訳結果</returns>
        Task<TranslationResult> AutoTranslateAsync(string text, CancellationToken cancellationToken = default);

        /// <summary>
        /// テキストの言語を検出します
        /// </summary>
        /// <param name="text">言語を検出するテキスト</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        /// <returns>検出された言語コード</returns>
        Task<string> DetectLanguageAsync(string text, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 翻訳結果を表すクラス
    /// </summary>
    public class TranslationResult
    {
        /// <summary>
        /// 元のテキスト
        /// </summary>
        public string OriginalText { get; set; } = string.Empty;

        /// <summary>
        /// 翻訳されたテキスト
        /// </summary>
        public string TranslatedText { get; set; } = string.Empty;

        /// <summary>
        /// 検出された元の言語
        /// </summary>
        public string DetectedLanguage { get; set; } = string.Empty;

        /// <summary>
        /// 翻訳先の言語
        /// </summary>
        public string TargetLanguage { get; set; } = string.Empty;

        /// <summary>
        /// 翻訳が成功したかどうか
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// エラーメッセージ（失敗時）
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
