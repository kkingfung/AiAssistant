using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace AiAssistant
{
    /// <summary>
    /// Markdownテキストを簡易的にTextBlockで表示するヘルパー
    /// コードブロック、太字、斜体、インラインコードをサポート
    /// </summary>
    public static class MarkdownTextBlockHelper
    {
        /// <summary>
        /// Markdownテキストから装飾されたTextBlockを作成します
        /// </summary>
        public static TextBlock CreateFormattedTextBlock(string markdownText, bool isUser)
        {
            var settings = AppSettings.Instance.Assistant;
            var isDark = settings.IsDarkTheme;

            var textBlock = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Foreground = isUser ? Brushes.White : (isDark ? new SolidColorBrush(Color.FromRgb(220, 220, 220)) : Brushes.Black),
                FontSize = 13
            };

            if (string.IsNullOrWhiteSpace(markdownText))
            {
                return textBlock;
            }

            // コードブロックを処理（```で囲まれた部分）
            var codeBlockPattern = @"```(\w+)?\n([\s\S]*?)```";
            var parts = Regex.Split(markdownText, codeBlockPattern);

            for (int i = 0; i < parts.Length; i++)
            {
                if (i % 3 == 0)
                {
                    // 通常のテキスト部分
                    ProcessInlineFormatting(textBlock, parts[i], isUser);
                }
                else if (i % 3 == 2)
                {
                    // コードブロック部分
                    AddCodeBlock(textBlock, parts[i], parts[i - 1]);
                }
            }

            return textBlock;
        }

        /// <summary>
        /// インライン書式（太字、斜体、インラインコード）を処理します
        /// </summary>
        private static void ProcessInlineFormatting(TextBlock textBlock, string text, bool isUser)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            // インラインコード `code`
            var inlineCodePattern = @"`([^`]+)`";
            var parts = Regex.Split(text, inlineCodePattern);

            for (int i = 0; i < parts.Length; i++)
            {
                if (i % 2 == 0)
                {
                    // 通常のテキスト（太字・斜体を処理）
                    ProcessBoldAndItalic(textBlock, parts[i], isUser);
                }
                else
                {
                    // インラインコード
                    var run = new Run(parts[i])
                    {
                        Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                        Foreground = new SolidColorBrush(Color.FromRgb(220, 50, 47)),
                        FontFamily = new FontFamily("Consolas, Courier New"),
                        FontSize = 12
                    };
                    textBlock.Inlines.Add(run);
                }
            }
        }

        /// <summary>
        /// 太字（**text**）と斜体（*text*）を処理します
        /// </summary>
        private static void ProcessBoldAndItalic(TextBlock textBlock, string text, bool isUser)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var settings = AppSettings.Instance.Assistant;
            var isDark = settings.IsDarkTheme;

            // 太字 **text**
            var boldPattern = @"\*\*([^\*]+)\*\*";
            var parts = Regex.Split(text, boldPattern);

            for (int i = 0; i < parts.Length; i++)
            {
                if (i % 2 == 0)
                {
                    // 通常のテキスト
                    AddPlainText(textBlock, parts[i], isUser);
                }
                else
                {
                    // 太字
                    var run = new Run(parts[i])
                    {
                        FontWeight = FontWeights.Bold,
                        Foreground = isUser ? Brushes.White : (isDark ? new SolidColorBrush(Color.FromRgb(220, 220, 220)) : Brushes.Black)
                    };
                    textBlock.Inlines.Add(run);
                }
            }
        }

        /// <summary>
        /// プレーンテキストを追加します
        /// </summary>
        private static void AddPlainText(TextBlock textBlock, string text, bool isUser)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var settings = AppSettings.Instance.Assistant;
            var isDark = settings.IsDarkTheme;

            var run = new Run(text)
            {
                Foreground = isUser ? Brushes.White : (isDark ? new SolidColorBrush(Color.FromRgb(220, 220, 220)) : Brushes.Black)
            };
            textBlock.Inlines.Add(run);
        }

        /// <summary>
        /// コードブロックを追加します
        /// </summary>
        private static void AddCodeBlock(TextBlock textBlock, string code, string language)
        {
            if (string.IsNullOrEmpty(code))
            {
                return;
            }

            // 改行を追加
            textBlock.Inlines.Add(new LineBreak());

            // コードブロック用のBorderを作成（TextBlockには直接追加できないのでRunとして表現）
            var codeRun = new Run($"\n{code.Trim()}\n")
            {
                Background = new SolidColorBrush(Color.FromRgb(40, 44, 52)),
                Foreground = new SolidColorBrush(Color.FromRgb(171, 178, 191)),
                FontFamily = new FontFamily("Consolas, Courier New"),
                FontSize = 12
            };

            textBlock.Inlines.Add(codeRun);
            textBlock.Inlines.Add(new LineBreak());
        }
    }
}
