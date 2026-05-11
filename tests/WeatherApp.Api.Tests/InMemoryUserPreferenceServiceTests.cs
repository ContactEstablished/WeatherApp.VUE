using WeatherApp.Api.Models;
using WeatherApp.Api.Services;

namespace WeatherApp.Api.Tests;

public sealed class InMemoryUserPreferenceServiceTests
{
    [Fact]
    public async Task PreferencesDefaultToImperialAndCanBeUpdated()
    {
        var service = new InMemoryUserPreferenceService();

        var defaults = await service.GetPreferencesAsync("anonymous", CancellationToken.None);
        var updated = await service.UpdatePreferencesAsync("anonymous", "METRIC", CancellationToken.None);
        var persisted = await service.GetPreferencesAsync("anonymous", CancellationToken.None);

        Assert.Equal(WeatherUnits.Imperial, defaults.UnitSystem);
        Assert.Equal(WeatherUnits.Metric, updated.UnitSystem);
        Assert.Equal(WeatherUnits.Metric, persisted.UnitSystem);
    }

    [Fact]
    public async Task SaveLocationAsyncPreventsDuplicatesAndTracksSingleDefault()
    {
        var service = new InMemoryUserPreferenceService();

        await service.SaveLocationAsync("anonymous", SanFrancisco(isDefault: true), CancellationToken.None);
        await service.SaveLocationAsync("anonymous", SanFrancisco(isDefault: true), CancellationToken.None);
        await service.SaveLocationAsync("anonymous", Seattle(isDefault: true), CancellationToken.None);

        var locations = await service.GetSavedLocationsAsync("anonymous", CancellationToken.None);

        Assert.Equal(2, locations.Count);
        Assert.Equal("Seattle", Assert.Single(locations, location => location.IsDefault).Name);
    }

    [Fact]
    public async Task SetDefaultAndDeleteLocationAsyncUpdateSavedLocations()
    {
        var service = new InMemoryUserPreferenceService();

        await service.SaveLocationAsync("anonymous", SanFrancisco(), CancellationToken.None);
        await service.SaveLocationAsync("anonymous", Seattle(), CancellationToken.None);

        var initial = await service.GetSavedLocationsAsync("anonymous", CancellationToken.None);
        var sanFranciscoId = initial.Single(location => location.Name == "San Francisco").Id!.Value;
        var seattleId = initial.Single(location => location.Name == "Seattle").Id!.Value;

        await service.SetDefaultLocationAsync("anonymous", sanFranciscoId, CancellationToken.None);
        await service.DeleteLocationAsync("anonymous", seattleId, CancellationToken.None);

        var locations = await service.GetSavedLocationsAsync("anonymous", CancellationToken.None);

        var saved = Assert.Single(locations);
        Assert.Equal("San Francisco", saved.Name);
        Assert.True(saved.IsDefault);
    }

    [Fact]
    public async Task UpdateLocationAsyncChangesLocationAndDefaultState()
    {
        var service = new InMemoryUserPreferenceService();

        await service.SaveLocationAsync("anonymous", SanFrancisco(), CancellationToken.None);
        var saved = Assert.Single(await service.GetSavedLocationsAsync("anonymous", CancellationToken.None));

        await service.UpdateLocationAsync(
            "anonymous",
            saved.Id!.Value,
            new UpdateSavedLocationRequest("Oakland", "California", "United States", 37.8044m, -122.2712m, true),
            CancellationToken.None);

        var updated = Assert.Single(await service.GetSavedLocationsAsync("anonymous", CancellationToken.None));
        Assert.Equal("Oakland", updated.Name);
        Assert.True(updated.IsDefault);
    }

    [Fact]
    public async Task ReorderLocationsAsyncUpdatesSavedLocationOrder()
    {
        var service = new InMemoryUserPreferenceService();

        await service.SaveLocationAsync("anonymous", SanFrancisco(), CancellationToken.None);
        await service.SaveLocationAsync("anonymous", Seattle(), CancellationToken.None);

        var initial = await service.GetSavedLocationsAsync("anonymous", CancellationToken.None);
        var sanFranciscoId = initial.Single(location => location.Name == "San Francisco").Id!.Value;
        var seattleId = initial.Single(location => location.Name == "Seattle").Id!.Value;

        await service.ReorderLocationsAsync("anonymous", [seattleId, sanFranciscoId], CancellationToken.None);

        var reordered = await service.GetSavedLocationsAsync("anonymous", CancellationToken.None);
        Assert.Collection(
            reordered,
            location => Assert.Equal("Seattle", location.Name),
            location => Assert.Equal("San Francisco", location.Name));
    }

    private static SaveLocationRequest SanFrancisco(bool isDefault = false)
    {
        return new SaveLocationRequest("San Francisco", "California", "United States", 37.7749m, -122.4194m, isDefault);
    }

    private static SaveLocationRequest Seattle(bool isDefault = false)
    {
        return new SaveLocationRequest("Seattle", "Washington", "United States", 47.6062m, -122.3321m, isDefault);
    }
}
