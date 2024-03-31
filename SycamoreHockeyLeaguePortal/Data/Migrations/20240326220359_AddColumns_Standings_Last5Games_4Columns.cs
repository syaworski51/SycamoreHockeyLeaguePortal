using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SycamoreHockeyLeaguePortal.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddColumns_Standings_Last5Games_4Columns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GamesPlayedInLast5Games",
                table: "Standings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LossesInLast5Games",
                table: "Standings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "WinPctInLast5Games",
                table: "Standings",
                type: "decimal(4,1)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "WinsInLast5Games",
                table: "Standings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GamesPlayedInLast5Games",
                table: "Standings");

            migrationBuilder.DropColumn(
                name: "LossesInLast5Games",
                table: "Standings");

            migrationBuilder.DropColumn(
                name: "WinPctInLast5Games",
                table: "Standings");

            migrationBuilder.DropColumn(
                name: "WinsInLast5Games",
                table: "Standings");
        }
    }
}
