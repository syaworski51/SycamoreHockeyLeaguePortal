using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SycamoreHockeyLeaguePortal.Data.Migrations
{
    /// <inheritdoc />
    public partial class NewColumns__Standings__StreakGF_StreakGA_StreakGD : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StreakGoalDifferential",
                table: "Standings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StreakGoalsAgainst",
                table: "Standings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StreakGoalsFor",
                table: "Standings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StreakGoalDifferential",
                table: "Standings");

            migrationBuilder.DropColumn(
                name: "StreakGoalsAgainst",
                table: "Standings");

            migrationBuilder.DropColumn(
                name: "StreakGoalsFor",
                table: "Standings");
        }
    }
}
