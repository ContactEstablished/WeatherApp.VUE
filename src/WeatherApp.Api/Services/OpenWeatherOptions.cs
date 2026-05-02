namespace WeatherApp.Api.Services;

public sealed class OpenWeatherOptions
{
    public string BaseUrl { get; set; } = "https://api.openweathermap.org";

    public string? ApiKey { get; set; }

    public bool UseMockWhenMissing { get; set; } = true;
}
