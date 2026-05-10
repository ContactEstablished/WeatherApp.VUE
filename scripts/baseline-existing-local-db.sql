/*
  Use this only for a local development database that was created before EF
  migrations were introduced, where UserPreferences and SavedLocations already
  exist but __EFMigrationsHistory does not contain InitialCreate.
*/

IF OBJECT_ID(N'dbo.UserPreferences', N'U') IS NULL
    THROW 51000, 'Cannot baseline migration history because dbo.UserPreferences does not exist.', 1;

IF OBJECT_ID(N'dbo.SavedLocations', N'U') IS NULL
    THROW 51000, 'Cannot baseline migration history because dbo.SavedLocations does not exist.', 1;

IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;

IF NOT EXISTS (
    SELECT 1
    FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503000627_InitialCreate'
)
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260503000627_InitialCreate', N'10.0.7');
END;
