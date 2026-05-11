using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeatherApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedLocationSortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "SavedLocations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                WITH OrderedLocations AS (
                    SELECT
                        Id,
                        ROW_NUMBER() OVER (
                            PARTITION BY UserId
                            ORDER BY IsDefault DESC, Name ASC, Id ASC
                        ) - 1 AS NextSortOrder
                    FROM SavedLocations
                )
                UPDATE SavedLocations
                SET SortOrder = OrderedLocations.NextSortOrder
                FROM SavedLocations
                INNER JOIN OrderedLocations ON SavedLocations.Id = OrderedLocations.Id;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_SavedLocations_UserId_SortOrder",
                table: "SavedLocations",
                columns: new[] { "UserId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SavedLocations_UserId_SortOrder",
                table: "SavedLocations");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "SavedLocations");
        }
    }
}
