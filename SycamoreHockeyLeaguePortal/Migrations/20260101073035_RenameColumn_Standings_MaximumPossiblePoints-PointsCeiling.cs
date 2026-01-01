using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SycamoreHockeyLeaguePortal.Migrations
{
    /// <inheritdoc />
    public partial class RenameColumn_Standings_MaximumPossiblePointsPointsCeiling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MaximumPossiblePoints",
                table: "Standings",
                newName: "PointsCeiling");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PointsCeiling",
                table: "Standings",
                newName: "MaximumPossiblePoints");
        }
    }
}
