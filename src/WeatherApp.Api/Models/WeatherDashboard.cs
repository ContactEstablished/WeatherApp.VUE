namespace WeatherApp.Api.Models;

public sealed record WeatherDashboard(
    CurrentWeather Current,
    IReadOnlyList<HourlyForecast> Hourly,
    IReadOnlyList<DailyForecast> Daily,
    IReadOnlyList<WeatherPreview> Previews,
    IReadOnlyList<WeatherMetric> Metrics,
    IReadOnlyList<LocationSuggestion> Locations,
    string UnitSystem,
    string TemperatureUnit,
    string WindUnit);

public sealed record CurrentWeather(
    string Location,
    DateTimeOffset ObservedAt,
    string Condition,
    string Summary,
    string Description,
    int Temperature,
    int FeelsLike,
    int Low,
    int High,
    DateTimeOffset Sunrise,
    DateTimeOffset Sunset,
    string BackgroundImageUrl);

public sealed record HourlyForecast(
    string Label,
    DateTimeOffset Time,
    string Condition,
    int Temperature,
    decimal WindSpeed,
    int PrecipitationChance);

public sealed record DailyForecast(
    string Day,
    DateOnly Date,
    string Condition,
    int High,
    int Low,
    int PrecipitationChance);

public sealed record WeatherPreview(
    string Condition,
    int High,
    int Low,
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
    decimal Longitude,
    int? Id = null,
    bool IsDefault = false);
