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
        return Task.FromResult<IReadOnlyList<LocationSuggestion>>(locations.OrderBy(location => location.SortOrder).ThenBy(location => location.Name).ToArray());
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
                    request.IsDefault,
                    locations.Count));
            }
        }

        return Task.CompletedTask;
    }

    public Task UpdateLocationAsync(string userId, int locationId, UpdateSavedLocationRequest request, CancellationToken cancellationToken)
    {
        var locations = _locations.GetOrAdd(userId, _ => []);
        lock (locations)
        {
            var index = locations.FindIndex(location => location.Id == locationId);
            if (index < 0)
            {
                return Task.CompletedTask;
            }

            if (request.IsDefault)
            {
                for (var defaultIndex = 0; defaultIndex < locations.Count; defaultIndex++)
                {
                    var saved = locations[defaultIndex];
                    locations[defaultIndex] = saved with { IsDefault = false };
                }
            }

            locations[index] = locations[index] with
            {
                Name = request.Name,
                Region = request.Region,
                Country = request.Country,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                IsDefault = request.IsDefault
            };
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

    public Task ReorderLocationsAsync(string userId, IReadOnlyList<int> locationIds, CancellationToken cancellationToken)
    {
        var locations = _locations.GetOrAdd(userId, _ => []);
        lock (locations)
        {
            var orderById = locationIds
                .Select((id, index) => new { id, index })
                .ToDictionary(item => item.id, item => item.index);

            for (var index = 0; index < locations.Count; index++)
            {
                var saved = locations[index];
                if (saved.Id is { } id && orderById.TryGetValue(id, out var sortOrder))
                {
                    locations[index] = saved with { SortOrder = sortOrder };
                }
            }
        }

        return Task.CompletedTask;
    }
}
