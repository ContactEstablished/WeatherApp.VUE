using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using WeatherApp.Api.Services;

namespace WeatherApp.Api.Tests;

public sealed class OpenWeatherForecastServiceTests
{
    [Fact]
    public async Task GetDashboardAsyncMapsOpenWeatherResponseUsingRequestedUnits()
    {
        var handler = new StubHttpMessageHandler()
            .RespondJson("/geo/1.0/direct", """
                [
                  {
                    "name": "San Francisco",
                    "lat": 37.7749,
                    "lon": -122.4194,
                    "country": "US",
                    "state": "California"
                  }
                ]
                """)
            .RespondJson("/data/3.0/onecall", OneCallJson(temp: 64.4, feelsLike: 63.2, windSpeed: 11.4, visibility: 16093));
        var service = CreateService(handler);

        var dashboard = await service.GetDashboardAsync("San Francisco, California", WeatherUnits.Imperial, CancellationToken.None);

        Assert.Equal(WeatherUnits.Imperial, dashboard.UnitSystem);
        Assert.Equal("F", dashboard.TemperatureUnit);
        Assert.Equal("mph", dashboard.WindUnit);
        Assert.Equal("San Francisco, California", dashboard.Current.Location);
        Assert.Equal("Clear Sky", dashboard.Current.Condition);
        Assert.Equal(64, dashboard.Current.Temperature);
        Assert.Equal(63, dashboard.Current.FeelsLike);
        Assert.Equal(56, dashboard.Current.Low);
        Assert.Equal(72, dashboard.Current.High);
        Assert.Equal("Humidity is 62% with winds from NW.", dashboard.Current.Description);

        var windMetric = Assert.Single(dashboard.Metrics, metric => metric.Key == "wind");
        Assert.Equal("11.4", windMetric.Value);
        Assert.Equal("mph", windMetric.Unit);
        Assert.Equal("NW", windMetric.Hint);

        var visibilityMetric = Assert.Single(dashboard.Metrics, metric => metric.Key == "visibility");
        Assert.Equal("10", visibilityMetric.Value);
        Assert.Equal("mi", visibilityMetric.Unit);

        Assert.Equal("/geo/1.0/direct?q=San%20Francisco%2CCA%2CUS&limit=5&appid=test-api-key", handler.Requests[0].PathAndQuery);
        Assert.Contains("units=imperial", handler.Requests[1].PathAndQuery);
    }

    [Fact]
    public async Task GetDashboardAsyncMapsMetricUnitsAndDailyForecasts()
    {
        var handler = new StubHttpMessageHandler()
            .RespondJson("/geo/1.0/direct", """
                [
                  {
                    "name": "Paris",
                    "lat": 48.8566,
                    "lon": 2.3522,
                    "country": "FR"
                  }
                ]
                """)
            .RespondJson("/data/3.0/onecall", OneCallJson(temp: 18.1, feelsLike: 17.6, windSpeed: 4.8, visibility: 10000));
        var service = CreateService(handler);

        var dashboard = await service.GetDashboardAsync("Paris", WeatherUnits.Metric, CancellationToken.None);

        Assert.Equal(WeatherUnits.Metric, dashboard.UnitSystem);
        Assert.Equal("C", dashboard.TemperatureUnit);
        Assert.Equal("m/s", dashboard.WindUnit);
        Assert.Equal("Paris, FR", dashboard.Current.Location);
        Assert.Equal(18, dashboard.Current.Temperature);
        Assert.Equal("10", Assert.Single(dashboard.Metrics, metric => metric.Key == "visibility").Value);
        Assert.Equal("km", Assert.Single(dashboard.Metrics, metric => metric.Key == "visibility").Unit);
        Assert.Collection(
            dashboard.Daily,
            day => Assert.Equal("Tue", day.Day),
            day => Assert.Equal("Wed", day.Day),
            day => Assert.Equal("Thu", day.Day),
            day => Assert.Equal("Fri", day.Day),
            day => Assert.Equal("Sat", day.Day));
        Assert.Contains("units=metric", handler.Requests[1].PathAndQuery);
    }

    [Fact]
    public async Task SearchLocationsAsyncSupportsZipCodesAndCachesResults()
    {
        var handler = new StubHttpMessageHandler()
            .RespondJson("/geo/1.0/zip", """
                {
                  "name": "Beverly Hills",
                  "lat": 34.0901,
                  "lon": -118.4065,
                  "country": "US"
                }
                """);
        var service = CreateService(handler);

        var first = await service.SearchLocationsAsync("90210", CancellationToken.None);
        var second = await service.SearchLocationsAsync("90210", CancellationToken.None);

        var location = Assert.Single(first);
        Assert.Equal("Beverly Hills", location.Name);
        Assert.Equal("US", location.Region);
        Assert.Equal(34.0901m, location.Latitude);
        Assert.Same(first, second);
        Assert.Single(handler.Requests);
        Assert.Equal("/geo/1.0/zip?zip=90210,US&appid=test-api-key", handler.Requests[0].PathAndQuery);
    }

    [Fact]
    public async Task SearchLocationsAsyncFallsBackWhenProviderFails()
    {
        var handler = new StubHttpMessageHandler()
            .RespondStatus("/geo/1.0/direct", HttpStatusCode.InternalServerError);
        var service = CreateService(handler);

        var locations = await service.SearchLocationsAsync("Seattle", CancellationToken.None);

        var location = Assert.Single(locations);
        Assert.Equal("Seattle", location.Name);
        Assert.Equal("Washington", location.Region);
    }

    [Fact]
    public async Task GetDashboardAsyncFallsBackWhenApiKeyIsMissing()
    {
        var handler = new StubHttpMessageHandler();
        var service = CreateService(handler, apiKey: null);

        var dashboard = await service.GetDashboardAsync("Austin", WeatherUnits.Metric, CancellationToken.None);

        Assert.Equal(WeatherUnits.Metric, dashboard.UnitSystem);
        Assert.Equal("Austin", dashboard.Current.Location);
        Assert.Empty(handler.Requests);
    }

    private static OpenWeatherForecastService CreateService(StubHttpMessageHandler handler, string? apiKey = "test-api-key")
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.openweathermap.org")
        };
        var options = Options.Create(new OpenWeatherOptions
        {
            ApiKey = apiKey,
            BaseUrl = "https://api.openweathermap.org"
        });

        return new OpenWeatherForecastService(httpClient, options, new MemoryCache(new MemoryCacheOptions()), new MockWeatherForecastService());
    }

    private static string OneCallJson(double temp, double feelsLike, double windSpeed, int visibility)
    {
        return $$"""
            {
              "timezone_offset": -25200,
              "current": {
                "dt": 1779248460,
                "sunrise": 1779198480,
                "sunset": 1779251040,
                "temp": {{temp}},
                "feels_like": {{feelsLike}},
                "humidity": 62,
                "wind_speed": {{windSpeed}},
                "wind_deg": 315,
                "visibility": {{visibility}},
                "weather": [
                  {
                    "main": "Clear",
                    "description": "clear sky"
                  }
                ]
              },
              "hourly": [
                { "dt": 1779248460, "temp": {{temp}}, "humidity": 62, "wind_speed": {{windSpeed}}, "visibility": {{visibility}}, "pop": 0.10, "weather": [{ "main": "Clear", "description": "clear sky" }] },
                { "dt": 1779252060, "temp": 62.1, "humidity": 64, "wind_speed": 10.0, "visibility": {{visibility}}, "pop": 0.20, "weather": [{ "main": "Clouds", "description": "few clouds" }] },
                { "dt": 1779255660, "temp": 61.4, "humidity": 66, "wind_speed": 9.5, "visibility": {{visibility}}, "pop": 0.30, "weather": [{ "main": "Clouds", "description": "scattered clouds" }] },
                { "dt": 1779259260, "temp": 60.8, "humidity": 68, "wind_speed": 9.0, "visibility": {{visibility}}, "pop": 0.40, "weather": [{ "main": "Rain", "description": "light rain" }] },
                { "dt": 1779262860, "temp": 59.6, "humidity": 70, "wind_speed": 8.5, "visibility": {{visibility}}, "pop": 0.50, "weather": [{ "main": "Rain", "description": "moderate rain" }] },
                { "dt": 1779266460, "temp": 58.9, "humidity": 72, "wind_speed": 8.0, "visibility": {{visibility}}, "pop": 0.60, "weather": [{ "main": "Clouds", "description": "overcast clouds" }] },
                { "dt": 1779270060, "temp": 58.2, "humidity": 74, "wind_speed": 7.5, "visibility": {{visibility}}, "pop": 0.70, "weather": [{ "main": "Clear", "description": "clear sky" }] }
              ],
              "daily": [
                { "dt": 1779199200, "temp": { "min": 55.5, "max": 72.4 }, "pop": 0.10, "rain": 0.2, "summary": "Clear and mild", "weather": [{ "main": "Clear", "description": "clear sky" }] },
                { "dt": 1779285600, "temp": { "min": 56.5, "max": 73.4 }, "pop": 0.20, "rain": 0.0, "summary": "Clouds increase", "weather": [{ "main": "Clouds", "description": "few clouds" }] },
                { "dt": 1779372000, "temp": { "min": 57.5, "max": 74.4 }, "pop": 0.30, "rain": 1.1, "summary": "Showers possible", "weather": [{ "main": "Rain", "description": "light rain" }] },
                { "dt": 1779458400, "temp": { "min": 58.5, "max": 75.4 }, "pop": 0.40, "rain": 0.0, "summary": "Sun returns", "weather": [{ "main": "Clear", "description": "clear sky" }] },
                { "dt": 1779544800, "temp": { "min": 59.5, "max": 76.4 }, "pop": 0.50, "snow": 0.0, "summary": "Warmer afternoon", "weather": [{ "main": "Clear", "description": "clear sky" }] }
              ]
            }
            """;
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly List<StubResponse> _responses = [];

        public List<Uri> Requests { get; } = [];

        public StubHttpMessageHandler RespondJson(string pathPrefix, string json)
        {
            _responses.Add(new StubResponse(pathPrefix, new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            }));
            return this;
        }

        public StubHttpMessageHandler RespondStatus(string pathPrefix, HttpStatusCode statusCode)
        {
            _responses.Add(new StubResponse(pathPrefix, new HttpResponseMessage(statusCode)));
            return this;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var requestUri = request.RequestUri ?? throw new InvalidOperationException("Expected request URI.");
            Requests.Add(requestUri);

            var stub = _responses.FirstOrDefault(response =>
                requestUri.AbsolutePath.StartsWith(response.PathPrefix, StringComparison.OrdinalIgnoreCase));

            if (stub is null)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            return Task.FromResult(stub.Message);
        }

        private sealed record StubResponse(string PathPrefix, HttpResponseMessage Message);
    }
}
