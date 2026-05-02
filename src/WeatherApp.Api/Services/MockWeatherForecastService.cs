using WeatherApp.Api.Models;

namespace WeatherApp.Api.Services;

public sealed class MockWeatherForecastService : IWeatherForecastService
{
    private static readonly IReadOnlyList<LocationSuggestion> Locations =
    [
        new("San Francisco", "California", "United States", 37.7749m, -122.4194m),
        new("Seattle", "Washington", "United States", 47.6062m, -122.3321m),
        new("Chicago", "Illinois", "United States", 41.8781m, -87.6298m),
        new("New York", "New York", "United States", 40.7128m, -74.0060m),
        new("Austin", "Texas", "United States", 30.2672m, -97.7431m)
    ];

    public Task<WeatherDashboard> GetDashboardAsync(string location, string unitSystem, CancellationToken cancellationToken)
    {
        var units = WeatherUnits.Normalize(unitSystem);
        var isImperial = units == WeatherUnits.Imperial;
        var now = new DateTimeOffset(2026, 5, 19, 21, 41, 0, TimeSpan.FromHours(-7));
        var today = DateOnly.FromDateTime(now.DateTime);
        var sunrise = new DateTimeOffset(now.Year, now.Month, now.Day, 5, 48, 0, now.Offset);
        var sunset = new DateTimeOffset(now.Year, now.Month, now.Day, 20, 24, 0, now.Offset);
        var temp = isImperial ? 64 : 18;
        var feelsLike = isImperial ? 63 : 17;
        var low = isImperial ? 61 : 16;
        var high = isImperial ? 82 : 28;

        var current = new CurrentWeather(
            NormalizeLocation(location),
            now,
            "Clear Night",
            "A clear and calm night with mild temperatures.",
            "Perfect for a peaceful evening.",
            temp,
            feelsLike,
            low,
            high,
            sunrise,
            sunset,
            "https://images.unsplash.com/photo-1500530855697-b586d89ba3ee?auto=format&fit=crop&w=1600&q=80");

        IReadOnlyList<HourlyForecast> hourly =
        [
            new("Now", now, "Clear Night", temp, isImperial ? 5 : 2.2m, 4),
            new("11 PM", now.AddHours(1), "Clear Night", isImperial ? 63 : 17, isImperial ? 4 : 2m, 5),
            new("12 AM", now.AddHours(2), "Clear Night", isImperial ? 61 : 16, isImperial ? 4 : 2m, 5),
            new("1 AM", now.AddHours(3), "Clear Night", isImperial ? 61 : 16, isImperial ? 4 : 1.8m, 4),
            new("2 AM", now.AddHours(4), "Clear Night", isImperial ? 59 : 15, isImperial ? 4 : 1.8m, 3),
            new("3 AM", now.AddHours(5), "Clear Night", isImperial ? 57 : 14, isImperial ? 3 : 1.5m, 3),
            new("4 AM", now.AddHours(6), "Clear Night", isImperial ? 57 : 14, isImperial ? 3 : 1.5m, 2)
        ];

        IReadOnlyList<DailyForecast> daily =
        [
            new("Tue", today, "Partly Cloudy", isImperial ? 73 : 23, isImperial ? 57 : 14, 10),
            new("Wed", today.AddDays(1), "Rain Showers", isImperial ? 68 : 20, isImperial ? 55 : 13, 60),
            new("Thu", today.AddDays(2), "Cloudy", isImperial ? 70 : 21, isImperial ? 57 : 14, 20),
            new("Fri", today.AddDays(3), "Sunny", isImperial ? 75 : 24, isImperial ? 59 : 15, 5),
            new("Sat", today.AddDays(4), "Sunny", isImperial ? 79 : 26, isImperial ? 61 : 16, 5)
        ];

        IReadOnlyList<WeatherPreview> previews =
        [
            new("Sunny", isImperial ? 82 : 28, isImperial ? 61 : 16, "Bright and sunny skies throughout the day."),
            new("Rainy", isImperial ? 68 : 20, isImperial ? 57 : 14, "Light to moderate rain expected in the evening."),
            new("Cloudy", isImperial ? 72 : 22, isImperial ? 59 : 15, "Mostly cloudy skies with cool temperatures.")
        ];

        IReadOnlyList<WeatherMetric> metrics =
        [
            new("humidity", "Humidity", "62", "%", "Comfortable", [18, 20, 19, 23, 25, 21, 27, 29, 26, 27]),
            new("wind", "Wind", isImperial ? "11" : "5", WeatherUnits.WindUnit(units), "NW", [14, 13, 15, 19, 21, 18, 16, 20, 29, 23]),
            new("precipitation", "Precipitation", "0.2", "mm", "Light", [1, 2, 5, 8, 12, 16, 7, 3, 9]),
            new("visibility", "Visibility", "16", "km", "Excellent", [12, 13, 14, 17, 19, 18, 15, 13, 15, 14])
        ];

        var dashboard = new WeatherDashboard(
            current,
            hourly,
            daily,
            previews,
            metrics,
            Locations,
            units,
            WeatherUnits.TemperatureUnit(units),
            WeatherUnits.WindUnit(units));
        return Task.FromResult(dashboard);
    }

    public Task<IReadOnlyList<LocationSuggestion>> SearchLocationsAsync(string query, CancellationToken cancellationToken)
    {
        var normalized = query.Trim();
        var matches = string.IsNullOrWhiteSpace(normalized)
            ? Locations
            : Locations
                .Where(location =>
                    location.Name.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                    location.Region.Contains(normalized, StringComparison.OrdinalIgnoreCase))
                .ToArray();

        return Task.FromResult<IReadOnlyList<LocationSuggestion>>(matches);
    }

    private static string NormalizeLocation(string location)
    {
        return string.IsNullOrWhiteSpace(location)
            ? "San Francisco, CA"
            : location.Trim();
    }
}
