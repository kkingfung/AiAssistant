#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AiAssistant.Companionship
{
    /// <summary>
    /// コンパニオンデータ永続化サービスの実装
    /// companionship.json にデータを保存・読み込み
    /// </summary>
    public sealed class CompanionshipDataService : ICompanionshipDataService
    {
        private const string DataFileName = "companionship.json";
        private readonly string _dataFilePath;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public CompanionshipDataService()
        {
            _dataFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DataFileName);

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters =
                {
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                }
            };
        }

        /// <inheritdoc/>
        public bool DataExists => File.Exists(_dataFilePath);

        /// <inheritdoc/>
        public CompanionContext Load()
        {
            if (!DataExists)
            {
                Debug.WriteLine("[CompanionshipDataService] No data file found, returning default context.");
                return CompanionContext.CreateDefault();
            }

            try
            {
                var json = File.ReadAllText(_dataFilePath);
                var context = JsonSerializer.Deserialize<CompanionContext>(json, _jsonOptions);

                if (context == null)
                {
                    Debug.WriteLine("[CompanionshipDataService] Failed to deserialize, returning default context.");
                    return CompanionContext.CreateDefault();
                }

                Debug.WriteLine($"[CompanionshipDataService] Loaded context: BondPoints={context.BondPoints}, Level={context.BondLevel}");

                // 日付が変わっていたらデイリーログインボーナスのフラグを更新
                if (context.LastActiveDate.Date != DateTime.Today)
                {
                    context.DaysActive++;
                    context.LastActiveDate = DateTime.Today;
                }

                return context;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CompanionshipDataService] Error loading data: {ex.Message}");
                return CompanionContext.CreateDefault();
            }
        }

        /// <inheritdoc/>
        public async Task<CompanionContext> LoadAsync()
        {
            if (!DataExists)
            {
                Debug.WriteLine("[CompanionshipDataService] No data file found, returning default context.");
                return CompanionContext.CreateDefault();
            }

            try
            {
                var json = await File.ReadAllTextAsync(_dataFilePath);
                var context = JsonSerializer.Deserialize<CompanionContext>(json, _jsonOptions);

                if (context == null)
                {
                    Debug.WriteLine("[CompanionshipDataService] Failed to deserialize, returning default context.");
                    return CompanionContext.CreateDefault();
                }

                Debug.WriteLine($"[CompanionshipDataService] Loaded context: BondPoints={context.BondPoints}, Level={context.BondLevel}");

                // 日付が変わっていたらデイリーログインボーナスのフラグを更新
                if (context.LastActiveDate.Date != DateTime.Today)
                {
                    context.DaysActive++;
                    context.LastActiveDate = DateTime.Today;
                }

                return context;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CompanionshipDataService] Error loading data: {ex.Message}");
                return CompanionContext.CreateDefault();
            }
        }

        /// <inheritdoc/>
        public void Save(CompanionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            try
            {
                var json = JsonSerializer.Serialize(context, _jsonOptions);
                File.WriteAllText(_dataFilePath, json);

                Debug.WriteLine($"[CompanionshipDataService] Saved context: BondPoints={context.BondPoints}, Level={context.BondLevel}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CompanionshipDataService] Error saving data: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task SaveAsync(CompanionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            try
            {
                var json = JsonSerializer.Serialize(context, _jsonOptions);
                await File.WriteAllTextAsync(_dataFilePath, json);

                Debug.WriteLine($"[CompanionshipDataService] Saved context: BondPoints={context.BondPoints}, Level={context.BondLevel}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CompanionshipDataService] Error saving data: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public void Reset()
        {
            try
            {
                if (DataExists)
                {
                    File.Delete(_dataFilePath);
                    Debug.WriteLine("[CompanionshipDataService] Data file deleted.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CompanionshipDataService] Error resetting data: {ex.Message}");
            }
        }
    }
}
