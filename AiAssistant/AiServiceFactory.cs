using System;
using System.IO;
using System.Threading.Tasks;

namespace AiAssistant
{
    /// <summary>
    /// AIサービスのファクトリークラス
    /// 設定に基づいて適切なAIサービス実装を作成します
    /// </summary>
    public static class AiServiceFactory
    {
        /// <summary>
        /// 設定に基づいて最適なAIサービスを作成します
        /// 優先順位: ローカルLLM → クラウドOpenAI → Mock
        /// </summary>
        public static async Task<(IAiService service, string serviceType)> CreateAsync()
        {
            var settings = AppSettings.Instance;

            // ローカルLLMを優先する場合
            if (settings.LocalLlm.ShouldUseLocal)
            {
                var (localService, serviceType) = await TryCreateLocalServiceAsync();
                if (localService != null)
                {
                    return (localService, serviceType);
                }
            }

            // クラウドOpenAIを試す
            if (settings.OpenAI.IsConfigured)
            {
                try
                {
                    var chatGptService = new ChatGptService();
                    return (chatGptService, "ChatGPT (Cloud)");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ChatGPTサービス作成失敗: {ex.Message}");
                }
            }

            // Claude APIを試す
            if (settings.Claude.IsConfigured)
            {
                try
                {
                    var claudeService = new ClaudeAiService();
                    return (claudeService, $"Claude ({settings.Claude.Model})");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Claudeサービス作成失敗: {ex.Message}");
                }
            }

            // LM Studioを試す
            if (settings.LmStudio.ShouldUse)
            {
                var (lmStudioService, serviceType) = await TryCreateLmStudioServiceAsync();
                if (lmStudioService != null)
                {
                    return (lmStudioService, serviceType);
                }
            }

            // フォールバック: Mockサービス
            return (new MockAiService(), "Mock (Demo)");
        }

        /// <summary>
        /// ローカルLLMサービスの作成を試みます
        /// </summary>
        private static async Task<(IAiService? service, string serviceType)> TryCreateLocalServiceAsync()
        {
            var settings = AppSettings.Instance.LocalLlm;

            try
            {
                switch (settings.Provider.ToLower())
                {
                    case "ollama":
                        return await TryCreateOllamaServiceAsync(settings);

                    case "llamasharp":
                        return TryCreateLLamaSharpService(settings);

                    case "onnx":
                        return TryCreateOnnxService(settings);

                    default:
                        System.Diagnostics.Debug.WriteLine($"未知のプロバイダー: {settings.Provider}");
                        return (null, string.Empty);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ローカルLLMサービス作成エラー: {ex.Message}");
                return (null, string.Empty);
            }
        }

        /// <summary>
        /// Ollamaサービスの作成を試みます
        /// </summary>
        private static async Task<(IAiService? service, string serviceType)> TryCreateOllamaServiceAsync(LocalLlmSettings settings)
        {
            Console.WriteLine($"[Factory] Ollama初期化開始 - Endpoint: {settings.Endpoint}, Model: {settings.Model}");

            // Ollamaが実行中かチェック
            bool ollamaAvailable = await OllamaAiService.IsOllamaAvailableAsync(settings.Endpoint);
            Console.WriteLine($"[Factory] Ollama利用可能: {ollamaAvailable}");

            if (!ollamaAvailable)
            {
                System.Diagnostics.Debug.WriteLine("Ollamaが実行されていません");
                return (null, string.Empty);
            }

            // モデルがダウンロード済みかチェック
            bool modelAvailable = await OllamaAiService.IsModelAvailableAsync(settings.Endpoint, settings.Model);
            Console.WriteLine($"[Factory] モデル利用可能: {modelAvailable}");

            if (!modelAvailable)
            {
                System.Diagnostics.Debug.WriteLine($"モデル '{settings.Model}' がダウンロードされていません");
                return (null, string.Empty);
            }

            // Ollamaサービスを作成
            var service = new OllamaAiService(settings.Endpoint, settings.Model);
            var serviceType = $"Ollama ({settings.Model})";

            Console.WriteLine($"[Factory] Ollamaサービス作成成功: {serviceType}");
            System.Diagnostics.Debug.WriteLine($"Ollamaサービス作成成功: {serviceType}");
            return (service, serviceType);
        }

        /// <summary>
        /// LLamaSharpサービスの作成を試みます（将来実装用）
        /// </summary>
        private static (IAiService? service, string serviceType) TryCreateLLamaSharpService(LocalLlmSettings settings)
        {
            // モデルファイルの存在チェック
            if (string.IsNullOrWhiteSpace(settings.ModelPath) || !File.Exists(settings.ModelPath))
            {
                System.Diagnostics.Debug.WriteLine($"モデルファイルが見つかりません: {settings.ModelPath}");
                return (null, string.Empty);
            }

            // TODO: LLamaSharpサービスの実装
            System.Diagnostics.Debug.WriteLine("LLamaSharpは未実装です");
            return (null, string.Empty);
        }

        /// <summary>
        /// ONNXサービスの作成を試みます（将来実装用）
        /// </summary>
        private static (IAiService? service, string serviceType) TryCreateOnnxService(LocalLlmSettings settings)
        {
            // モデルディレクトリの存在チェック
            if (string.IsNullOrWhiteSpace(settings.ModelPath) || !Directory.Exists(settings.ModelPath))
            {
                System.Diagnostics.Debug.WriteLine($"モデルディレクトリが見つかりません: {settings.ModelPath}");
                return (null, string.Empty);
            }

            // TODO: ONNXサービスの実装
            System.Diagnostics.Debug.WriteLine("ONNXは未実装です");
            return (null, string.Empty);
        }

        /// <summary>
        /// 利用可能なOllamaモデルのリストを取得します
        /// </summary>
        public static async Task<System.Collections.Generic.List<string>> GetAvailableOllamaModelsAsync()
        {
            var settings = AppSettings.Instance.LocalLlm;

            if (await OllamaAiService.IsOllamaAvailableAsync(settings.Endpoint))
            {
                return await OllamaAiService.GetAvailableModelsAsync(settings.Endpoint);
            }

            return new System.Collections.Generic.List<string>();
        }

        /// <summary>
        /// LM Studioサービスの作成を試みます
        /// </summary>
        private static async Task<(IAiService? service, string serviceType)> TryCreateLmStudioServiceAsync()
        {
            var settings = AppSettings.Instance.LmStudio;

            Console.WriteLine($"[Factory] LM Studio初期化開始 - Endpoint: {settings.Endpoint}");

            // LM Studioが実行中かチェック
            bool lmStudioAvailable = await LmStudioAiService.IsLmStudioAvailableAsync(settings.Endpoint);
            Console.WriteLine($"[Factory] LM Studio利用可能: {lmStudioAvailable}");

            if (!lmStudioAvailable)
            {
                System.Diagnostics.Debug.WriteLine("LM Studioが実行されていません");
                return (null, string.Empty);
            }

            // LM Studioサービスを作成
            var service = new LmStudioAiService(settings.Endpoint, settings.Model);
            var serviceType = $"LM Studio ({settings.Model})";

            Console.WriteLine($"[Factory] LM Studioサービス作成成功: {serviceType}");
            return (service, serviceType);
        }

        /// <summary>
        /// 利用可能なLM Studioモデルのリストを取得します
        /// </summary>
        public static async Task<System.Collections.Generic.List<string>> GetAvailableLmStudioModelsAsync()
        {
            var settings = AppSettings.Instance.LmStudio;

            if (await LmStudioAiService.IsLmStudioAvailableAsync(settings.Endpoint))
            {
                return await LmStudioAiService.GetAvailableModelsAsync(settings.Endpoint);
            }

            return new System.Collections.Generic.List<string>();
        }
    }
}
