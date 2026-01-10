using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AiAssistant
{
    /// <summary>
    /// AIサービスを使用した教育サービス実装
    /// 言語学習とプログラミング学習機能を提供します
    /// </summary>
    public sealed class EducationService : IEducationService
    {
        private readonly IAiService _aiService;

        /// <summary>
        /// EducationServiceのコンストラクタ
        /// </summary>
        /// <param name="aiService">使用するAIサービス</param>
        public EducationService(IAiService aiService)
        {
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
        }

        #region Language Learning

        /// <summary>
        /// 文法チェックを行います
        /// </summary>
        public async Task<GrammarCheckResult> CheckGrammarAsync(string text, SupportedLanguage language, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new GrammarCheckResult
                {
                    IsSuccess = false,
                    ErrorMessage = "テキストが空です"
                };
            }

            try
            {
                var languageName = GetLanguageName(language);
                var prompt = $@"You are a {languageName} language teacher. Check the following text for grammar, spelling, and natural expression issues.

Text to check:
""{text}""

Respond in this exact format:
CORRECTED: [corrected text - if no issues, repeat the original]
ISSUES:
- ORIGINAL: [problematic part] | CORRECTION: [correction] | EXPLANATION: [brief explanation in Japanese]
- (repeat for each issue, or write ""None"" if no issues)

Important:
- Be thorough but constructive
- Explain each issue clearly in Japanese
- Consider natural expression, not just grammar";

                var response = await _aiService.GetResponseAsync(prompt, cancellationToken);
                return ParseGrammarCheckResponse(response, text);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"文法チェックエラー: {ex.Message}");
                return new GrammarCheckResult
                {
                    OriginalText = text,
                    IsSuccess = false,
                    ErrorMessage = $"文法チェックに失敗しました: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 単語やフレーズの説明を取得します
        /// </summary>
        public async Task<WordExplanationResult> ExplainWordAsync(string word, SupportedLanguage language, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                return new WordExplanationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "単語が空です"
                };
            }

            try
            {
                var languageName = GetLanguageName(language);
                var prompt = $@"Explain the following {languageName} word/phrase in Japanese:

Word: ""{word}""

Respond in this exact format:
DEFINITION: [meaning in Japanese]
PRONUNCIATION: [pronunciation guide]
EXAMPLES:
- [example sentence 1]
- [example sentence 2]
- [example sentence 3]
SYNONYMS: [synonym1], [synonym2], [synonym3]

Important: Keep explanations clear and helpful for language learners.";

                var response = await _aiService.GetResponseAsync(prompt, cancellationToken);
                return ParseWordExplanationResponse(response, word);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"単語説明エラー: {ex.Message}");
                return new WordExplanationResult
                {
                    Word = word,
                    IsSuccess = false,
                    ErrorMessage = $"単語の説明に失敗しました: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 会話練習を行います
        /// </summary>
        public async Task<ConversationPracticeResult> PracticeConversationAsync(string userMessage, SupportedLanguage language, LanguageLevel level, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                return new ConversationPracticeResult
                {
                    IsSuccess = false,
                    ErrorMessage = "メッセージが空です"
                };
            }

            try
            {
                var languageName = GetLanguageName(language);
                var levelName = level.ToString();
                var prompt = $@"You are a friendly {languageName} conversation partner for a {levelName} level learner.

User's message: ""{userMessage}""

Instructions:
1. Respond naturally in {languageName} appropriate for {levelName} level
2. If there are mistakes in the user's message, gently note them
3. Occasionally introduce useful vocabulary

Respond in this format:
RESPONSE: [your conversational response in {languageName}]
CORRECTION: [if the user made mistakes, explain in Japanese. Write ""なし"" if no mistakes]
VOCABULARY: [introduce one useful word/phrase with explanation in Japanese. Write ""なし"" if not applicable]";

                var response = await _aiService.GetResponseAsync(prompt, cancellationToken);
                return ParseConversationResponse(response, userMessage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"会話練習エラー: {ex.Message}");
                return new ConversationPracticeResult
                {
                    UserMessage = userMessage,
                    IsSuccess = false,
                    ErrorMessage = $"会話練習に失敗しました: {ex.Message}"
                };
            }
        }

        #endregion

        #region Programming Learning

        /// <summary>
        /// コードレビューを行います
        /// </summary>
        public async Task<CodeReviewResult> ReviewCodeAsync(string code, ProgrammingLanguage language, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return new CodeReviewResult
                {
                    IsSuccess = false,
                    ErrorMessage = "コードが空です"
                };
            }

            try
            {
                var langName = GetProgrammingLanguageName(language);
                var prompt = $@"Review the following {langName} code and provide constructive feedback.

```{langName.ToLower()}
{code}
```

Respond in this format:
SUMMARY: [brief overall assessment in Japanese]
SCORE: [1-10]
ITEMS:
- CATEGORY: [Bug/Performance/Style/Security] | SEVERITY: [Critical/Warning/Info] | LINE: [line number or ""N/A""] | DESCRIPTION: [issue description in Japanese] | SUGGESTION: [fix suggestion in Japanese]
- (repeat for each issue)

Focus on:
- Bugs and potential errors
- Performance issues
- Code style and readability
- Security concerns
- Best practices";

                var response = await _aiService.GetResponseAsync(prompt, cancellationToken);
                return ParseCodeReviewResponse(response, code);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"コードレビューエラー: {ex.Message}");
                return new CodeReviewResult
                {
                    OriginalCode = code,
                    IsSuccess = false,
                    ErrorMessage = $"コードレビューに失敗しました: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// エラーの解説とデバッグヘルプを提供します
        /// </summary>
        public async Task<DebugHelpResult> ExplainErrorAsync(string code, string errorMessage, ProgrammingLanguage language, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                return new DebugHelpResult
                {
                    IsSuccess = false,
                    Error = "エラーメッセージが空です"
                };
            }

            try
            {
                var langName = GetProgrammingLanguageName(language);
                var codeSection = string.IsNullOrWhiteSpace(code) ? "" : $@"

Code:
```{langName.ToLower()}
{code}
```";

                var prompt = $@"Explain this {langName} error and help debug it.

Error message: {errorMessage}{codeSection}

Respond in this format (in Japanese):
EXPLANATION: [what this error means]
CAUSE: [why this error occurred]
FIX: [how to fix it]
FIXED_CODE: [corrected code if applicable, or ""N/A""]";

                var response = await _aiService.GetResponseAsync(prompt, cancellationToken);
                return ParseDebugHelpResponse(response, errorMessage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"デバッグヘルプエラー: {ex.Message}");
                return new DebugHelpResult
                {
                    ErrorMessage = errorMessage,
                    IsSuccess = false,
                    Error = $"デバッグヘルプに失敗しました: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 概念やパターンを説明します
        /// </summary>
        public async Task<ConceptExplanationResult> ExplainConceptAsync(string topic, ProgrammingLanguage language, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                return new ConceptExplanationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "トピックが空です"
                };
            }

            try
            {
                var langName = GetProgrammingLanguageName(language);
                var prompt = $@"Explain the programming concept ""{topic}"" with examples in {langName}.

Respond in this format (in Japanese):
EXPLANATION: [clear explanation of the concept]
CODE_EXAMPLE:
```{langName.ToLower()}
[practical code example]
```
KEY_POINTS:
- [key point 1]
- [key point 2]
- [key point 3]
RELATED: [related topic 1], [related topic 2], [related topic 3]";

                var response = await _aiService.GetResponseAsync(prompt, cancellationToken);
                return ParseConceptExplanationResponse(response, topic);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"概念説明エラー: {ex.Message}");
                return new ConceptExplanationResult
                {
                    Topic = topic,
                    IsSuccess = false,
                    ErrorMessage = $"概念の説明に失敗しました: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// コードの改善提案を生成します
        /// </summary>
        public async Task<CodeImprovementResult> SuggestImprovementsAsync(string code, ProgrammingLanguage language, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return new CodeImprovementResult
                {
                    IsSuccess = false,
                    ErrorMessage = "コードが空です"
                };
            }

            try
            {
                var langName = GetProgrammingLanguageName(language);
                var prompt = $@"Suggest improvements for this {langName} code.

```{langName.ToLower()}
{code}
```

Respond in this format:
IMPROVED_CODE:
```{langName.ToLower()}
[improved code]
```
IMPROVEMENTS:
- CATEGORY: [Readability/Performance/BestPractice] | BEFORE: [original code snippet] | AFTER: [improved code snippet] | EXPLANATION: [why this is better, in Japanese]
- (repeat for each improvement)

Focus on practical improvements that make the code better.";

                var response = await _aiService.GetResponseAsync(prompt, cancellationToken);
                return ParseCodeImprovementResponse(response, code);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"コード改善エラー: {ex.Message}");
                return new CodeImprovementResult
                {
                    OriginalCode = code,
                    IsSuccess = false,
                    ErrorMessage = $"コード改善の提案に失敗しました: {ex.Message}"
                };
            }
        }

        #endregion

        #region Random Learning

        /// <summary>
        /// ランダムな単語/フレーズを出題して解説します
        /// </summary>
        public async Task<RandomWordResult> GetRandomWordAsync(SupportedLanguage language, LanguageLevel level, CancellationToken cancellationToken = default)
        {
            try
            {
                var languageName = GetLanguageName(language);
                var levelName = level.ToString();
                var prompt = $@"You are a {languageName} language teacher. Pick a random useful word or phrase for a {levelName} level learner.

Choose something practical and commonly used. DO NOT ask the user to pick - you must choose one.

Respond in this exact format:
WORD: [the word or phrase in {languageName}]
PRONUNCIATION: [pronunciation guide]
DEFINITION: [meaning in Japanese]
CATEGORY: [Daily/Business/Idiom/Slang]
EXAMPLES:
- [example sentence 1]
- [example sentence 2]
USAGE_NOTE: [when/how to use this word, in Japanese]
RELATED: [related word 1], [related word 2], [related word 3]

Be creative and pick something interesting and useful!";

                var response = await _aiService.GetResponseAsync(prompt, cancellationToken);
                return ParseRandomWordResponse(response);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ランダム単語エラー: {ex.Message}");
                return new RandomWordResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"ランダム単語の取得に失敗しました: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// ランダムなAPI/ライブラリを解説します
        /// </summary>
        public async Task<RandomApiResult> GetRandomApiAsync(ProgrammingLanguage language, CancellationToken cancellationToken = default)
        {
            try
            {
                var langName = GetProgrammingLanguageName(language);
                var prompt = $@"You are a {langName} programming teacher. Pick a random useful API, method, or library feature that developers should know.

Choose something practical - could be from the standard library, a common framework, or a useful pattern. DO NOT ask the user to pick - you must choose one.

Respond in this exact format:
API_NAME: [the API/method name]
LIBRARY: [which library or framework it belongs to]
DESCRIPTION: [what it does, in Japanese]
SYNTAX: [the basic syntax/signature]
PARAMETERS:
- [parameter 1]: [description]
- [parameter 2]: [description]
RETURN: [what it returns, in Japanese]
CODE_EXAMPLE:
```{langName.ToLower()}
[practical code example showing usage]
```
USE_CASES:
- [use case 1, in Japanese]
- [use case 2, in Japanese]
TIP: [a helpful tip about using this API, in Japanese]

Pick something interesting that would surprise and educate the developer!";

                var response = await _aiService.GetResponseAsync(prompt, cancellationToken);
                return ParseRandomApiResponse(response);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ランダムAPIエラー: {ex.Message}");
                return new RandomApiResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"ランダムAPIの取得に失敗しました: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// ランダムなプログラミング概念を解説します
        /// </summary>
        public async Task<RandomConceptResult> GetRandomConceptAsync(ProgrammingLanguage language, CancellationToken cancellationToken = default)
        {
            try
            {
                var langName = GetProgrammingLanguageName(language);
                var prompt = $@"You are a {langName} programming teacher. Pick a random programming concept, design pattern, algorithm, or best practice that developers should know.

Choose something educational - could be a design pattern, algorithm, data structure, architectural principle, or best practice. DO NOT ask the user to pick - you must choose one.

Respond in this exact format:
CONCEPT: [the concept name]
CATEGORY: [DesignPattern/Algorithm/DataStructure/BestPractice/Architecture]
EXPLANATION: [clear explanation in Japanese]
KEY_POINTS:
- [key point 1, in Japanese]
- [key point 2, in Japanese]
- [key point 3, in Japanese]
CODE_EXAMPLE:
```{langName.ToLower()}
[practical code example demonstrating the concept]
```
WHEN_TO_USE: [when and why to use this, in Japanese]
RELATED: [related concept 1], [related concept 2], [related concept 3]

Pick something that will truly expand the developer's knowledge!";

                var response = await _aiService.GetResponseAsync(prompt, cancellationToken);
                return ParseRandomConceptResponse(response);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ランダム概念エラー: {ex.Message}");
                return new RandomConceptResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"ランダム概念の取得に失敗しました: {ex.Message}"
                };
            }
        }

        #endregion

        #region Helper Methods

        private static string GetLanguageName(SupportedLanguage language)
        {
            return language switch
            {
                SupportedLanguage.English => "English",
                SupportedLanguage.Japanese => "Japanese",
                SupportedLanguage.Korean => "Korean",
                _ => "English"
            };
        }

        private static string GetProgrammingLanguageName(ProgrammingLanguage language)
        {
            return language switch
            {
                ProgrammingLanguage.CSharp => "C#",
                ProgrammingLanguage.Python => "Python",
                ProgrammingLanguage.JavaScript => "JavaScript",
                ProgrammingLanguage.TypeScript => "TypeScript",
                ProgrammingLanguage.Java => "Java",
                ProgrammingLanguage.Rust => "Rust",
                ProgrammingLanguage.Go => "Go",
                ProgrammingLanguage.Ruby => "Ruby",
                ProgrammingLanguage.PHP => "PHP",
                ProgrammingLanguage.C => "C",
                ProgrammingLanguage.Cpp => "C++",
                ProgrammingLanguage.Swift => "Swift",
                ProgrammingLanguage.Kotlin => "Kotlin",
                _ => "C#"
            };
        }

        private static GrammarCheckResult ParseGrammarCheckResponse(string response, string originalText)
        {
            var result = new GrammarCheckResult
            {
                OriginalText = originalText,
                IsSuccess = true
            };

            var lines = response.Split('\n');
            var issueLines = new List<string>();
            bool inIssues = false;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("CORRECTED:", StringComparison.OrdinalIgnoreCase))
                {
                    result.CorrectedText = trimmed[10..].Trim();
                }
                else if (trimmed.StartsWith("ISSUES:", StringComparison.OrdinalIgnoreCase))
                {
                    inIssues = true;
                }
                else if (inIssues && trimmed.StartsWith("-"))
                {
                    var issueLine = trimmed[1..].Trim();
                    if (!issueLine.Equals("None", StringComparison.OrdinalIgnoreCase))
                    {
                        issueLines.Add(issueLine);
                    }
                }
            }

            foreach (var issueLine in issueLines)
            {
                var issue = ParseGrammarIssue(issueLine);
                if (issue != null)
                {
                    result.Issues.Add(issue);
                }
            }

            if (string.IsNullOrEmpty(result.CorrectedText))
            {
                result.CorrectedText = originalText;
            }

            return result;
        }

        private static GrammarIssue? ParseGrammarIssue(string line)
        {
            var parts = line.Split('|');
            if (parts.Length < 3) return null;

            var issue = new GrammarIssue();

            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.StartsWith("ORIGINAL:", StringComparison.OrdinalIgnoreCase))
                {
                    issue.Original = trimmed[9..].Trim();
                }
                else if (trimmed.StartsWith("CORRECTION:", StringComparison.OrdinalIgnoreCase))
                {
                    issue.Correction = trimmed[11..].Trim();
                }
                else if (trimmed.StartsWith("EXPLANATION:", StringComparison.OrdinalIgnoreCase))
                {
                    issue.Explanation = trimmed[12..].Trim();
                }
            }

            return string.IsNullOrEmpty(issue.Original) ? null : issue;
        }

        private static WordExplanationResult ParseWordExplanationResponse(string response, string word)
        {
            var result = new WordExplanationResult
            {
                Word = word,
                IsSuccess = true
            };

            var lines = response.Split('\n');
            bool inExamples = false;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("DEFINITION:", StringComparison.OrdinalIgnoreCase))
                {
                    result.Definition = trimmed[11..].Trim();
                    inExamples = false;
                }
                else if (trimmed.StartsWith("PRONUNCIATION:", StringComparison.OrdinalIgnoreCase))
                {
                    result.Pronunciation = trimmed[14..].Trim();
                    inExamples = false;
                }
                else if (trimmed.StartsWith("EXAMPLES:", StringComparison.OrdinalIgnoreCase))
                {
                    inExamples = true;
                }
                else if (trimmed.StartsWith("SYNONYMS:", StringComparison.OrdinalIgnoreCase))
                {
                    inExamples = false;
                    var synonyms = trimmed[9..].Trim();
                    result.Synonyms = new List<string>(synonyms.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
                }
                else if (inExamples && trimmed.StartsWith("-"))
                {
                    result.ExampleSentences.Add(trimmed[1..].Trim());
                }
            }

            return result;
        }

        private static ConversationPracticeResult ParseConversationResponse(string response, string userMessage)
        {
            var result = new ConversationPracticeResult
            {
                UserMessage = userMessage,
                IsSuccess = true
            };

            var lines = response.Split('\n');

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("RESPONSE:", StringComparison.OrdinalIgnoreCase))
                {
                    result.Response = trimmed[9..].Trim();
                }
                else if (trimmed.StartsWith("CORRECTION:", StringComparison.OrdinalIgnoreCase))
                {
                    var correction = trimmed[11..].Trim();
                    if (!correction.Equals("なし", StringComparison.OrdinalIgnoreCase) && !correction.Equals("None", StringComparison.OrdinalIgnoreCase))
                    {
                        result.CorrectionNote = correction;
                    }
                }
                else if (trimmed.StartsWith("VOCABULARY:", StringComparison.OrdinalIgnoreCase))
                {
                    var vocab = trimmed[11..].Trim();
                    if (!vocab.Equals("なし", StringComparison.OrdinalIgnoreCase) && !vocab.Equals("None", StringComparison.OrdinalIgnoreCase))
                    {
                        result.VocabularyTip = vocab;
                    }
                }
            }

            if (string.IsNullOrEmpty(result.Response))
            {
                result.Response = response.Trim();
            }

            return result;
        }

        private static CodeReviewResult ParseCodeReviewResponse(string response, string code)
        {
            var result = new CodeReviewResult
            {
                OriginalCode = code,
                IsSuccess = true,
                OverallScore = 5
            };

            var lines = response.Split('\n');
            bool inItems = false;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("SUMMARY:", StringComparison.OrdinalIgnoreCase))
                {
                    result.Summary = trimmed[8..].Trim();
                    inItems = false;
                }
                else if (trimmed.StartsWith("SCORE:", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(trimmed[6..].Trim(), out int score))
                    {
                        result.OverallScore = Math.Clamp(score, 1, 10);
                    }
                    inItems = false;
                }
                else if (trimmed.StartsWith("ITEMS:", StringComparison.OrdinalIgnoreCase))
                {
                    inItems = true;
                }
                else if (inItems && trimmed.StartsWith("-"))
                {
                    var item = ParseCodeReviewItem(trimmed[1..].Trim());
                    if (item != null)
                    {
                        result.Items.Add(item);
                    }
                }
            }

            return result;
        }

        private static CodeReviewItem? ParseCodeReviewItem(string line)
        {
            var parts = line.Split('|');
            if (parts.Length < 2) return null;

            var item = new CodeReviewItem();

            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.StartsWith("CATEGORY:", StringComparison.OrdinalIgnoreCase))
                {
                    item.Category = trimmed[9..].Trim();
                }
                else if (trimmed.StartsWith("SEVERITY:", StringComparison.OrdinalIgnoreCase))
                {
                    item.Severity = trimmed[9..].Trim();
                }
                else if (trimmed.StartsWith("LINE:", StringComparison.OrdinalIgnoreCase))
                {
                    var lineStr = trimmed[5..].Trim();
                    if (int.TryParse(lineStr, out int lineNum))
                    {
                        item.LineNumber = lineNum;
                    }
                }
                else if (trimmed.StartsWith("DESCRIPTION:", StringComparison.OrdinalIgnoreCase))
                {
                    item.Description = trimmed[12..].Trim();
                }
                else if (trimmed.StartsWith("SUGGESTION:", StringComparison.OrdinalIgnoreCase))
                {
                    item.Suggestion = trimmed[11..].Trim();
                }
            }

            return string.IsNullOrEmpty(item.Description) ? null : item;
        }

        private static DebugHelpResult ParseDebugHelpResponse(string response, string errorMessage)
        {
            var result = new DebugHelpResult
            {
                ErrorMessage = errorMessage,
                IsSuccess = true
            };

            var lines = response.Split('\n');
            bool inFixedCode = false;
            var fixedCodeBuilder = new StringBuilder();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("EXPLANATION:", StringComparison.OrdinalIgnoreCase))
                {
                    result.Explanation = trimmed[12..].Trim();
                    inFixedCode = false;
                }
                else if (trimmed.StartsWith("CAUSE:", StringComparison.OrdinalIgnoreCase))
                {
                    result.PossibleCause = trimmed[6..].Trim();
                    inFixedCode = false;
                }
                else if (trimmed.StartsWith("FIX:", StringComparison.OrdinalIgnoreCase))
                {
                    result.SuggestedFix = trimmed[4..].Trim();
                    inFixedCode = false;
                }
                else if (trimmed.StartsWith("FIXED_CODE:", StringComparison.OrdinalIgnoreCase))
                {
                    inFixedCode = true;
                }
                else if (inFixedCode)
                {
                    if (!trimmed.Equals("N/A", StringComparison.OrdinalIgnoreCase))
                    {
                        fixedCodeBuilder.AppendLine(line);
                    }
                }
            }

            var fixedCode = fixedCodeBuilder.ToString().Trim();
            if (!string.IsNullOrEmpty(fixedCode) && !fixedCode.Equals("N/A", StringComparison.OrdinalIgnoreCase))
            {
                // Remove markdown code block markers if present
                fixedCode = Regex.Replace(fixedCode, @"^```\w*\s*", "");
                fixedCode = Regex.Replace(fixedCode, @"\s*```$", "");
                result.FixedCode = fixedCode.Trim();
            }

            return result;
        }

        private static ConceptExplanationResult ParseConceptExplanationResponse(string response, string topic)
        {
            var result = new ConceptExplanationResult
            {
                Topic = topic,
                IsSuccess = true
            };

            var lines = response.Split('\n');
            bool inCodeExample = false;
            bool inKeyPoints = false;
            var codeBuilder = new StringBuilder();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("EXPLANATION:", StringComparison.OrdinalIgnoreCase))
                {
                    result.Explanation = trimmed[12..].Trim();
                    inCodeExample = false;
                    inKeyPoints = false;
                }
                else if (trimmed.StartsWith("CODE_EXAMPLE:", StringComparison.OrdinalIgnoreCase))
                {
                    inCodeExample = true;
                    inKeyPoints = false;
                }
                else if (trimmed.StartsWith("KEY_POINTS:", StringComparison.OrdinalIgnoreCase))
                {
                    inCodeExample = false;
                    inKeyPoints = true;
                }
                else if (trimmed.StartsWith("RELATED:", StringComparison.OrdinalIgnoreCase))
                {
                    inCodeExample = false;
                    inKeyPoints = false;
                    var related = trimmed[8..].Trim();
                    result.RelatedTopics = new List<string>(related.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
                }
                else if (inCodeExample)
                {
                    codeBuilder.AppendLine(line);
                }
                else if (inKeyPoints && trimmed.StartsWith("-"))
                {
                    result.KeyPoints.Add(trimmed[1..].Trim());
                }
            }

            var codeExample = codeBuilder.ToString().Trim();
            if (!string.IsNullOrEmpty(codeExample))
            {
                // Remove markdown code block markers if present
                codeExample = Regex.Replace(codeExample, @"^```\w*\s*", "");
                codeExample = Regex.Replace(codeExample, @"\s*```$", "");
                result.CodeExample = codeExample.Trim();
            }

            return result;
        }

        private static CodeImprovementResult ParseCodeImprovementResponse(string response, string originalCode)
        {
            var result = new CodeImprovementResult
            {
                OriginalCode = originalCode,
                IsSuccess = true
            };

            var lines = response.Split('\n');
            bool inImprovedCode = false;
            bool inImprovements = false;
            var improvedCodeBuilder = new StringBuilder();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("IMPROVED_CODE:", StringComparison.OrdinalIgnoreCase))
                {
                    inImprovedCode = true;
                    inImprovements = false;
                }
                else if (trimmed.StartsWith("IMPROVEMENTS:", StringComparison.OrdinalIgnoreCase))
                {
                    inImprovedCode = false;
                    inImprovements = true;
                }
                else if (inImprovedCode)
                {
                    improvedCodeBuilder.AppendLine(line);
                }
                else if (inImprovements && trimmed.StartsWith("-"))
                {
                    var item = ParseImprovementItem(trimmed[1..].Trim());
                    if (item != null)
                    {
                        result.Improvements.Add(item);
                    }
                }
            }

            var improvedCode = improvedCodeBuilder.ToString().Trim();
            if (!string.IsNullOrEmpty(improvedCode))
            {
                // Remove markdown code block markers if present
                improvedCode = Regex.Replace(improvedCode, @"^```\w*\s*", "");
                improvedCode = Regex.Replace(improvedCode, @"\s*```$", "");
                result.ImprovedCode = improvedCode.Trim();
            }

            return result;
        }

        private static ImprovementItem? ParseImprovementItem(string line)
        {
            var parts = line.Split('|');
            if (parts.Length < 2) return null;

            var item = new ImprovementItem();

            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.StartsWith("CATEGORY:", StringComparison.OrdinalIgnoreCase))
                {
                    item.Category = trimmed[9..].Trim();
                }
                else if (trimmed.StartsWith("BEFORE:", StringComparison.OrdinalIgnoreCase))
                {
                    item.Before = trimmed[7..].Trim();
                }
                else if (trimmed.StartsWith("AFTER:", StringComparison.OrdinalIgnoreCase))
                {
                    item.After = trimmed[6..].Trim();
                }
                else if (trimmed.StartsWith("EXPLANATION:", StringComparison.OrdinalIgnoreCase))
                {
                    item.Explanation = trimmed[12..].Trim();
                }
            }

            return string.IsNullOrEmpty(item.Explanation) ? null : item;
        }

        private static RandomWordResult ParseRandomWordResponse(string response)
        {
            var result = new RandomWordResult
            {
                IsSuccess = true
            };

            var lines = response.Split('\n');
            bool inExamples = false;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("WORD:", StringComparison.OrdinalIgnoreCase))
                {
                    result.Word = trimmed[5..].Trim();
                    inExamples = false;
                }
                else if (trimmed.StartsWith("PRONUNCIATION:", StringComparison.OrdinalIgnoreCase))
                {
                    result.Pronunciation = trimmed[14..].Trim();
                    inExamples = false;
                }
                else if (trimmed.StartsWith("DEFINITION:", StringComparison.OrdinalIgnoreCase))
                {
                    result.Definition = trimmed[11..].Trim();
                    inExamples = false;
                }
                else if (trimmed.StartsWith("CATEGORY:", StringComparison.OrdinalIgnoreCase))
                {
                    result.Category = trimmed[9..].Trim();
                    inExamples = false;
                }
                else if (trimmed.StartsWith("EXAMPLES:", StringComparison.OrdinalIgnoreCase))
                {
                    inExamples = true;
                }
                else if (trimmed.StartsWith("USAGE_NOTE:", StringComparison.OrdinalIgnoreCase))
                {
                    result.UsageNote = trimmed[11..].Trim();
                    inExamples = false;
                }
                else if (trimmed.StartsWith("RELATED:", StringComparison.OrdinalIgnoreCase))
                {
                    inExamples = false;
                    var related = trimmed[8..].Trim();
                    result.RelatedWords = new List<string>(related.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
                }
                else if (inExamples && trimmed.StartsWith("-"))
                {
                    result.ExampleSentences.Add(trimmed[1..].Trim());
                }
            }

            return result;
        }

        private static RandomApiResult ParseRandomApiResponse(string response)
        {
            var result = new RandomApiResult
            {
                IsSuccess = true
            };

            var lines = response.Split('\n');
            bool inParameters = false;
            bool inCodeExample = false;
            bool inUseCases = false;
            var codeBuilder = new StringBuilder();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("API_NAME:", StringComparison.OrdinalIgnoreCase))
                {
                    result.ApiName = trimmed[9..].Trim();
                    inParameters = false;
                    inCodeExample = false;
                    inUseCases = false;
                }
                else if (trimmed.StartsWith("LIBRARY:", StringComparison.OrdinalIgnoreCase))
                {
                    result.Library = trimmed[8..].Trim();
                    inParameters = false;
                    inCodeExample = false;
                    inUseCases = false;
                }
                else if (trimmed.StartsWith("DESCRIPTION:", StringComparison.OrdinalIgnoreCase))
                {
                    result.Description = trimmed[12..].Trim();
                    inParameters = false;
                    inCodeExample = false;
                    inUseCases = false;
                }
                else if (trimmed.StartsWith("SYNTAX:", StringComparison.OrdinalIgnoreCase))
                {
                    result.Syntax = trimmed[7..].Trim();
                    inParameters = false;
                    inCodeExample = false;
                    inUseCases = false;
                }
                else if (trimmed.StartsWith("PARAMETERS:", StringComparison.OrdinalIgnoreCase))
                {
                    inParameters = true;
                    inCodeExample = false;
                    inUseCases = false;
                }
                else if (trimmed.StartsWith("RETURN:", StringComparison.OrdinalIgnoreCase))
                {
                    result.ReturnValue = trimmed[7..].Trim();
                    inParameters = false;
                    inCodeExample = false;
                    inUseCases = false;
                }
                else if (trimmed.StartsWith("CODE_EXAMPLE:", StringComparison.OrdinalIgnoreCase))
                {
                    inParameters = false;
                    inCodeExample = true;
                    inUseCases = false;
                }
                else if (trimmed.StartsWith("USE_CASES:", StringComparison.OrdinalIgnoreCase))
                {
                    inParameters = false;
                    inCodeExample = false;
                    inUseCases = true;
                }
                else if (trimmed.StartsWith("TIP:", StringComparison.OrdinalIgnoreCase))
                {
                    result.Tip = trimmed[4..].Trim();
                    inParameters = false;
                    inCodeExample = false;
                    inUseCases = false;
                }
                else if (inParameters && trimmed.StartsWith("-"))
                {
                    result.Parameters.Add(trimmed[1..].Trim());
                }
                else if (inCodeExample)
                {
                    codeBuilder.AppendLine(line);
                }
                else if (inUseCases && trimmed.StartsWith("-"))
                {
                    result.UseCases.Add(trimmed[1..].Trim());
                }
            }

            var codeExample = codeBuilder.ToString().Trim();
            if (!string.IsNullOrEmpty(codeExample))
            {
                codeExample = Regex.Replace(codeExample, @"^```\w*\s*", "");
                codeExample = Regex.Replace(codeExample, @"\s*```$", "");
                result.CodeExample = codeExample.Trim();
            }

            return result;
        }

        private static RandomConceptResult ParseRandomConceptResponse(string response)
        {
            var result = new RandomConceptResult
            {
                IsSuccess = true
            };

            var lines = response.Split('\n');
            bool inKeyPoints = false;
            bool inCodeExample = false;
            var codeBuilder = new StringBuilder();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("CONCEPT:", StringComparison.OrdinalIgnoreCase))
                {
                    result.ConceptName = trimmed[8..].Trim();
                    inKeyPoints = false;
                    inCodeExample = false;
                }
                else if (trimmed.StartsWith("CATEGORY:", StringComparison.OrdinalIgnoreCase))
                {
                    result.Category = trimmed[9..].Trim();
                    inKeyPoints = false;
                    inCodeExample = false;
                }
                else if (trimmed.StartsWith("EXPLANATION:", StringComparison.OrdinalIgnoreCase))
                {
                    result.Explanation = trimmed[12..].Trim();
                    inKeyPoints = false;
                    inCodeExample = false;
                }
                else if (trimmed.StartsWith("KEY_POINTS:", StringComparison.OrdinalIgnoreCase))
                {
                    inKeyPoints = true;
                    inCodeExample = false;
                }
                else if (trimmed.StartsWith("CODE_EXAMPLE:", StringComparison.OrdinalIgnoreCase))
                {
                    inKeyPoints = false;
                    inCodeExample = true;
                }
                else if (trimmed.StartsWith("WHEN_TO_USE:", StringComparison.OrdinalIgnoreCase))
                {
                    result.WhenToUse = trimmed[12..].Trim();
                    inKeyPoints = false;
                    inCodeExample = false;
                }
                else if (trimmed.StartsWith("RELATED:", StringComparison.OrdinalIgnoreCase))
                {
                    inKeyPoints = false;
                    inCodeExample = false;
                    var related = trimmed[8..].Trim();
                    result.RelatedConcepts = new List<string>(related.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
                }
                else if (inKeyPoints && trimmed.StartsWith("-"))
                {
                    result.KeyPoints.Add(trimmed[1..].Trim());
                }
                else if (inCodeExample)
                {
                    codeBuilder.AppendLine(line);
                }
            }

            var codeExample = codeBuilder.ToString().Trim();
            if (!string.IsNullOrEmpty(codeExample))
            {
                codeExample = Regex.Replace(codeExample, @"^```\w*\s*", "");
                codeExample = Regex.Replace(codeExample, @"\s*```$", "");
                result.CodeExample = codeExample.Trim();
            }

            return result;
        }

        #endregion
    }
}
