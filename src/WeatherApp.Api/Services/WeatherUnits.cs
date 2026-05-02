namespace WeatherApp.Api.Services;

public static class WeatherUnits
{
    public const string Imperial = "imperial";
    public const string Metric = "metric";

    public static string Normalize(string? unitSystem)
    {
        return string.Equals(unitSystem, Metric, StringComparison.OrdinalIgnoreCase)
            ? Metric
            : Imperial;
    }

    public static string TemperatureUnit(string unitSystem)
    {
        return Normalize(unitSystem) == Metric ? "C" : "F";
    }

    public static string WindUnit(string unitSystem)
    {
        return Normalize(unitSystem) == Metric ? "m/s" : "mph";
    }
}
