using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SycamoreHockeyLeaguePortal.Data.Migrations
{
    /// <inheritdoc />
    public partial class NewColumns_HeadToHeadSeries_Team1OTWinsTeam2OTWins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Team1OTWins",
                table: "HeadToHeadSeries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Team2OTWins",
                table: "HeadToHeadSeries",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Team1OTWins",
                table: "HeadToHeadSeries");

            migrationBuilder.DropColumn(
                name: "Team2OTWins",
                table: "HeadToHeadSeries");
        }
    }
}
