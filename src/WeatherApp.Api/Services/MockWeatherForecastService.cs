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

    public Task<WeatherDashboard> GetDashboardAsync(string location, CancellationToken cancellationToken)
    {
        var now = new DateTimeOffset(2026, 5, 19, 21, 41, 0, TimeSpan.FromHours(-7));
        var today = DateOnly.FromDateTime(now.DateTime);
        var sunrise = new DateTimeOffset(now.Year, now.Month, now.Day, 5, 48, 0, now.Offset);
        var sunset = new DateTimeOffset(now.Year, now.Month, now.Day, 20, 24, 0, now.Offset);

        var current = new CurrentWeather(
            NormalizeLocation(location),
            now,
            "Clear Night",
            "A clear and calm night with mild temperatures.",
            "Perfect for a peaceful evening.",
            18,
            17,
            16,
            28,
            sunrise,
            sunset,
            "https://images.unsplash.com/photo-1500530855697-b586d89ba3ee?auto=format&fit=crop&w=1600&q=80");

        IReadOnlyList<HourlyForecast> hourly =
        [
            new("Now", now, "Clear Night", 18, 8, 4),
            new("11 PM", now.AddHours(1), "Clear Night", 17, 7, 5),
            new("12 AM", now.AddHours(2), "Clear Night", 16, 7, 5),
            new("1 AM", now.AddHours(3), "Clear Night", 16, 6, 4),
            new("2 AM", now.AddHours(4), "Clear Night", 15, 6, 3),
            new("3 AM", now.AddHours(5), "Clear Night", 14, 5, 3),
            new("4 AM", now.AddHours(6), "Clear Night", 14, 5, 2)
        ];

        IReadOnlyList<DailyForecast> daily =
        [
            new("Tue", today, "Partly Cloudy", 23, 14, 10),
            new("Wed", today.AddDays(1), "Rain Showers", 20, 13, 60),
            new("Thu", today.AddDays(2), "Cloudy", 21, 14, 20),
            new("Fri", today.AddDays(3), "Sunny", 24, 15, 5),
            new("Sat", today.AddDays(4), "Sunny", 26, 16, 5)
        ];

        IReadOnlyList<WeatherPreview> previews =
        [
            new("Sunny", 28, 16, "Bright and sunny skies throughout the day."),
            new("Rainy", 20, 14, "Light to moderate rain expected in the evening."),
            new("Cloudy", 22, 15, "Mostly cloudy skies with cool temperatures.")
        ];

        IReadOnlyList<WeatherMetric> metrics =
        [
            new("humidity", "Humidity", "62", "%", "Comfortable", [18, 20, 19, 23, 25, 21, 27, 29, 26, 27]),
            new("wind", "Wind", "18", "km/h", "NW", [14, 13, 15, 19, 21, 18, 16, 20, 29, 23]),
            new("precipitation", "Precipitation", "0.2", "mm", "Light", [1, 2, 5, 8, 12, 16, 7, 3, 9]),
            new("visibility", "Visibility", "16", "km", "Excellent", [12, 13, 14, 17, 19, 18, 15, 13, 15, 14])
        ];

        var dashboard = new WeatherDashboard(current, hourly, daily, previews, metrics, Locations);
        return Task.FromResult(dashboard);
    }

    public Task<IReadOnlyList<LocationSuggestion>> SearchLocationsAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(Locations);
    }

    private static string NormalizeLocation(string location)
    {
        return string.IsNullOrWhiteSpace(location)
            ? "San Francisco, CA"
            : location.Trim();
    }
}
