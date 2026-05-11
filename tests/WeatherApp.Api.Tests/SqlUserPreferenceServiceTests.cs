using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WeatherApp.Api.Data;
using WeatherApp.Api.Models;
using WeatherApp.Api.Services;

namespace WeatherApp.Api.Tests;

public sealed class SqlUserPreferenceServiceTests
{
    [Fact]
    public async Task GetPreferencesAsyncCreatesAndPersistsDefaultPreferences()
    {
        await using var database = await SqlPreferenceDatabase.CreateAsync();
        var service = database.CreateService();

        var preferences = await service.GetPreferencesAsync("anonymous", CancellationToken.None);

        Assert.Equal("anonymous", preferences.UserId);
        Assert.Equal(WeatherUnits.Imperial, preferences.UnitSystem);

        await using var verificationContext = database.CreateContext();
        var entity = await verificationContext.UserPreferences.SingleAsync();
        Assert.Equal("anonymous", entity.UserId);
        Assert.Equal(WeatherUnits.Imperial, entity.UnitSystem);
    }

    [Fact]
    public async Task UpdatePreferencesAsyncNormalizesAndUpdatesExistingPreference()
    {
        await using var database = await SqlPreferenceDatabase.CreateAsync();
        var service = database.CreateService();

        await service.GetPreferencesAsync("anonymous", CancellationToken.None);
        var updated = await service.UpdatePreferencesAsync("anonymous", "METRIC", CancellationToken.None);
        var persisted = await service.GetPreferencesAsync("anonymous", CancellationToken.None);

        Assert.Equal(WeatherUnits.Metric, updated.UnitSystem);
        Assert.Equal(WeatherUnits.Metric, persisted.UnitSystem);

        await using var verificationContext = database.CreateContext();
        Assert.Equal(WeatherUnits.Metric, (await verificationContext.UserPreferences.SingleAsync()).UnitSystem);
    }

    [Fact]
    public async Task SaveLocationAsyncPreventsDuplicatesAndTracksOneDefaultPerUser()
    {
        await using var database = await SqlPreferenceDatabase.CreateAsync();
        var service = database.CreateService();

        await service.SaveLocationAsync("anonymous", SanFrancisco(isDefault: true), CancellationToken.None);
        await service.SaveLocationAsync("anonymous", SanFrancisco(isDefault: true), CancellationToken.None);
        await service.SaveLocationAsync("anonymous", Seattle(isDefault: true), CancellationToken.None);
        await service.SaveLocationAsync("someone-else", Austin(isDefault: true), CancellationToken.None);

        var locations = await service.GetSavedLocationsAsync("anonymous", CancellationToken.None);
        var otherUserLocations = await service.GetSavedLocationsAsync("someone-else", CancellationToken.None);

        Assert.Equal(2, locations.Count);
        Assert.Equal("Seattle", Assert.Single(locations, location => location.IsDefault).Name);
        Assert.Equal("Austin", Assert.Single(otherUserLocations, location => location.IsDefault).Name);
    }

    [Fact]
    public async Task GetSavedLocationsAsyncOrdersDefaultFirstThenName()
    {
        await using var database = await SqlPreferenceDatabase.CreateAsync();
        var service = database.CreateService();

        await service.SaveLocationAsync("anonymous", Seattle(), CancellationToken.None);
        await service.SaveLocationAsync("anonymous", Austin(), CancellationToken.None);
        await service.SaveLocationAsync("anonymous", SanFrancisco(isDefault: true), CancellationToken.None);

        var locations = await service.GetSavedLocationsAsync("anonymous", CancellationToken.None);

        Assert.Collection(
            locations,
            location =>
            {
                Assert.Equal("San Francisco", location.Name);
                Assert.True(location.IsDefault);
            },
            location => Assert.Equal("Austin", location.Name),
            location => Assert.Equal("Seattle", location.Name));
    }

    [Fact]
    public async Task SetDefaultLocationAsyncOnlyUpdatesRequestedUsersLocations()
    {
        await using var database = await SqlPreferenceDatabase.CreateAsync();
        var service = database.CreateService();

        await service.SaveLocationAsync("anonymous", SanFrancisco(), CancellationToken.None);
        await service.SaveLocationAsync("anonymous", Seattle(), CancellationToken.None);
        await service.SaveLocationAsync("someone-else", Austin(isDefault: true), CancellationToken.None);

        var anonymousLocations = await service.GetSavedLocationsAsync("anonymous", CancellationToken.None);
        var seattleId = anonymousLocations.Single(location => location.Name == "Seattle").Id!.Value;

        await service.SetDefaultLocationAsync("anonymous", seattleId, CancellationToken.None);

        anonymousLocations = await service.GetSavedLocationsAsync("anonymous", CancellationToken.None);
        var otherUserLocations = await service.GetSavedLocationsAsync("someone-else", CancellationToken.None);

        Assert.Equal("Seattle", Assert.Single(anonymousLocations, location => location.IsDefault).Name);
        Assert.Equal("Austin", Assert.Single(otherUserLocations, location => location.IsDefault).Name);
    }

    [Fact]
    public async Task DeleteLocationAsyncRemovesOnlyRequestedUsersLocation()
    {
        await using var database = await SqlPreferenceDatabase.CreateAsync();
        var service = database.CreateService();

        await service.SaveLocationAsync("anonymous", SanFrancisco(), CancellationToken.None);
        await service.SaveLocationAsync("someone-else", SanFrancisco(), CancellationToken.None);

        var anonymousLocation = Assert.Single(await service.GetSavedLocationsAsync("anonymous", CancellationToken.None));

        await service.DeleteLocationAsync("anonymous", anonymousLocation.Id!.Value, CancellationToken.None);

        Assert.Empty(await service.GetSavedLocationsAsync("anonymous", CancellationToken.None));
        Assert.Equal("San Francisco", Assert.Single(await service.GetSavedLocationsAsync("someone-else", CancellationToken.None)).Name);
    }

    private static SaveLocationRequest SanFrancisco(bool isDefault = false)
    {
        return new SaveLocationRequest("San Francisco", "California", "United States", 37.7749m, -122.4194m, isDefault);
    }

    private static SaveLocationRequest Seattle(bool isDefault = false)
    {
        return new SaveLocationRequest("Seattle", "Washington", "United States", 47.6062m, -122.3321m, isDefault);
    }

    private static SaveLocationRequest Austin(bool isDefault = false)
    {
        return new SaveLocationRequest("Austin", "Texas", "United States", 30.2672m, -97.7431m, isDefault);
    }

    private sealed class SqlPreferenceDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<WeatherAppDbContext> _options;

        private SqlPreferenceDatabase(SqliteConnection connection, DbContextOptions<WeatherAppDbContext> options)
        {
            _connection = connection;
            _options = options;
        }

        public static async Task<SqlPreferenceDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<WeatherAppDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var context = new WeatherAppDbContext(options);
            await context.Database.EnsureCreatedAsync();

            return new SqlPreferenceDatabase(connection, options);
        }

        public SqlUserPreferenceService CreateService()
        {
            return new SqlUserPreferenceService(CreateContext());
        }

        public WeatherAppDbContext CreateContext()
        {
            return new WeatherAppDbContext(_options);
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
        }
    }
}
