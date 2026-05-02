using WeatherApp.Api.Models;

namespace WeatherApp.Api.Services;

public interface IWeatherForecastService
{
    Task<WeatherDashboard> GetDashboardAsync(string location, CancellationToken cancellationToken);

    Task<IReadOnlyList<LocationSuggestion>> SearchLocationsAsync(CancellationToken cancellationToken);
}
