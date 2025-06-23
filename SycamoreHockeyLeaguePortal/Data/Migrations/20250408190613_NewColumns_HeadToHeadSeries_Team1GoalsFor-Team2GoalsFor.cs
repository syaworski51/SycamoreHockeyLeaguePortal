using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SycamoreHockeyLeaguePortal.Data.Migrations
{
    /// <inheritdoc />
    public partial class NewColumns_HeadToHeadSeries_Team1GoalsForTeam2GoalsFor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Team1GoalsFor",
                table: "HeadToHeadSeries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Team2GoalsFor",
                table: "HeadToHeadSeries",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Team1GoalsFor",
                table: "HeadToHeadSeries");

            migrationBuilder.DropColumn(
                name: "Team2GoalsFor",
                table: "HeadToHeadSeries");
        }
    }
}
