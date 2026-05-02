namespace WeatherApp.Api.Models;

public sealed record WeatherDashboard(
    CurrentWeather Current,
    IReadOnlyList<HourlyForecast> Hourly,
    IReadOnlyList<DailyForecast> Daily,
    IReadOnlyList<WeatherPreview> Previews,
    IReadOnlyList<WeatherMetric> Metrics,
    IReadOnlyList<LocationSuggestion> Locations);

public sealed record CurrentWeather(
    string Location,
    DateTimeOffset ObservedAt,
    string Condition,
    string Summary,
    string Description,
    int TemperatureC,
    int FeelsLikeC,
    int LowC,
    int HighC,
    DateTimeOffset Sunrise,
    DateTimeOffset Sunset,
    string BackgroundImageUrl);

public sealed record HourlyForecast(
    string Label,
    DateTimeOffset Time,
    string Condition,
    int TemperatureC,
    int WindKph,
    int PrecipitationChance);

public sealed record DailyForecast(
    string Day,
    DateOnly Date,
    string Condition,
    int HighC,
    int LowC,
    int PrecipitationChance);

public sealed record WeatherPreview(
    string Condition,
    int HighC,
    int LowC,
    string Description);

public sealed record WeatherMetric(
    string Key,
    string Label,
    string Value,
    string Unit,
    string Hint,
    IReadOnlyList<int> Trend);

public sealed record LocationSuggestion(
    string Name,
    string Region,
    string Country,
    decimal Latitude,
    decimal Longitude);
