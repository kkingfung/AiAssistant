using System;
using System.Threading;
using System.Threading.Tasks;

namespace AiAssistant
{
    /// <summary>
    /// 天気情報を表すクラス
    /// </summary>
    public sealed class WeatherInfo
    {
        public string City { get; set; } = string.Empty;
        public DateTime DateTime { get; set; }
        public double Temperature { get; set; }
        public double FeelsLike { get; set; }
        public int Humidity { get; set; }
        public double WindSpeed { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public double? PrecipitationProbability { get; set; }
        public double? PrecipitationMm { get; set; }
    }

    /// <summary>
    /// 天気予報（1日分）を表すクラス
    /// </summary>
    public sealed class DailyForecast
    {
        public DateTime Date { get; set; }
        public double MaxTemp { get; set; }
        public double MinTemp { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public double? PrecipitationProbability { get; set; }
        public double? PrecipitationSum { get; set; }
    }

    /// <summary>
    /// 天気サービスのインターフェース
    /// </summary>
    public interface IWeatherService
    {
        /// <summary>
        /// 現在の天気を取得します
        /// </summary>
        Task<WeatherInfo?> GetCurrentWeatherAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 週間天気予報を取得します
        /// </summary>
        Task<DailyForecast[]> GetWeeklyForecastAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 天気情報をサマリー文字列に変換します
        /// </summary>
        string FormatWeatherSummary(WeatherInfo? current, DailyForecast[]? forecast);
    }
}
