using WeatherApp.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("WeatherClient", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddSingleton<IWeatherForecastService, MockWeatherForecastService>();

var app = builder.Build();

app.UseCors("WeatherClient");

var weather = app.MapGroup("/api/weather");

weather.MapGet("/dashboard", async (
    string? location,
    IWeatherForecastService forecastService,
    CancellationToken cancellationToken) =>
{
    var dashboard = await forecastService.GetDashboardAsync(location ?? "San Francisco, CA", cancellationToken);
    return Results.Ok(dashboard);
});

weather.MapGet("/locations", async (
    IWeatherForecastService forecastService,
    CancellationToken cancellationToken) =>
{
    var locations = await forecastService.SearchLocationsAsync(cancellationToken);
    return Results.Ok(locations);
});

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "WeatherApp.Api",
    time = DateTimeOffset.UtcNow
}));

app.Run();
