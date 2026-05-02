using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using WeatherApp.Api.Models;

namespace WeatherApp.Api.Services;

public sealed class OpenWeatherForecastService(
    HttpClient httpClient,
    IOptions<OpenWeatherOptions> options,
    IMemoryCache memoryCache,
    MockWeatherForecastService fallback) : IWeatherForecastService
{
    private static readonly IReadOnlyDictionary<string, string> UsStates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Alabama"] = "AL",
        ["Alaska"] = "AK",
        ["Arizona"] = "AZ",
        ["Arkansas"] = "AR",
        ["California"] = "CA",
        ["Colorado"] = "CO",
        ["Connecticut"] = "CT",
        ["Delaware"] = "DE",
        ["Florida"] = "FL",
        ["Georgia"] = "GA",
        ["Hawaii"] = "HI",
        ["Idaho"] = "ID",
        ["Illinois"] = "IL",
        ["Indiana"] = "IN",
        ["Iowa"] = "IA",
        ["Kansas"] = "KS",
        ["Kentucky"] = "KY",
        ["Louisiana"] = "LA",
        ["Maine"] = "ME",
        ["Maryland"] = "MD",
        ["Massachusetts"] = "MA",
        ["Michigan"] = "MI",
        ["Minnesota"] = "MN",
        ["Mississippi"] = "MS",
        ["Missouri"] = "MO",
        ["Montana"] = "MT",
        ["Nebraska"] = "NE",
        ["Nevada"] = "NV",
        ["New Hampshire"] = "NH",
        ["New Jersey"] = "NJ",
        ["New Mexico"] = "NM",
        ["New York"] = "NY",
        ["North Carolina"] = "NC",
        ["North Dakota"] = "ND",
        ["Ohio"] = "OH",
        ["Oklahoma"] = "OK",
        ["Oregon"] = "OR",
        ["Pennsylvania"] = "PA",
        ["Rhode Island"] = "RI",
        ["South Carolina"] = "SC",
        ["South Dakota"] = "SD",
        ["Tennessee"] = "TN",
        ["Texas"] = "TX",
        ["Utah"] = "UT",
        ["Vermont"] = "VT",
        ["Virginia"] = "VA",
        ["Washington"] = "WA",
        ["West Virginia"] = "WV",
        ["Wisconsin"] = "WI",
        ["Wyoming"] = "WY"
    };

    private readonly OpenWeatherOptions _options = options.Value;

    public async Task<WeatherDashboard> GetDashboardAsync(string location, string unitSystem, CancellationToken cancellationToken)
    {
        var units = WeatherUnits.Normalize(unitSystem);
        var dashboardCacheKey = $"openweather:dashboard:{location.Trim().ToLowerInvariant()}:{units}";

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return await fallback.GetDashboardAsync(location, units, cancellationToken);
        }

        if (memoryCache.TryGetValue(dashboardCacheKey, out WeatherDashboard? cachedDashboard) && cachedDashboard is not null)
        {
            return cachedDashboard;
        }

        try
        {
            var locations = await SearchLocationsAsync(location, cancellationToken);
            var selectedLocation = locations.FirstOrDefault();

            if (selectedLocation is null)
            {
                return await fallback.GetDashboardAsync(location, units, cancellationToken);
            }

            var oneCall = await GetJsonAsync<OpenWeatherOneCallResponse>(
                $"/data/3.0/onecall?lat={selectedLocation.Latitude.ToString(CultureInfo.InvariantCulture)}&lon={selectedLocation.Longitude.ToString(CultureInfo.InvariantCulture)}&exclude=minutely&units={units}&appid={Uri.EscapeDataString(_options.ApiKey)}",
                cancellationToken);

            if (oneCall?.Current is null)
            {
                return await fallback.GetDashboardAsync(location, units, cancellationToken);
            }

            var dashboard = MapDashboard(selectedLocation, oneCall, locations, units);
            memoryCache.Set(dashboardCacheKey, dashboard, TimeSpan.FromMinutes(10));
            return dashboard;
        }
        catch
        {
            return await fallback.GetDashboardAsync(location, units, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<LocationSuggestion>> SearchLocationsAsync(string query, CancellationToken cancellationToken)
    {
        var normalizedQuery = query.Trim().ToLowerInvariant();
        var locationCacheKey = $"openweather:locations:{normalizedQuery}";

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return await fallback.SearchLocationsAsync(query, cancellationToken);
        }

        if (memoryCache.TryGetValue(locationCacheKey, out IReadOnlyList<LocationSuggestion>? cachedLocations) && cachedLocations is not null)
        {
            return cachedLocations;
        }

        try
        {
            var trimmed = query.Trim();
            if (LooksLikeZipCode(trimmed))
            {
                var zip = await GetJsonAsync<OpenWeatherZipResponse>(
                    $"/geo/1.0/zip?zip={Uri.EscapeDataString(trimmed)},US&appid={Uri.EscapeDataString(_options.ApiKey)}",
                    cancellationToken);

                LocationSuggestion[] zipLocations = zip is null
                    ? []
                    : [new LocationSuggestion(zip.Name, zip.Country, zip.Country, (decimal)zip.Lat, (decimal)zip.Lon)];

                memoryCache.Set(locationCacheKey, zipLocations, TimeSpan.FromHours(6));
                return zipLocations;
            }

            IReadOnlyList<OpenWeatherLocationResponse>? results = [];
            foreach (var geoQuery in BuildGeoQueries(trimmed))
            {
                results = await GetJsonAsync<IReadOnlyList<OpenWeatherLocationResponse>>(
                    $"/geo/1.0/direct?q={Uri.EscapeDataString(geoQuery)}&limit=5&appid={Uri.EscapeDataString(_options.ApiKey)}",
                    cancellationToken);

                if (results?.Count > 0)
                {
                    break;
                }
            }

            var locations = results?
                .Where(result => !string.IsNullOrWhiteSpace(result.Name))
                .Select(result => new LocationSuggestion(
                    result.Name,
                    result.State ?? result.Country,
                    result.Country,
                    (decimal)result.Lat,
                    (decimal)result.Lon))
                .ToArray() ?? [];

            memoryCache.Set(locationCacheKey, locations, TimeSpan.FromHours(6));
            return locations;
        }
        catch
        {
            return await fallback.SearchLocationsAsync(query, cancellationToken);
        }
    }

    private async Task<T?> GetJsonAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(path, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
    }

    private static WeatherDashboard MapDashboard(
        LocationSuggestion location,
        OpenWeatherOneCallResponse oneCall,
        IReadOnlyList<LocationSuggestion> locations,
        string units)
    {
        var offset = TimeSpan.FromSeconds(oneCall.TimezoneOffset);
        var current = oneCall.Current!;
        var currentWeather = current.Weather.FirstOrDefault();
        var currentCondition = ToTitleCase(currentWeather?.Description ?? currentWeather?.Main ?? "Current Weather");
        var daily = oneCall.Daily.Take(5).ToArray();

        var currentModel = new CurrentWeather(
            FormatLocation(location),
            FromUnix(current.Dt, offset),
            currentCondition,
            BuildSummary(currentCondition, current.Temp, WeatherUnits.TemperatureUnit(units)),
            BuildDescription(current),
            Round(current.Temp),
            Round(current.FeelsLike),
            Round(daily.FirstOrDefault()?.Temp.Min ?? current.Temp),
            Round(daily.FirstOrDefault()?.Temp.Max ?? current.Temp),
            FromUnix(current.Sunrise, offset),
            FromUnix(current.Sunset, offset),
            BackgroundFor(currentWeather?.Main));

        var hourlyModels = oneCall.Hourly
            .Take(7)
            .Select((hour, index) =>
            {
                var time = FromUnix(hour.Dt, offset);
                return new HourlyForecast(
                    index == 0 ? "Now" : time.ToString("h tt", CultureInfo.InvariantCulture),
                    time,
                    ToTitleCase(hour.Weather.FirstOrDefault()?.Description ?? "Forecast"),
                    Round(hour.Temp),
                    Math.Round((decimal)hour.WindSpeed, 1),
                    Percent(hour.Pop));
            })
            .ToArray();

        var dailyModels = daily
            .Select(day =>
            {
                var time = FromUnix(day.Dt, offset);
                return new DailyForecast(
                    time.ToString("ddd", CultureInfo.InvariantCulture),
                    DateOnly.FromDateTime(time.DateTime),
                    ToTitleCase(day.Weather.FirstOrDefault()?.Description ?? "Forecast"),
                    Round(day.Temp.Max),
                    Round(day.Temp.Min),
                    Percent(day.Pop));
            })
            .ToArray();

        var previews = daily
            .Skip(1)
            .Take(3)
            .Select(day => new WeatherPreview(
                ToTitleCase(day.Weather.FirstOrDefault()?.Main ?? "Forecast"),
                Round(day.Temp.Max),
                Round(day.Temp.Min),
                ToSentence(day.Summary ?? day.Weather.FirstOrDefault()?.Description ?? "Weather conditions continue through the day.")))
            .ToArray();

        IReadOnlyList<WeatherMetric> metrics =
        [
            new("humidity", "Humidity", current.Humidity.ToString(CultureInfo.InvariantCulture), "%", HumidityHint(current.Humidity), Trend(oneCall.Hourly.Select(hour => hour.Humidity))),
            new("wind", "Wind", Math.Round(current.WindSpeed, 1).ToString("0.#", CultureInfo.InvariantCulture), WeatherUnits.WindUnit(units), DegreesToCompass(current.WindDeg), Trend(oneCall.Hourly.Select(hour => (int)Math.Round(hour.WindSpeed * 4)))),
            new("precipitation", "Precipitation", PrecipitationValue(daily.FirstOrDefault()), "mm", "Expected", Trend(daily.Select(day => Percent(day.Pop)))),
            new("visibility", "Visibility", VisibilityValue(current.Visibility, units), VisibilityUnit(units), "Observed", Trend(oneCall.Hourly.Select(hour => Math.Max(1, (hour.Visibility ?? current.Visibility) / 500))))
        ];

        return new WeatherDashboard(
            currentModel,
            hourlyModels,
            dailyModels,
            previews,
            metrics,
            locations,
            units,
            WeatherUnits.TemperatureUnit(units),
            WeatherUnits.WindUnit(units));
    }

    private static bool LooksLikeZipCode(string value)
    {
        return value.Length is 5 or 10 && char.IsDigit(value[0]) && value.All(character => char.IsDigit(character) || character == '-');
    }

    private static IReadOnlyList<string> BuildGeoQueries(string query)
    {
        var normalized = query.Trim();
        var parts = normalized
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 2 && TryNormalizeUsState(parts[1], out var stateCode))
        {
            return [$"{parts[0]},{stateCode},US", normalized, parts[0]];
        }

        if (parts.Length == 2 && parts[1].Length == 2)
        {
            return [$"{parts[0]},{parts[1].ToUpperInvariant()}", normalized, parts[0]];
        }

        return [normalized];
    }

    private static bool TryNormalizeUsState(string value, out string stateCode)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 2 && UsStates.Values.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
        {
            stateCode = trimmed.ToUpperInvariant();
            return true;
        }

        return UsStates.TryGetValue(trimmed, out stateCode!);
    }

    private static DateTimeOffset FromUnix(long value, TimeSpan offset)
    {
        return DateTimeOffset.FromUnixTimeSeconds(value).ToOffset(offset);
    }

    private static string FormatLocation(LocationSuggestion location)
    {
        var region = string.IsNullOrWhiteSpace(location.Region) ? location.Country : location.Region;
        return $"{location.Name}, {region}";
    }

    private static string ToTitleCase(string value)
    {
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value.Trim().ToLowerInvariant());
    }

    private static string ToSentence(string value)
    {
        var trimmed = value.Trim();
        return trimmed.EndsWith('.') ? trimmed : $"{trimmed}.";
    }

    private static string BuildSummary(string condition, double temperature, string temperatureUnit)
    {
        return $"{condition} with temperatures near {Round(temperature)} degrees {temperatureUnit}.";
    }

    private static string BuildDescription(OpenWeatherCurrentWeather current)
    {
        return $"Humidity is {current.Humidity}% with winds from {DegreesToCompass(current.WindDeg)}.";
    }

    private static string BackgroundFor(string? condition)
    {
        return condition?.ToLowerInvariant() switch
        {
            "rain" or "drizzle" or "thunderstorm" => "https://images.unsplash.com/photo-1519692933481-e162a57d6721?auto=format&fit=crop&w=1600&q=80",
            "snow" => "https://images.unsplash.com/photo-1516431883659-655d41c09bf9?auto=format&fit=crop&w=1600&q=80",
            "clouds" => "https://images.unsplash.com/photo-1534088568595-a066f410bcda?auto=format&fit=crop&w=1600&q=80",
            "clear" => "https://images.unsplash.com/photo-1500530855697-b586d89ba3ee?auto=format&fit=crop&w=1600&q=80",
            _ => "https://images.unsplash.com/photo-1500530855697-b586d89ba3ee?auto=format&fit=crop&w=1600&q=80"
        };
    }

    private static int Round(double value)
    {
        return (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }

    private static int Percent(double value)
    {
        return (int)Math.Round(value * 100, MidpointRounding.AwayFromZero);
    }

    private static IReadOnlyList<int> Trend(IEnumerable<int> values)
    {
        return values.Take(10).ToArray();
    }

    private static string HumidityHint(int humidity)
    {
        return humidity switch
        {
            < 35 => "Dry",
            > 70 => "Humid",
            _ => "Comfortable"
        };
    }

    private static string DegreesToCompass(int degrees)
    {
        var directions = new[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
        var index = (int)Math.Round(degrees / 45.0) % 8;
        return directions[index];
    }

    private static string PrecipitationValue(OpenWeatherDailyWeather? day)
    {
        var volume = (day?.Rain ?? 0) + (day?.Snow ?? 0);
        return volume.ToString("0.#", CultureInfo.InvariantCulture);
    }

    private static string VisibilityValue(int meters, string units)
    {
        var value = WeatherUnits.Normalize(units) == WeatherUnits.Metric
            ? meters / 1000m
            : meters / 1609.344m;

        return value.ToString("0.#", CultureInfo.InvariantCulture);
    }

    private static string VisibilityUnit(string units)
    {
        return WeatherUnits.Normalize(units) == WeatherUnits.Metric ? "km" : "mi";
    }
}

public sealed record OpenWeatherLocationResponse(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("lat")] double Lat,
    [property: JsonPropertyName("lon")] double Lon,
    [property: JsonPropertyName("country")] string Country,
    [property: JsonPropertyName("state")] string? State);

public sealed record OpenWeatherZipResponse(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("lat")] double Lat,
    [property: JsonPropertyName("lon")] double Lon,
    [property: JsonPropertyName("country")] string Country);

public sealed record OpenWeatherOneCallResponse(
    [property: JsonPropertyName("timezone_offset")] int TimezoneOffset,
    [property: JsonPropertyName("current")] OpenWeatherCurrentWeather? Current,
    [property: JsonPropertyName("hourly")] IReadOnlyList<OpenWeatherHourlyWeather> Hourly,
    [property: JsonPropertyName("daily")] IReadOnlyList<OpenWeatherDailyWeather> Daily);

public sealed record OpenWeatherCurrentWeather(
    [property: JsonPropertyName("dt")] long Dt,
    [property: JsonPropertyName("sunrise")] long Sunrise,
    [property: JsonPropertyName("sunset")] long Sunset,
    [property: JsonPropertyName("temp")] double Temp,
    [property: JsonPropertyName("feels_like")] double FeelsLike,
    [property: JsonPropertyName("humidity")] int Humidity,
    [property: JsonPropertyName("wind_speed")] double WindSpeed,
    [property: JsonPropertyName("wind_deg")] int WindDeg,
    [property: JsonPropertyName("visibility")] int Visibility,
    [property: JsonPropertyName("weather")] IReadOnlyList<OpenWeatherCondition> Weather);

public sealed record OpenWeatherHourlyWeather(
    [property: JsonPropertyName("dt")] long Dt,
    [property: JsonPropertyName("temp")] double Temp,
    [property: JsonPropertyName("humidity")] int Humidity,
    [property: JsonPropertyName("wind_speed")] double WindSpeed,
    [property: JsonPropertyName("visibility")] int? Visibility,
    [property: JsonPropertyName("pop")] double Pop,
    [property: JsonPropertyName("weather")] IReadOnlyList<OpenWeatherCondition> Weather);

public sealed record OpenWeatherDailyWeather(
    [property: JsonPropertyName("dt")] long Dt,
    [property: JsonPropertyName("temp")] OpenWeatherDailyTemperature Temp,
    [property: JsonPropertyName("pop")] double Pop,
    [property: JsonPropertyName("rain")] double? Rain,
    [property: JsonPropertyName("snow")] double? Snow,
    [property: JsonPropertyName("summary")] string? Summary,
    [property: JsonPropertyName("weather")] IReadOnlyList<OpenWeatherCondition> Weather);

public sealed record OpenWeatherDailyTemperature(
    [property: JsonPropertyName("min")] double Min,
    [property: JsonPropertyName("max")] double Max);

public sealed record OpenWeatherCondition(
    [property: JsonPropertyName("main")] string Main,
    [property: JsonPropertyName("description")] string Description);
