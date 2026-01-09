using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace AiAssistant
{
    /// <summary>
    /// チャットバブルスタイルのプリセット
    /// </summary>
    public enum ChatBubblePreset
    {
        Modern,         // モダン（デフォルト）
        Minimal,        // ミニマル
        Glassmorphism,  // グラスモーフィズム
        Retro,          // レトロ
        PixelArt,       // ピクセルアート風
        Neon,           // ネオン
        Pastel,         // パステル
        Dark,           // ダーク
        Custom          // カスタム
    }

    /// <summary>
    /// チャットバブルのスタイル設定
    /// </summary>
    public sealed class ChatBubbleStyle
    {
        /// <summary>
        /// スタイル名
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = "Modern";

        /// <summary>
        /// ユーザーメッセージの背景色（ARGB hex）
        /// </summary>
        [JsonPropertyName("userBubbleColor")]
        public string UserBubbleColor { get; set; } = "#FF1E90FF";

        /// <summary>
        /// AIメッセージの背景色（ライトテーマ）
        /// </summary>
        [JsonPropertyName("aiBubbleColorLight")]
        public string AiBubbleColorLight { get; set; } = "#FFF0F0F0";

        /// <summary>
        /// AIメッセージの背景色（ダークテーマ）
        /// </summary>
        [JsonPropertyName("aiBubbleColorDark")]
        public string AiBubbleColorDark { get; set; } = "#FF323232";

        /// <summary>
        /// ユーザーメッセージのテキスト色
        /// </summary>
        [JsonPropertyName("userTextColor")]
        public string UserTextColor { get; set; } = "#FFFFFFFF";

        /// <summary>
        /// AIメッセージのテキスト色（ライトテーマ）
        /// </summary>
        [JsonPropertyName("aiTextColorLight")]
        public string AiTextColorLight { get; set; } = "#FF000000";

        /// <summary>
        /// AIメッセージのテキスト色（ダークテーマ）
        /// </summary>
        [JsonPropertyName("aiTextColorDark")]
        public string AiTextColorDark { get; set; } = "#FFFFFFFF";

        /// <summary>
        /// 角丸の半径
        /// </summary>
        [JsonPropertyName("cornerRadius")]
        public double CornerRadius { get; set; } = 8.0;

        /// <summary>
        /// フォントサイズ
        /// </summary>
        [JsonPropertyName("fontSize")]
        public double FontSize { get; set; } = 13.0;

        /// <summary>
        /// フォントファミリー
        /// </summary>
        [JsonPropertyName("fontFamily")]
        public string FontFamily { get; set; } = "Segoe UI";

        /// <summary>
        /// 境界線の太さ
        /// </summary>
        [JsonPropertyName("borderThickness")]
        public double BorderThickness { get; set; } = 0.0;

        /// <summary>
        /// 境界線の色
        /// </summary>
        [JsonPropertyName("borderColor")]
        public string BorderColor { get; set; } = "#00000000";

        /// <summary>
        /// シャドウを有効にするか
        /// </summary>
        [JsonPropertyName("enableShadow")]
        public bool EnableShadow { get; set; } = false;

        /// <summary>
        /// シャドウの色
        /// </summary>
        [JsonPropertyName("shadowColor")]
        public string ShadowColor { get; set; } = "#40000000";

        /// <summary>
        /// シャドウのぼかし半径
        /// </summary>
        [JsonPropertyName("shadowBlurRadius")]
        public double ShadowBlurRadius { get; set; } = 4.0;

        /// <summary>
        /// パディング
        /// </summary>
        [JsonPropertyName("padding")]
        public double Padding { get; set; } = 10.0;

        /// <summary>
        /// マージン
        /// </summary>
        [JsonPropertyName("margin")]
        public double Margin { get; set; } = 8.0;

        /// <summary>
        /// 透明度（0.0〜1.0）
        /// </summary>
        [JsonPropertyName("opacity")]
        public double Opacity { get; set; } = 1.0;

        /// <summary>
        /// 色文字列をColorに変換します
        /// </summary>
        public static Color ParseColor(string colorString)
        {
            try
            {
                if (colorString.StartsWith("#"))
                {
                    return (Color)ColorConverter.ConvertFromString(colorString);
                }
                return Colors.Gray;
            }
            catch
            {
                return Colors.Gray;
            }
        }

        /// <summary>
        /// 色をSolidColorBrushに変換します
        /// </summary>
        public static SolidColorBrush ToBrush(string colorString)
        {
            return new SolidColorBrush(ParseColor(colorString));
        }

        /// <summary>
        /// プリセットからスタイルを作成します
        /// </summary>
        public static ChatBubbleStyle FromPreset(ChatBubblePreset preset)
        {
            return preset switch
            {
                ChatBubblePreset.Modern => CreateModernStyle(),
                ChatBubblePreset.Minimal => CreateMinimalStyle(),
                ChatBubblePreset.Glassmorphism => CreateGlassmorphismStyle(),
                ChatBubblePreset.Retro => CreateRetroStyle(),
                ChatBubblePreset.PixelArt => CreatePixelArtStyle(),
                ChatBubblePreset.Neon => CreateNeonStyle(),
                ChatBubblePreset.Pastel => CreatePastelStyle(),
                ChatBubblePreset.Dark => CreateDarkStyle(),
                _ => CreateModernStyle()
            };
        }

        /// <summary>
        /// 全プリセットの一覧を取得します
        /// </summary>
        public static Dictionary<ChatBubblePreset, ChatBubbleStyle> GetAllPresets()
        {
            return new Dictionary<ChatBubblePreset, ChatBubbleStyle>
            {
                { ChatBubblePreset.Modern, CreateModernStyle() },
                { ChatBubblePreset.Minimal, CreateMinimalStyle() },
                { ChatBubblePreset.Glassmorphism, CreateGlassmorphismStyle() },
                { ChatBubblePreset.Retro, CreateRetroStyle() },
                { ChatBubblePreset.PixelArt, CreatePixelArtStyle() },
                { ChatBubblePreset.Neon, CreateNeonStyle() },
                { ChatBubblePreset.Pastel, CreatePastelStyle() },
                { ChatBubblePreset.Dark, CreateDarkStyle() }
            };
        }

        #region プリセットスタイル定義

        private static ChatBubbleStyle CreateModernStyle()
        {
            return new ChatBubbleStyle
            {
                Name = "Modern",
                UserBubbleColor = "#FF1E90FF",
                AiBubbleColorLight = "#FFF0F0F0",
                AiBubbleColorDark = "#FF323232",
                UserTextColor = "#FFFFFFFF",
                AiTextColorLight = "#FF000000",
                AiTextColorDark = "#FFFFFFFF",
                CornerRadius = 8.0,
                FontSize = 13.0,
                FontFamily = "Segoe UI",
                BorderThickness = 0.0,
                EnableShadow = false,
                Padding = 10.0,
                Margin = 8.0,
                Opacity = 1.0
            };
        }

        private static ChatBubbleStyle CreateMinimalStyle()
        {
            return new ChatBubbleStyle
            {
                Name = "Minimal",
                UserBubbleColor = "#FF2C2C2C",
                AiBubbleColorLight = "#FFF8F8F8",
                AiBubbleColorDark = "#FF1A1A1A",
                UserTextColor = "#FFFFFFFF",
                AiTextColorLight = "#FF333333",
                AiTextColorDark = "#FFE0E0E0",
                CornerRadius = 4.0,
                FontSize = 13.0,
                FontFamily = "Segoe UI Light",
                BorderThickness = 1.0,
                BorderColor = "#20000000",
                EnableShadow = false,
                Padding = 12.0,
                Margin = 6.0,
                Opacity = 1.0
            };
        }

        private static ChatBubbleStyle CreateGlassmorphismStyle()
        {
            return new ChatBubbleStyle
            {
                Name = "Glassmorphism",
                UserBubbleColor = "#B01E90FF",
                AiBubbleColorLight = "#80FFFFFF",
                AiBubbleColorDark = "#80404040",
                UserTextColor = "#FFFFFFFF",
                AiTextColorLight = "#FF000000",
                AiTextColorDark = "#FFFFFFFF",
                CornerRadius = 16.0,
                FontSize = 13.0,
                FontFamily = "Segoe UI",
                BorderThickness = 1.0,
                BorderColor = "#40FFFFFF",
                EnableShadow = true,
                ShadowColor = "#20000000",
                ShadowBlurRadius = 10.0,
                Padding = 12.0,
                Margin = 8.0,
                Opacity = 0.95
            };
        }

        private static ChatBubbleStyle CreateRetroStyle()
        {
            return new ChatBubbleStyle
            {
                Name = "Retro",
                UserBubbleColor = "#FF008080",
                AiBubbleColorLight = "#FFFFFDD0",
                AiBubbleColorDark = "#FF2F4F4F",
                UserTextColor = "#FFFFFFFF",
                AiTextColorLight = "#FF000000",
                AiTextColorDark = "#FFFFFDD0",
                CornerRadius = 0.0,
                FontSize = 13.0,
                FontFamily = "Consolas",
                BorderThickness = 2.0,
                BorderColor = "#FF000000",
                EnableShadow = true,
                ShadowColor = "#FF000000",
                ShadowBlurRadius = 0.0,
                Padding = 8.0,
                Margin = 8.0,
                Opacity = 1.0
            };
        }

        private static ChatBubbleStyle CreatePixelArtStyle()
        {
            return new ChatBubbleStyle
            {
                Name = "Pixel Art",
                UserBubbleColor = "#FF5865F2",
                AiBubbleColorLight = "#FFEDD9B4",
                AiBubbleColorDark = "#FF3C3836",
                UserTextColor = "#FFFFFFFF",
                AiTextColorLight = "#FF3C3836",
                AiTextColorDark = "#FFEBDBB2",
                CornerRadius = 0.0,
                FontSize = 12.0,
                FontFamily = "Courier New",
                BorderThickness = 3.0,
                BorderColor = "#FF000000",
                EnableShadow = false,
                Padding = 8.0,
                Margin = 6.0,
                Opacity = 1.0
            };
        }

        private static ChatBubbleStyle CreateNeonStyle()
        {
            return new ChatBubbleStyle
            {
                Name = "Neon",
                UserBubbleColor = "#FF0D0D0D",
                AiBubbleColorLight = "#FF1A1A2E",
                AiBubbleColorDark = "#FF0D0D0D",
                UserTextColor = "#FF00FF88",
                AiTextColorLight = "#FFFF00FF",
                AiTextColorDark = "#FF00FFFF",
                CornerRadius = 8.0,
                FontSize = 13.0,
                FontFamily = "Segoe UI",
                BorderThickness = 2.0,
                BorderColor = "#FF00FF88",
                EnableShadow = true,
                ShadowColor = "#8000FF88",
                ShadowBlurRadius = 8.0,
                Padding = 10.0,
                Margin = 8.0,
                Opacity = 1.0
            };
        }

        private static ChatBubbleStyle CreatePastelStyle()
        {
            return new ChatBubbleStyle
            {
                Name = "Pastel",
                UserBubbleColor = "#FFFFB3BA",
                AiBubbleColorLight = "#FFBAFFC9",
                AiBubbleColorDark = "#FF7A9E7E",
                UserTextColor = "#FF5D4037",
                AiTextColorLight = "#FF2E7D32",
                AiTextColorDark = "#FFE8F5E9",
                CornerRadius = 20.0,
                FontSize = 13.0,
                FontFamily = "Segoe UI",
                BorderThickness = 0.0,
                EnableShadow = true,
                ShadowColor = "#30000000",
                ShadowBlurRadius = 6.0,
                Padding = 12.0,
                Margin = 8.0,
                Opacity = 1.0
            };
        }

        private static ChatBubbleStyle CreateDarkStyle()
        {
            return new ChatBubbleStyle
            {
                Name = "Dark",
                UserBubbleColor = "#FF7289DA",
                AiBubbleColorLight = "#FF2C2F33",
                AiBubbleColorDark = "#FF23272A",
                UserTextColor = "#FFFFFFFF",
                AiTextColorLight = "#FFFFFFFF",
                AiTextColorDark = "#FFB9BBBE",
                CornerRadius = 8.0,
                FontSize = 13.0,
                FontFamily = "Segoe UI",
                BorderThickness = 0.0,
                EnableShadow = false,
                Padding = 10.0,
                Margin = 8.0,
                Opacity = 1.0
            };
        }

        #endregion
    }
}
