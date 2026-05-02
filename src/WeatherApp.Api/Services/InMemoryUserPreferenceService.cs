using System.Collections.Concurrent;
using WeatherApp.Api.Models;

namespace WeatherApp.Api.Services;

public sealed class InMemoryUserPreferenceService : IUserPreferenceService
{
    private readonly ConcurrentDictionary<string, UserPreferences> _preferences = new();
    private readonly ConcurrentDictionary<string, List<LocationSuggestion>> _locations = new();

    public Task<UserPreferences> GetPreferencesAsync(string userId, CancellationToken cancellationToken)
    {
        var preferences = _preferences.GetOrAdd(userId, id => new UserPreferences(id, WeatherUnits.Imperial));
        return Task.FromResult(preferences);
    }

    public Task<UserPreferences> UpdatePreferencesAsync(string userId, string unitSystem, CancellationToken cancellationToken)
    {
        var preferences = new UserPreferences(userId, WeatherUnits.Normalize(unitSystem));
        _preferences.AddOrUpdate(userId, preferences, (_, _) => preferences);
        return Task.FromResult(preferences);
    }

    public Task<IReadOnlyList<LocationSuggestion>> GetSavedLocationsAsync(string userId, CancellationToken cancellationToken)
    {
        var locations = _locations.GetOrAdd(userId, _ => []);
        return Task.FromResult<IReadOnlyList<LocationSuggestion>>(locations);
    }

    public Task SaveLocationAsync(string userId, SaveLocationRequest request, CancellationToken cancellationToken)
    {
        var locations = _locations.GetOrAdd(userId, _ => []);
        lock (locations)
        {
            if (locations.All(location =>
                    !location.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase) ||
                    !location.Region.Equals(request.Region, StringComparison.OrdinalIgnoreCase)))
            {
                locations.Add(new LocationSuggestion(
                    request.Name,
                    request.Region,
                    request.Country,
                    request.Latitude,
                    request.Longitude));
            }
        }

        return Task.CompletedTask;
    }
}
