using WeatherApp.Api.Services;

namespace WeatherApp.Api.Tests;

public sealed class MockWeatherForecastServiceTests
{
    [Fact]
    public async Task GetDashboardAsyncReturnsImperialValuesByDefault()
    {
        var service = new MockWeatherForecastService();

        var dashboard = await service.GetDashboardAsync(" Seattle ", "unknown", CancellationToken.None);

        Assert.Equal(WeatherUnits.Imperial, dashboard.UnitSystem);
        Assert.Equal("F", dashboard.TemperatureUnit);
        Assert.Equal("mph", dashboard.WindUnit);
        Assert.Equal("Seattle", dashboard.Current.Location);
        Assert.Equal(64, dashboard.Current.Temperature);
        Assert.Equal(11, int.Parse(dashboard.Metrics.Single(metric => metric.Key == "wind").Value));
    }

    [Fact]
    public async Task GetDashboardAsyncReturnsMetricValuesWhenRequested()
    {
        var service = new MockWeatherForecastService();

        var dashboard = await service.GetDashboardAsync("", WeatherUnits.Metric, CancellationToken.None);

        Assert.Equal(WeatherUnits.Metric, dashboard.UnitSystem);
        Assert.Equal("C", dashboard.TemperatureUnit);
        Assert.Equal("m/s", dashboard.WindUnit);
        Assert.Equal("San Francisco, CA", dashboard.Current.Location);
        Assert.Equal(18, dashboard.Current.Temperature);
        Assert.Equal("5", dashboard.Metrics.Single(metric => metric.Key == "wind").Value);
    }

    [Fact]
    public async Task SearchLocationsAsyncMatchesCityOrRegionAndReturnsAllForBlankQuery()
    {
        var service = new MockWeatherForecastService();

        var california = await service.SearchLocationsAsync("calif", CancellationToken.None);
        var blank = await service.SearchLocationsAsync(" ", CancellationToken.None);

        Assert.Collection(
            california,
            location =>
            {
                Assert.Equal("San Francisco", location.Name);
                Assert.Equal("California", location.Region);
            });
        Assert.True(blank.Count >= 5);
    }
}
