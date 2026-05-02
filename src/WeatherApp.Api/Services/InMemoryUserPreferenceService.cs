using System.Collections.Concurrent;
using WeatherApp.Api.Models;

namespace WeatherApp.Api.Services;

public sealed class InMemoryUserPreferenceService : IUserPreferenceService
{
    private readonly ConcurrentDictionary<string, UserPreferences> _preferences = new();
    private readonly ConcurrentDictionary<string, List<LocationSuggestion>> _locations = new();
    private int _nextLocationId;

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
                if (request.IsDefault)
                {
                    for (var index = 0; index < locations.Count; index++)
                    {
                        var saved = locations[index];
                        locations[index] = saved with { IsDefault = false };
                    }
                }

                locations.Add(new LocationSuggestion(
                    request.Name,
                    request.Region,
                    request.Country,
                    request.Latitude,
                    request.Longitude,
                    Interlocked.Increment(ref _nextLocationId),
                    request.IsDefault));
            }
        }

        return Task.CompletedTask;
    }

    public Task DeleteLocationAsync(string userId, int locationId, CancellationToken cancellationToken)
    {
        var locations = _locations.GetOrAdd(userId, _ => []);
        lock (locations)
        {
            locations.RemoveAll(location => location.Id == locationId);
        }

        return Task.CompletedTask;
    }

    public Task SetDefaultLocationAsync(string userId, int locationId, CancellationToken cancellationToken)
    {
        var locations = _locations.GetOrAdd(userId, _ => []);
        lock (locations)
        {
            for (var index = 0; index < locations.Count; index++)
            {
                var saved = locations[index];
                locations[index] = saved with { IsDefault = saved.Id == locationId };
            }
        }

        return Task.CompletedTask;
    }
}
