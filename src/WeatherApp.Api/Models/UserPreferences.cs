namespace WeatherApp.Api.Models;

public sealed record UserPreferences(
    string UserId,
    string UnitSystem);

public sealed record UpdatePreferencesRequest(
    string UnitSystem);

public sealed record SaveLocationRequest(
    string Name,
    string Region,
    string Country,
    decimal Latitude,
    decimal Longitude,
    bool IsDefault = false);
