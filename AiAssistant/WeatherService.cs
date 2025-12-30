using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AiAssistant
{
    /// <summary>
    /// Open-Meteo APIを使用した天気サービス実装
    /// 無料でAPIキー不要
    /// </summary>
    public sealed class WeatherService : IWeatherService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _city;
        private readonly double _latitude;
        private readonly double _longitude;
        private bool _disposed;

        // 東京の座標
        private const double TokyoLatitude = 35.6762;
        private const double TokyoLongitude = 139.6503;

        public WeatherService(string city = "東京", double? latitude = null, double? longitude = null)
        {
            _city = city;
            _latitude = latitude ?? TokyoLatitude;
            _longitude = longitude ?? TokyoLongitude;

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        /// <summary>
        /// 現在の天気を取得します
        /// </summary>
        public async Task<WeatherInfo?> GetCurrentWeatherAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"https://api.open-meteo.com/v1/forecast?" +
                    $"latitude={_latitude.ToString(CultureInfo.InvariantCulture)}&" +
                    $"longitude={_longitude.ToString(CultureInfo.InvariantCulture)}&" +
                    $"current=temperature_2m,relative_humidity_2m,apparent_temperature,weather_code,wind_speed_10m,precipitation&" +
                    $"timezone=Asia%2FTokyo";

                var response = await _httpClient.GetStringAsync(url, cancellationToken);
                var json = JsonDocument.Parse(response);
                var current = json.RootElement.GetProperty("current");

                var weatherCode = current.GetProperty("weather_code").GetInt32();
                var (description, icon) = GetWeatherDescription(weatherCode);

                return new WeatherInfo
                {
                    City = _city,
                    DateTime = DateTime.Now,
                    Temperature = current.GetProperty("temperature_2m").GetDouble(),
                    FeelsLike = current.GetProperty("apparent_temperature").GetDouble(),
                    Humidity = current.GetProperty("relative_humidity_2m").GetInt32(),
                    WindSpeed = current.GetProperty("wind_speed_10m").GetDouble(),
                    PrecipitationMm = current.GetProperty("precipitation").GetDouble(),
                    Description = description,
                    Icon = icon
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Weather] 現在の天気取得エラー: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 週間天気予報を取得します
        /// </summary>
        public async Task<DailyForecast[]> GetWeeklyForecastAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"https://api.open-meteo.com/v1/forecast?" +
                    $"latitude={_latitude.ToString(CultureInfo.InvariantCulture)}&" +
                    $"longitude={_longitude.ToString(CultureInfo.InvariantCulture)}&" +
                    $"daily=temperature_2m_max,temperature_2m_min,weather_code,precipitation_probability_max,precipitation_sum&" +
                    $"timezone=Asia%2FTokyo&" +
                    $"forecast_days=7";

                var response = await _httpClient.GetStringAsync(url, cancellationToken);
                var json = JsonDocument.Parse(response);
                var daily = json.RootElement.GetProperty("daily");

                var dates = daily.GetProperty("time");
                var maxTemps = daily.GetProperty("temperature_2m_max");
                var minTemps = daily.GetProperty("temperature_2m_min");
                var weatherCodes = daily.GetProperty("weather_code");
                var precipProb = daily.GetProperty("precipitation_probability_max");
                var precipSum = daily.GetProperty("precipitation_sum");

                var forecasts = new DailyForecast[dates.GetArrayLength()];

                for (int i = 0; i < forecasts.Length; i++)
                {
                    var weatherCode = weatherCodes[i].GetInt32();
                    var (description, icon) = GetWeatherDescription(weatherCode);

                    forecasts[i] = new DailyForecast
                    {
                        Date = DateTime.Parse(dates[i].GetString()!),
                        MaxTemp = maxTemps[i].GetDouble(),
                        MinTemp = minTemps[i].GetDouble(),
                        Description = description,
                        Icon = icon,
                        PrecipitationProbability = precipProb[i].GetDouble(),
                        PrecipitationSum = precipSum[i].GetDouble()
                    };
                }

                return forecasts;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Weather] 週間予報取得エラー: {ex.Message}");
                return Array.Empty<DailyForecast>();
            }
        }

        /// <summary>
        /// 天気情報をサマリー文字列に変換します
        /// </summary>
        public string FormatWeatherSummary(WeatherInfo? current, DailyForecast[]? forecast)
        {
            var sb = new StringBuilder();

            if (current != null)
            {
                sb.AppendLine($"🌤 {current.City}の天気");
                sb.AppendLine();
                sb.AppendLine($"【現在】{current.Icon} {current.Description}");
                sb.AppendLine($"  気温: {current.Temperature:F1}°C (体感 {current.FeelsLike:F1}°C)");
                sb.AppendLine($"  湿度: {current.Humidity}%");
                sb.AppendLine($"  風速: {current.WindSpeed:F1} km/h");
                if (current.PrecipitationMm > 0)
                {
                    sb.AppendLine($"  降水: {current.PrecipitationMm:F1} mm");
                }
                sb.AppendLine();
            }

            if (forecast != null && forecast.Length > 0)
            {
                sb.AppendLine("【週間予報】");
                foreach (var day in forecast)
                {
                    var dateStr = day.Date.ToString("M/d(ddd)");
                    var tempStr = $"{day.MaxTemp:F0}°/{day.MinTemp:F0}°";
                    var rainStr = day.PrecipitationProbability > 0 ? $" 🌧{day.PrecipitationProbability:F0}%" : "";
                    sb.AppendLine($"  {dateStr}: {day.Icon} {tempStr}{rainStr}");
                }
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// WMO Weather Codeから天気説明とアイコンを取得
        /// </summary>
        private static (string Description, string Icon) GetWeatherDescription(int code)
        {
            return code switch
            {
                0 => ("快晴", "☀️"),
                1 => ("晴れ", "🌤"),
                2 => ("一部曇り", "⛅"),
                3 => ("曇り", "☁️"),
                45 or 48 => ("霧", "🌫"),
                51 or 53 or 55 => ("霧雨", "🌧"),
                56 or 57 => ("凍結霧雨", "🌧❄"),
                61 or 63 or 65 => ("雨", "🌧"),
                66 or 67 => ("凍結雨", "🌧❄"),
                71 or 73 or 75 => ("雪", "❄️"),
                77 => ("霰", "🌨"),
                80 or 81 or 82 => ("にわか雨", "🌦"),
                85 or 86 => ("にわか雪", "🌨"),
                95 => ("雷雨", "⛈"),
                96 or 99 => ("雹を伴う雷雨", "⛈"),
                _ => ("不明", "🌡")
            };
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _httpClient.Dispose();
        }
    }
}
