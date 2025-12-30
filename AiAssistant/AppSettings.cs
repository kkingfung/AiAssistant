using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiAssistant
{
    /// <summary>
    /// アプリケーション設定を管理するクラス
    /// appsettings.jsonから設定を読み込み、APIキーや各種設定を提供します
    /// </summary>
    public sealed class AppSettings
    {
        private static AppSettings? _instance;
        private static readonly object _lock = new();

        [JsonPropertyName("OpenAI")]
        public OpenAISettings OpenAI { get; set; } = new();

        [JsonPropertyName("Google")]
        public GoogleSettings Google { get; set; } = new();

        [JsonPropertyName("LocalLlm")]
        public LocalLlmSettings LocalLlm { get; set; } = new();

        [JsonPropertyName("Assistant")]
        public AssistantSettings Assistant { get; set; } = new();

        [JsonPropertyName("Weather")]
        public WeatherSettings Weather { get; set; } = new();

        [JsonPropertyName("Fund")]
        public FundSettings Fund { get; set; } = new();

        [JsonPropertyName("Anthropic")]
        public AnthropicSettings Anthropic { get; set; } = new();

        /// <summary>
        /// シングルトンインスタンスを取得
        /// </summary>
        public static AppSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= Load();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// appsettings.jsonから設定を読み込みます
        /// </summary>
        private static AppSettings Load()
        {
            var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

            if (!File.Exists(settingsPath))
            {
                // 設定ファイルが存在しない場合はデフォルト値を返す
                return new AppSettings();
            }

            try
            {
                var json = File.ReadAllText(settingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                });

                return settings ?? new AppSettings();
            }
            catch (Exception ex)
            {
                // 読み込みエラーの場合はデフォルト値を返す
                System.Diagnostics.Debug.WriteLine($"設定ファイルの読み込みに失敗しました: {ex.Message}");
                return new AppSettings();
            }
        }

        /// <summary>
        /// 設定を保存します
        /// </summary>
        public void Save()
        {
            var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

            try
            {
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = null
                });

                File.WriteAllText(settingsPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"設定ファイルの保存に失敗しました: {ex.Message}");
            }
        }

        /// <summary>
        /// 設定をリロードします
        /// </summary>
        public static void Reload()
        {
            lock (_lock)
            {
                _instance = Load();
            }
        }
    }

    /// <summary>
    /// OpenAI API設定
    /// </summary>
    public sealed class OpenAISettings
    {
        [JsonPropertyName("ApiKey")]
        public string ApiKey { get; set; } = string.Empty;

        [JsonPropertyName("Model")]
        public string Model { get; set; } = "gpt-4";

        [JsonPropertyName("MaxTokens")]
        public int MaxTokens { get; set; } = 2000;

        [JsonPropertyName("Temperature")]
        public double Temperature { get; set; } = 0.7;

        /// <summary>
        /// APIキーが設定されているかどうか
        /// </summary>
        public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey) && ApiKey != "YOUR_OPENAI_API_KEY_HERE";
    }

    /// <summary>
    /// Google API設定
    /// </summary>
    public sealed class GoogleSettings
    {
        [JsonPropertyName("ApiKey")]
        public string ApiKey { get; set; } = string.Empty;

        [JsonPropertyName("ClientId")]
        public string ClientId { get; set; } = string.Empty;

        [JsonPropertyName("ClientSecret")]
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// APIキーが設定されているかどうか
        /// </summary>
        public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey) && ApiKey != "YOUR_GOOGLE_API_KEY_HERE";
    }

    /// <summary>
    /// ペットの種類
    /// </summary>
    public enum PetType
    {
        Cat,      // HellCat
        Crab,     // SciFi Crab
        Dragon,   // SciFi Dragon
        Frog,     // SciFi Frog
        Shark,    // SciFi Shark
        Snake,    // SciFi Snake
        Random    // ランダムに選択
    }

    /// <summary>
    /// アシスタント設定
    /// </summary>
    public sealed class AssistantSettings
    {
        [JsonPropertyName("CharacterModelPath")]
        public string CharacterModelPath { get; set; } = string.Empty;

        [JsonPropertyName("AvatarImagePath")]
        public string AvatarImagePath { get; set; } = string.Empty;

        [JsonPropertyName("CharacterAnimationsFolder")]
        public string CharacterAnimationsFolder { get; set; } = "CharacterAnimations";

        [JsonPropertyName("SelectedPet")]
        public string SelectedPet { get; set; } = "Dragon"; // Cat, Crab, Dragon, Frog, Shark, Snake, Random

        [JsonPropertyName("AnimationSwitchIntervalSeconds")]
        public int AnimationSwitchIntervalSeconds { get; set; } = 15;

        [JsonPropertyName("Theme")]
        public string Theme { get; set; } = "Light"; // "Light" or "Dark"

        [JsonPropertyName("WindowWidth")]
        public double WindowWidth { get; set; } = 280;

        [JsonPropertyName("WindowHeight")]
        public double WindowHeight { get; set; } = 400;

        [JsonPropertyName("AspectRatio")]
        public double AspectRatio { get; set; } = 0.7;

        [JsonPropertyName("SaveWindowPosition")]
        public bool SaveWindowPosition { get; set; } = true;

        [JsonPropertyName("LastPositionX")]
        public double LastPositionX { get; set; } = 100;

        [JsonPropertyName("LastPositionY")]
        public double LastPositionY { get; set; } = 100;

        /// <summary>
        /// アバター画像が設定されているかどうか
        /// </summary>
        public bool HasAvatar => !string.IsNullOrWhiteSpace(AvatarImagePath) && File.Exists(AvatarImagePath);

        /// <summary>
        /// ダークテーマかどうか
        /// </summary>
        public bool IsDarkTheme => Theme.Equals("Dark", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// ローカルLLM設定
    /// </summary>
    public sealed class LocalLlmSettings
    {
        [JsonPropertyName("Enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("Provider")]
        public string Provider { get; set; } = "Ollama"; // "Ollama", "LLamaSharp", "ONNX"

        [JsonPropertyName("Endpoint")]
        public string Endpoint { get; set; } = "http://localhost:11434";

        [JsonPropertyName("Model")]
        public string Model { get; set; } = "phi3:mini";

        [JsonPropertyName("ModelPath")]
        public string ModelPath { get; set; } = string.Empty;

        [JsonPropertyName("MaxTokens")]
        public int MaxTokens { get; set; } = 500;

        [JsonPropertyName("PreferLocal")]
        public bool PreferLocal { get; set; } = true;

        /// <summary>
        /// ローカルLLMが有効で、ローカル優先設定かどうか
        /// </summary>
        public bool ShouldUseLocal => Enabled && PreferLocal;
    }

    /// <summary>
    /// 天気サービス設定
    /// </summary>
    public sealed class WeatherSettings
    {
        [JsonPropertyName("City")]
        public string City { get; set; } = "東京";

        [JsonPropertyName("Latitude")]
        public double Latitude { get; set; } = 35.6762;

        [JsonPropertyName("Longitude")]
        public double Longitude { get; set; } = 139.6503;
    }

    /// <summary>
    /// ファンド監視設定
    /// </summary>
    public sealed class FundSettings
    {
        [JsonPropertyName("FundUrls")]
        public List<string> FundUrls { get; set; } = new()
        {
            "https://fs.bk.mufg.jp/webasp/mufg/fund/detail/m02823520.html",
            "https://fs.bk.mufg.jp/webasp/mufg/fund/detail/m10322220.html"
        };
    }

    /// <summary>
    /// Anthropic API設定（組織アカウント用）
    /// </summary>
    public sealed class AnthropicSettings
    {
        [JsonPropertyName("AdminApiKey")]
        public string AdminApiKey { get; set; } = string.Empty;

        [JsonPropertyName("OrganizationId")]
        public string OrganizationId { get; set; } = string.Empty;

        /// <summary>
        /// Admin APIが設定されているかどうか
        /// </summary>
        public bool IsConfigured => !string.IsNullOrWhiteSpace(AdminApiKey) && !string.IsNullOrWhiteSpace(OrganizationId);
    }
}
