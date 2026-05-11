using Microsoft.EntityFrameworkCore;
using WeatherApp.Api.Data;
using WeatherApp.Api.Models;

namespace WeatherApp.Api.Services;

public sealed class SqlUserPreferenceService(WeatherAppDbContext dbContext) : IUserPreferenceService
{
    public async Task<UserPreferences> GetPreferencesAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            var entity = await dbContext.UserPreferences.FindAsync([userId], cancellationToken);
            if (entity is not null)
            {
                return new UserPreferences(userId, WeatherUnits.Normalize(entity.UnitSystem));
            }

            entity = new UserPreferenceEntity
            {
                UserId = userId,
                UnitSystem = WeatherUnits.Imperial
            };

            dbContext.UserPreferences.Add(entity);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new UserPreferences(userId, entity.UnitSystem);
        }
        catch
        {
            return new UserPreferences(userId, WeatherUnits.Imperial);
        }
    }

    public async Task<UserPreferences> UpdatePreferencesAsync(string userId, string unitSystem, CancellationToken cancellationToken)
    {
        var normalized = WeatherUnits.Normalize(unitSystem);

        try
        {
            var entity = await dbContext.UserPreferences.FindAsync([userId], cancellationToken);
            if (entity is null)
            {
                entity = new UserPreferenceEntity
                {
                    UserId = userId,
                    UnitSystem = normalized
                };
                dbContext.UserPreferences.Add(entity);
            }
            else
            {
                entity.UnitSystem = normalized;
                entity.UpdatedUtc = DateTimeOffset.UtcNow;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Keep the dashboard usable even when the local SQL Server is not ready yet.
        }

        return new UserPreferences(userId, normalized);
    }

    public async Task<IReadOnlyList<LocationSuggestion>> GetSavedLocationsAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.SavedLocations
                .Where(location => location.UserId == userId)
                .OrderBy(location => location.SortOrder)
                .ThenBy(location => location.Name)
                .Select(location => new LocationSuggestion(
                    location.Name,
                    location.Region,
                    location.Country,
                    location.Latitude,
                    location.Longitude,
                    location.Id,
                    location.IsDefault,
                    location.SortOrder))
                .ToArrayAsync(cancellationToken);
        }
        catch
        {
            return [];
        }
    }

    public async Task SaveLocationAsync(string userId, SaveLocationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var exists = await dbContext.SavedLocations.AnyAsync(location =>
                location.UserId == userId &&
                location.Name == request.Name &&
                location.Region == request.Region,
                cancellationToken);

            if (exists)
            {
                return;
            }

            if (request.IsDefault)
            {
                await dbContext.SavedLocations
                    .Where(location => location.UserId == userId)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(location => location.IsDefault, false),
                        cancellationToken);
            }

            dbContext.SavedLocations.Add(new SavedLocationEntity
            {
                UserId = userId,
                Name = request.Name,
                Region = request.Region,
                Country = request.Country,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                IsDefault = request.IsDefault,
                SortOrder = await NextSortOrderAsync(userId, cancellationToken)
            });

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Persistence is opportunistic for the sample app until the DB is provisioned.
        }
    }

    public async Task UpdateLocationAsync(string userId, int locationId, UpdateSavedLocationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var entity = await dbContext.SavedLocations
                .SingleOrDefaultAsync(location => location.UserId == userId && location.Id == locationId, cancellationToken);

            if (entity is null)
            {
                return;
            }

            if (request.IsDefault)
            {
                await dbContext.SavedLocations
                    .Where(location => location.UserId == userId && location.Id != locationId)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(location => location.IsDefault, false),
                        cancellationToken);
            }

            entity.Name = request.Name;
            entity.Region = request.Region;
            entity.Country = request.Country;
            entity.Latitude = request.Latitude;
            entity.Longitude = request.Longitude;
            entity.IsDefault = request.IsDefault;

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Persistence is opportunistic for the sample app until the DB is provisioned.
        }
    }

    public async Task DeleteLocationAsync(string userId, int locationId, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SavedLocations
                .Where(location => location.UserId == userId && location.Id == locationId)
                .ExecuteDeleteAsync(cancellationToken);
        }
        catch
        {
            // Persistence is opportunistic for the sample app until the DB is provisioned.
        }
    }

    public async Task SetDefaultLocationAsync(string userId, int locationId, CancellationToken cancellationToken)
    {
        try
        {
            var exists = await dbContext.SavedLocations.AnyAsync(
                location => location.UserId == userId && location.Id == locationId,
                cancellationToken);

            if (!exists)
            {
                return;
            }

            await dbContext.SavedLocations
                .Where(location => location.UserId == userId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(location => location.IsDefault, location => location.Id == locationId),
                    cancellationToken);
        }
        catch
        {
            // Persistence is opportunistic for the sample app until the DB is provisioned.
        }
    }

    public async Task ReorderLocationsAsync(string userId, IReadOnlyList<int> locationIds, CancellationToken cancellationToken)
    {
        try
        {
            var orderById = locationIds
                .Select((id, index) => new { id, index })
                .ToDictionary(item => item.id, item => item.index);

            var locations = await dbContext.SavedLocations
                .Where(location => location.UserId == userId && orderById.Keys.Contains(location.Id))
                .ToArrayAsync(cancellationToken);

            foreach (var location in locations)
            {
                location.SortOrder = orderById[location.Id];
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Persistence is opportunistic for the sample app until the DB is provisioned.
        }
    }

    private async Task<int> NextSortOrderAsync(string userId, CancellationToken cancellationToken)
    {
        var lastSortOrder = await dbContext.SavedLocations
            .Where(location => location.UserId == userId)
            .Select(location => (int?)location.SortOrder)
            .MaxAsync(cancellationToken);

        return (lastSortOrder ?? -1) + 1;
    }
}
