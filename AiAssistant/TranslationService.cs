using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AiAssistant
{
    /// <summary>
    /// AIサービスを使用した翻訳サービス実装
    /// ローカルLLMまたはChatGPTを使用してテキストを翻訳します
    /// </summary>
    public sealed class TranslationService : ITranslationService
    {
        private readonly IAiService _aiService;

        /// <summary>
        /// TranslationServiceのコンストラクタ
        /// </summary>
        /// <param name="aiService">使用するAIサービス</param>
        public TranslationService(IAiService aiService)
        {
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
        }

        /// <summary>
        /// テキストを指定した言語に翻訳します
        /// </summary>
        public async Task<TranslationResult> TranslateAsync(string text, string targetLanguage, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new TranslationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "翻訳するテキストが空です"
                };
            }

            try
            {
                var prompt = BuildTranslationPrompt(text, targetLanguage);
                var response = await _aiService.GetResponseAsync(prompt, cancellationToken);

                // レスポンスから翻訳結果を抽出
                var translatedText = ExtractTranslation(response);

                return new TranslationResult
                {
                    OriginalText = text,
                    TranslatedText = translatedText,
                    TargetLanguage = targetLanguage,
                    IsSuccess = true
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"翻訳エラー: {ex.Message}");
                return new TranslationResult
                {
                    OriginalText = text,
                    IsSuccess = false,
                    ErrorMessage = $"翻訳に失敗しました: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// テキストの言語を検出し、適切な言語に自動翻訳します
        /// </summary>
        public async Task<TranslationResult> AutoTranslateAsync(string text, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new TranslationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "翻訳するテキストが空です"
                };
            }

            try
            {
                // 言語検出と翻訳を一度に行うプロンプト
                var prompt = BuildAutoTranslationPrompt(text);
                var response = await _aiService.GetResponseAsync(prompt, cancellationToken);

                // レスポンスを解析
                var result = ParseAutoTranslationResponse(response, text);
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"自動翻訳エラー: {ex.Message}");
                return new TranslationResult
                {
                    OriginalText = text,
                    IsSuccess = false,
                    ErrorMessage = $"翻訳に失敗しました: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// テキストの言語を検出します
        /// </summary>
        public async Task<string> DetectLanguageAsync(string text, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "Unknown";
            }

            try
            {
                var prompt = $@"Detect the language of the following text and respond with ONLY the language name (e.g., Japanese, English, Korean, Chinese, etc.):

""{text}""

Language:";

                var response = await _aiService.GetResponseAsync(prompt, cancellationToken);
                return response.Trim();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"言語検出エラー: {ex.Message}");
                return "Unknown";
            }
        }

        /// <summary>
        /// 翻訳用プロンプトを構築します
        /// </summary>
        private static string BuildTranslationPrompt(string text, string targetLanguage)
        {
            return $@"Translate the following text to {targetLanguage}.
Respond with ONLY the translated text, without any explanations or additional text.

Text to translate:
""{text}""

Translation:";
        }

        /// <summary>
        /// 自動翻訳用プロンプトを構築します
        /// 日本語は英語に、それ以外は日本語に翻訳します
        /// </summary>
        private static string BuildAutoTranslationPrompt(string text)
        {
            return $@"Analyze the following text and translate it:
- If the text is primarily in Japanese, translate it to English
- If the text is primarily in English, translate it to Japanese
- If the text is in Korean, translate it to Japanese
- If the text is in Chinese, translate it to Japanese
- For other languages, translate to Japanese

Respond in this exact format:
DETECTED: [detected language]
TARGET: [target language]
TRANSLATION: [translated text]

Text:
""{text}""";
        }

        /// <summary>
        /// 翻訳結果を抽出します
        /// </summary>
        private static string ExtractTranslation(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
            {
                return string.Empty;
            }

            // "Translation:" の後のテキストを抽出
            var translationIndex = response.IndexOf("Translation:", StringComparison.OrdinalIgnoreCase);
            if (translationIndex >= 0)
            {
                return response[(translationIndex + 12)..].Trim().Trim('"');
            }

            // そのまま返す（プレフィックスがない場合）
            return response.Trim().Trim('"');
        }

        /// <summary>
        /// 自動翻訳レスポンスを解析します
        /// </summary>
        private static TranslationResult ParseAutoTranslationResponse(string response, string originalText)
        {
            var result = new TranslationResult
            {
                OriginalText = originalText,
                IsSuccess = true
            };

            if (string.IsNullOrWhiteSpace(response))
            {
                result.IsSuccess = false;
                result.ErrorMessage = "AIからの応答がありません";
                return result;
            }

            var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("DETECTED:", StringComparison.OrdinalIgnoreCase))
                {
                    result.DetectedLanguage = trimmedLine[9..].Trim();
                }
                else if (trimmedLine.StartsWith("TARGET:", StringComparison.OrdinalIgnoreCase))
                {
                    result.TargetLanguage = trimmedLine[7..].Trim();
                }
                else if (trimmedLine.StartsWith("TRANSLATION:", StringComparison.OrdinalIgnoreCase))
                {
                    result.TranslatedText = trimmedLine[12..].Trim().Trim('"');
                }
            }

            // TRANSLATION: が見つからない場合、最後の行を翻訳結果として使用
            if (string.IsNullOrEmpty(result.TranslatedText) && lines.Length > 0)
            {
                // フォーマットに従わない応答の場合、全体を翻訳結果とみなす
                var sb = new StringBuilder();
                bool foundTranslation = false;

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (trimmedLine.StartsWith("DETECTED:", StringComparison.OrdinalIgnoreCase) ||
                        trimmedLine.StartsWith("TARGET:", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (trimmedLine.StartsWith("TRANSLATION:", StringComparison.OrdinalIgnoreCase))
                    {
                        foundTranslation = true;
                        var translationPart = trimmedLine[12..].Trim();
                        if (!string.IsNullOrEmpty(translationPart))
                        {
                            sb.AppendLine(translationPart);
                        }
                        continue;
                    }

                    if (foundTranslation || !trimmedLine.Contains(':'))
                    {
                        sb.AppendLine(trimmedLine);
                    }
                }

                result.TranslatedText = sb.ToString().Trim().Trim('"');
            }

            // 翻訳結果がまだ空の場合
            if (string.IsNullOrEmpty(result.TranslatedText))
            {
                result.TranslatedText = response.Trim();
            }

            return result;
        }
    }
}
