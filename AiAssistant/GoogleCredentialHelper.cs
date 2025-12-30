using System;
using System.IO;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;

namespace AiAssistant
{
    /// <summary>
    /// Google OAuth2認証情報を管理するヘルパークラス
    /// client_secret.jsonファイルから認証情報を読み込みます
    /// </summary>
    public static class GoogleCredentialHelper
    {
        private static readonly string[] PossibleFilenames = new[]
        {
            "client_secret.json",
            "client_secrets.json",
            "credentials.json"
        };

        /// <summary>
        /// client_secret.jsonファイルのパスを取得します
        /// </summary>
        public static string? GetClientSecretPath()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;

            foreach (var filename in PossibleFilenames)
            {
                var path = Path.Combine(baseDir, filename);
                if (File.Exists(path))
                {
                    Console.WriteLine($"[GoogleCredential] 認証ファイル発見: {path}");
                    return path;
                }
            }

            // AiAssistantフォルダ内も確認
            var projectDir = Path.GetDirectoryName(baseDir.TrimEnd(Path.DirectorySeparatorChar));
            if (projectDir != null)
            {
                foreach (var filename in PossibleFilenames)
                {
                    var path = Path.Combine(projectDir, filename);
                    if (File.Exists(path))
                    {
                        Console.WriteLine($"[GoogleCredential] 認証ファイル発見: {path}");
                        return path;
                    }
                }
            }

            Console.WriteLine("[GoogleCredential] client_secret.jsonが見つかりません");
            return null;
        }

        /// <summary>
        /// client_secret.jsonからClientSecretsを読み込みます
        /// </summary>
        public static ClientSecrets? LoadClientSecrets()
        {
            var path = GetClientSecretPath();
            if (path == null)
            {
                // appsettings.jsonからフォールバック
                return LoadFromAppSettings();
            }

            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
                var secrets = GoogleClientSecrets.FromStream(stream);
                Console.WriteLine($"[GoogleCredential] ClientId: {secrets.Secrets.ClientId[..20]}...");
                return secrets.Secrets;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GoogleCredential] ファイル読み込みエラー: {ex.Message}");
                return LoadFromAppSettings();
            }
        }

        /// <summary>
        /// appsettings.jsonからClientSecretsを読み込みます（フォールバック）
        /// </summary>
        private static ClientSecrets? LoadFromAppSettings()
        {
            var settings = AppSettings.Instance.Google;

            if (string.IsNullOrWhiteSpace(settings.ClientId) ||
                string.IsNullOrWhiteSpace(settings.ClientSecret) ||
                settings.ClientId == "YOUR_GOOGLE_CLIENT_ID_HERE")
            {
                Console.WriteLine("[GoogleCredential] appsettings.jsonにも認証情報がありません");
                return null;
            }

            Console.WriteLine("[GoogleCredential] appsettings.jsonから認証情報を読み込みました");
            return new ClientSecrets
            {
                ClientId = settings.ClientId,
                ClientSecret = settings.ClientSecret
            };
        }

        /// <summary>
        /// Google認証が利用可能かどうかを確認します
        /// </summary>
        public static bool IsAvailable()
        {
            return GetClientSecretPath() != null || AppSettings.Instance.Google.IsConfigured;
        }
    }
}
