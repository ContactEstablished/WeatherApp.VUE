using WeatherApp.Api.Services;

namespace WeatherApp.Api.Tests;

public sealed class WeatherUnitsTests
{
    [Theory]
    [InlineData(null, WeatherUnits.Imperial)]
    [InlineData("", WeatherUnits.Imperial)]
    [InlineData("imperial", WeatherUnits.Imperial)]
    [InlineData("IMPERIAL", WeatherUnits.Imperial)]
    [InlineData("metric", WeatherUnits.Metric)]
    [InlineData("METRIC", WeatherUnits.Metric)]
    [InlineData("kelvin", WeatherUnits.Imperial)]
    public void NormalizeDefaultsToImperialExceptMetric(string? input, string expected)
    {
        Assert.Equal(expected, WeatherUnits.Normalize(input));
    }

    [Theory]
    [InlineData("imperial", "F", "mph")]
    [InlineData("metric", "C", "m/s")]
    [InlineData("unknown", "F", "mph")]
    public void UnitsMatchNormalizedSystem(string input, string temperatureUnit, string windUnit)
    {
        Assert.Equal(temperatureUnit, WeatherUnits.TemperatureUnit(input));
        Assert.Equal(windUnit, WeatherUnits.WindUnit(input));
    }
}
