using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SycamoreHockeyLeaguePortal.Migrations
{
    /// <inheritdoc />
    public partial class NewColumns_HeadToHeadSeries_Team1PointsTeam2Points : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Team1Points",
                table: "HeadToHeadSeries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Team2Points",
                table: "HeadToHeadSeries",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Team1Points",
                table: "HeadToHeadSeries");

            migrationBuilder.DropColumn(
                name: "Team2Points",
                table: "HeadToHeadSeries");
        }
    }
}
