using Microsoft.EntityFrameworkCore;

namespace WeatherApp.Api.Data;

public sealed class WeatherAppDbContext(DbContextOptions<WeatherAppDbContext> options) : DbContext(options)
{
    public DbSet<UserPreferenceEntity> UserPreferences => Set<UserPreferenceEntity>();

    public DbSet<SavedLocationEntity> SavedLocations => Set<SavedLocationEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserPreferenceEntity>(entity =>
        {
            entity.HasKey(preference => preference.UserId);
            entity.Property(preference => preference.UserId).HasMaxLength(120);
            entity.Property(preference => preference.UnitSystem).HasMaxLength(16);
        });

        modelBuilder.Entity<SavedLocationEntity>(entity =>
        {
            entity.HasKey(location => location.Id);
            entity.Property(location => location.UserId).HasMaxLength(120);
            entity.Property(location => location.Name).HasMaxLength(160);
            entity.Property(location => location.Region).HasMaxLength(160);
            entity.Property(location => location.Country).HasMaxLength(80);
            entity.Property(location => location.Latitude).HasPrecision(9, 6);
            entity.Property(location => location.Longitude).HasPrecision(9, 6);
            entity.HasIndex(location => new { location.UserId, location.Name, location.Region }).IsUnique();
        });
    }
}

public sealed class UserPreferenceEntity
{
    public string UserId { get; set; } = string.Empty;

    public string UnitSystem { get; set; } = "imperial";

    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class SavedLocationEntity
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    public bool IsDefault { get; set; }

    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
