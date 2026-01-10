using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AiAssistant
{
    /// <summary>
    /// 教育サービスのインターフェース
    /// 言語学習とプログラミング学習機能を提供します
    /// </summary>
    public interface IEducationService
    {
        /// <summary>
        /// 言語学習：文法をチェックして修正提案を返します
        /// </summary>
        /// <param name="text">チェックするテキスト</param>
        /// <param name="language">言語（English, Japanese, Korean）</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        Task<GrammarCheckResult> CheckGrammarAsync(string text, SupportedLanguage language, CancellationToken cancellationToken = default);

        /// <summary>
        /// 言語学習：単語やフレーズの説明を取得します
        /// </summary>
        /// <param name="word">単語またはフレーズ</param>
        /// <param name="language">言語</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        Task<WordExplanationResult> ExplainWordAsync(string word, SupportedLanguage language, CancellationToken cancellationToken = default);

        /// <summary>
        /// 言語学習：会話練習のレスポンスを生成します
        /// </summary>
        /// <param name="userMessage">ユーザーのメッセージ</param>
        /// <param name="language">練習する言語</param>
        /// <param name="level">レベル（Beginner, Intermediate, Advanced）</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        Task<ConversationPracticeResult> PracticeConversationAsync(string userMessage, SupportedLanguage language, LanguageLevel level, CancellationToken cancellationToken = default);

        /// <summary>
        /// プログラミング：コードレビューを行います
        /// </summary>
        /// <param name="code">レビューするコード</param>
        /// <param name="language">プログラミング言語</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        Task<CodeReviewResult> ReviewCodeAsync(string code, ProgrammingLanguage language, CancellationToken cancellationToken = default);

        /// <summary>
        /// プログラミング：エラーの解説とデバッグヘルプを提供します
        /// </summary>
        /// <param name="code">問題のあるコード</param>
        /// <param name="errorMessage">エラーメッセージ</param>
        /// <param name="language">プログラミング言語</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        Task<DebugHelpResult> ExplainErrorAsync(string code, string errorMessage, ProgrammingLanguage language, CancellationToken cancellationToken = default);

        /// <summary>
        /// プログラミング：概念やパターンを説明します
        /// </summary>
        /// <param name="topic">トピック（例: "async/await", "SOLID principles"）</param>
        /// <param name="language">プログラミング言語（例示用）</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        Task<ConceptExplanationResult> ExplainConceptAsync(string topic, ProgrammingLanguage language, CancellationToken cancellationToken = default);

        /// <summary>
        /// プログラミング：コードの改善提案を生成します
        /// </summary>
        /// <param name="code">改善するコード</param>
        /// <param name="language">プログラミング言語</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        Task<CodeImprovementResult> SuggestImprovementsAsync(string code, ProgrammingLanguage language, CancellationToken cancellationToken = default);

        /// <summary>
        /// ランダム学習：ランダムな単語/フレーズを出題して解説します
        /// </summary>
        /// <param name="language">言語</param>
        /// <param name="level">難易度レベル</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        Task<RandomWordResult> GetRandomWordAsync(SupportedLanguage language, LanguageLevel level, CancellationToken cancellationToken = default);

        /// <summary>
        /// ランダム学習：ランダムなAPI/ライブラリを解説します
        /// </summary>
        /// <param name="language">プログラミング言語</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        Task<RandomApiResult> GetRandomApiAsync(ProgrammingLanguage language, CancellationToken cancellationToken = default);

        /// <summary>
        /// ランダム学習：ランダムなプログラミング概念を解説します
        /// </summary>
        /// <param name="language">プログラミング言語</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        Task<RandomConceptResult> GetRandomConceptAsync(ProgrammingLanguage language, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// サポートする言語（自然言語）
    /// </summary>
    public enum SupportedLanguage
    {
        English,
        Japanese,
        Korean
    }

    /// <summary>
    /// 言語学習レベル
    /// </summary>
    public enum LanguageLevel
    {
        Beginner,
        Intermediate,
        Advanced
    }

    /// <summary>
    /// サポートするプログラミング言語
    /// </summary>
    public enum ProgrammingLanguage
    {
        CSharp,
        Python,
        JavaScript,
        TypeScript,
        Java,
        Rust,
        Go,
        Ruby,
        PHP,
        C,
        Cpp,
        Swift,
        Kotlin
    }

    /// <summary>
    /// 文法チェック結果
    /// </summary>
    public class GrammarCheckResult
    {
        public string OriginalText { get; set; } = string.Empty;
        public string CorrectedText { get; set; } = string.Empty;
        public List<GrammarIssue> Issues { get; set; } = new();
        public bool HasIssues => Issues.Count > 0;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 文法の問題
    /// </summary>
    public class GrammarIssue
    {
        public string Original { get; set; } = string.Empty;
        public string Correction { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
    }

    /// <summary>
    /// 単語説明結果
    /// </summary>
    public class WordExplanationResult
    {
        public string Word { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public string Pronunciation { get; set; } = string.Empty;
        public List<string> ExampleSentences { get; set; } = new();
        public List<string> Synonyms { get; set; } = new();
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 会話練習結果
    /// </summary>
    public class ConversationPracticeResult
    {
        public string UserMessage { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public string? CorrectionNote { get; set; }
        public string? VocabularyTip { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// コードレビュー結果
    /// </summary>
    public class CodeReviewResult
    {
        public string OriginalCode { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public List<CodeReviewItem> Items { get; set; } = new();
        public int OverallScore { get; set; } // 1-10
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// コードレビュー項目
    /// </summary>
    public class CodeReviewItem
    {
        public string Category { get; set; } = string.Empty; // "Bug", "Performance", "Style", "Security"
        public string Severity { get; set; } = string.Empty; // "Critical", "Warning", "Info"
        public string Description { get; set; } = string.Empty;
        public string? Suggestion { get; set; }
        public int? LineNumber { get; set; }
    }

    /// <summary>
    /// デバッグヘルプ結果
    /// </summary>
    public class DebugHelpResult
    {
        public string ErrorMessage { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public string PossibleCause { get; set; } = string.Empty;
        public string SuggestedFix { get; set; } = string.Empty;
        public string? FixedCode { get; set; }
        public bool IsSuccess { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// 概念説明結果
    /// </summary>
    public class ConceptExplanationResult
    {
        public string Topic { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public string? CodeExample { get; set; }
        public List<string> KeyPoints { get; set; } = new();
        public List<string> RelatedTopics { get; set; } = new();
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// コード改善結果
    /// </summary>
    public class CodeImprovementResult
    {
        public string OriginalCode { get; set; } = string.Empty;
        public string ImprovedCode { get; set; } = string.Empty;
        public List<ImprovementItem> Improvements { get; set; } = new();
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 改善項目
    /// </summary>
    public class ImprovementItem
    {
        public string Category { get; set; } = string.Empty; // "Readability", "Performance", "BestPractice"
        public string Before { get; set; } = string.Empty;
        public string After { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
    }

    /// <summary>
    /// ランダム単語学習結果
    /// </summary>
    public class RandomWordResult
    {
        public string Word { get; set; } = string.Empty;
        public string Pronunciation { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public List<string> ExampleSentences { get; set; } = new();
        public string UsageNote { get; set; } = string.Empty;
        public List<string> RelatedWords { get; set; } = new();
        public string Category { get; set; } = string.Empty; // "Daily", "Business", "Idiom", "Slang"
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// ランダムAPI学習結果
    /// </summary>
    public class RandomApiResult
    {
        public string ApiName { get; set; } = string.Empty;
        public string Library { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Syntax { get; set; } = string.Empty;
        public List<string> Parameters { get; set; } = new();
        public string ReturnValue { get; set; } = string.Empty;
        public string CodeExample { get; set; } = string.Empty;
        public List<string> UseCases { get; set; } = new();
        public string Tip { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// ランダム概念学習結果
    /// </summary>
    public class RandomConceptResult
    {
        public string ConceptName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // "DesignPattern", "Algorithm", "DataStructure", "BestPractice"
        public string Explanation { get; set; } = string.Empty;
        public List<string> KeyPoints { get; set; } = new();
        public string CodeExample { get; set; } = string.Empty;
        public string WhenToUse { get; set; } = string.Empty;
        public List<string> RelatedConcepts { get; set; } = new();
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
