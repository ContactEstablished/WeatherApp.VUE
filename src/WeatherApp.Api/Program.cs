using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WeatherApp.Api.Data;
using WeatherApp.Api.Models;
using WeatherApp.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

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

builder.Services.Configure<OpenWeatherOptions>(builder.Configuration.GetSection("OpenWeather"));
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<MockWeatherForecastService>();
builder.Services.AddHttpClient<IWeatherForecastService, OpenWeatherForecastService>((services, client) =>
{
    var options = services.GetRequiredService<IOptions<OpenWeatherOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(12);
});

var connectionString = builder.Configuration.GetConnectionString("WeatherApp");
if (string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddSingleton<IUserPreferenceService, InMemoryUserPreferenceService>();
}
else
{
    builder.Services.AddDbContext<WeatherAppDbContext>(options =>
    {
        options.UseSqlServer(connectionString);
    });
    builder.Services.AddScoped<IUserPreferenceService, SqlUserPreferenceService>();
}

var app = builder.Build();

if (!string.IsNullOrWhiteSpace(connectionString))
{
    using var scope = app.Services.CreateScope();
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<WeatherAppDbContext>();
        await dbContext.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Database migration failed. Persistence endpoints will fall back to no-op behavior until SQL Server is available.");
    }
}

app.UseCors("WeatherClient");

var weather = app.MapGroup("/api/weather");

weather.MapGet("/dashboard", async (
    string? location,
    string? unitSystem,
    string? userId,
    IWeatherForecastService forecastService,
    IUserPreferenceService preferenceService,
    CancellationToken cancellationToken) =>
{
    var normalizedUserId = NormalizeUserId(userId);
    var preferences = await preferenceService.GetPreferencesAsync(normalizedUserId, cancellationToken);
    var units = WeatherUnits.Normalize(unitSystem ?? preferences.UnitSystem);

    if (!string.Equals(preferences.UnitSystem, units, StringComparison.OrdinalIgnoreCase))
    {
        await preferenceService.UpdatePreferencesAsync(normalizedUserId, units, cancellationToken);
    }

    var dashboard = await forecastService.GetDashboardAsync(location ?? "San Francisco, CA", units, cancellationToken);
    return Results.Ok(dashboard);
});

weather.MapGet("/locations", async (
    string? query,
    IWeatherForecastService forecastService,
    CancellationToken cancellationToken) =>
{
    var locations = await forecastService.SearchLocationsAsync(query ?? "San Francisco", cancellationToken);
    return Results.Ok(locations);
});

app.MapGet("/api/users/{userId}/preferences", async (
    string userId,
    IUserPreferenceService preferenceService,
    CancellationToken cancellationToken) =>
{
    var preferences = await preferenceService.GetPreferencesAsync(NormalizeUserId(userId), cancellationToken);
    return Results.Ok(preferences);
});

app.MapPut("/api/users/{userId}/preferences", async (
    string userId,
    UpdatePreferencesRequest request,
    IUserPreferenceService preferenceService,
    CancellationToken cancellationToken) =>
{
    var preferences = await preferenceService.UpdatePreferencesAsync(
        NormalizeUserId(userId),
        request.UnitSystem,
        cancellationToken);

    return Results.Ok(preferences);
});

app.MapGet("/api/users/{userId}/locations", async (
    string userId,
    IUserPreferenceService preferenceService,
    CancellationToken cancellationToken) =>
{
    var locations = await preferenceService.GetSavedLocationsAsync(NormalizeUserId(userId), cancellationToken);
    return Results.Ok(locations);
});

app.MapPost("/api/users/{userId}/locations", async (
    string userId,
    SaveLocationRequest request,
    IUserPreferenceService preferenceService,
    CancellationToken cancellationToken) =>
{
    await preferenceService.SaveLocationAsync(NormalizeUserId(userId), request, cancellationToken);
    return Results.NoContent();
});

app.MapPut("/api/users/{userId}/locations/{locationId:int}", async (
    string userId,
    int locationId,
    UpdateSavedLocationRequest request,
    IUserPreferenceService preferenceService,
    CancellationToken cancellationToken) =>
{
    await preferenceService.UpdateLocationAsync(NormalizeUserId(userId), locationId, request, cancellationToken);
    return Results.NoContent();
});

app.MapDelete("/api/users/{userId}/locations/{locationId:int}", async (
    string userId,
    int locationId,
    IUserPreferenceService preferenceService,
    CancellationToken cancellationToken) =>
{
    await preferenceService.DeleteLocationAsync(NormalizeUserId(userId), locationId, cancellationToken);
    return Results.NoContent();
});

app.MapPut("/api/users/{userId}/locations/{locationId:int}/default", async (
    string userId,
    int locationId,
    IUserPreferenceService preferenceService,
    CancellationToken cancellationToken) =>
{
    await preferenceService.SetDefaultLocationAsync(NormalizeUserId(userId), locationId, cancellationToken);
    return Results.NoContent();
});

app.MapPut("/api/users/{userId}/locations/reorder", async (
    string userId,
    ReorderSavedLocationsRequest request,
    IUserPreferenceService preferenceService,
    CancellationToken cancellationToken) =>
{
    await preferenceService.ReorderLocationsAsync(NormalizeUserId(userId), request.LocationIds, cancellationToken);
    return Results.NoContent();
});

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "WeatherApp.Api",
    time = DateTimeOffset.UtcNow
}));

app.Run();

static string NormalizeUserId(string? userId)
{
    return string.IsNullOrWhiteSpace(userId) ? "anonymous" : userId.Trim();
}
