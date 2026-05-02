using WeatherApp.Api.Models;

namespace WeatherApp.Api.Services;

public interface IWeatherForecastService
{
    Task<WeatherDashboard> GetDashboardAsync(string location, string unitSystem, CancellationToken cancellationToken);

    Task<IReadOnlyList<LocationSuggestion>> SearchLocationsAsync(string query, CancellationToken cancellationToken);
}
