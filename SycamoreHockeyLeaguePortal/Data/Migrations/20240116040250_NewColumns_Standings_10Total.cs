using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SycamoreHockeyLeaguePortal.Data.Migrations
{
    /// <inheritdoc />
    public partial class NewColumns_Standings_10Total : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GamesPlayed",
                table: "Standings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GamesPlayedVsConference",
                table: "Standings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GamesPlayedVsDivision",
                table: "Standings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InterConfGamesPlayed",
                table: "Standings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "InterConfWinPct",
                table: "Standings",
                type: "decimal(4,1)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Points",
                table: "Standings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PointsPct",
                table: "Standings",
                type: "decimal(4,1)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WinPct",
                table: "Standings",
                type: "decimal(4,1)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WinPctVsConference",
                table: "Standings",
                type: "decimal(4,1)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WinPctVsDivision",
                table: "Standings",
                type: "decimal(4,1)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GamesPlayed",
                table: "Standings");

            migrationBuilder.DropColumn(
                name: "GamesPlayedVsConference",
                table: "Standings");

            migrationBuilder.DropColumn(
                name: "GamesPlayedVsDivision",
                table: "Standings");

            migrationBuilder.DropColumn(
                name: "InterConfGamesPlayed",
                table: "Standings");

            migrationBuilder.DropColumn(
                name: "InterConfWinPct",
                table: "Standings");

            migrationBuilder.DropColumn(
                name: "Points",
                table: "Standings");

            migrationBuilder.DropColumn(
                name: "PointsPct",
                table: "Standings");

            migrationBuilder.DropColumn(
                name: "WinPct",
                table: "Standings");

            migrationBuilder.DropColumn(
                name: "WinPctVsConference",
                table: "Standings");

            migrationBuilder.DropColumn(
                name: "WinPctVsDivision",
                table: "Standings");
        }
    }
}
