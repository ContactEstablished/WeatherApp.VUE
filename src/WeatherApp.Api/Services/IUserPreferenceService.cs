using WeatherApp.Api.Models;

namespace WeatherApp.Api.Services;

public interface IUserPreferenceService
{
    Task<UserPreferences> GetPreferencesAsync(string userId, CancellationToken cancellationToken);

    Task<UserPreferences> UpdatePreferencesAsync(string userId, string unitSystem, CancellationToken cancellationToken);

    Task<IReadOnlyList<LocationSuggestion>> GetSavedLocationsAsync(string userId, CancellationToken cancellationToken);

    Task SaveLocationAsync(string userId, SaveLocationRequest request, CancellationToken cancellationToken);

    Task DeleteLocationAsync(string userId, int locationId, CancellationToken cancellationToken);

    Task SetDefaultLocationAsync(string userId, int locationId, CancellationToken cancellationToken);
}
