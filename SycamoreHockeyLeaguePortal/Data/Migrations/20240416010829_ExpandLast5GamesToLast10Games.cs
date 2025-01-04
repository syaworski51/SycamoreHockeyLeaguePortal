using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SycamoreHockeyLeaguePortal.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExpandLast5GamesToLast10Games : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WinsInLast5Games",
                table: "Standings",
                newName: "WinsInLast10Games");

            migrationBuilder.RenameColumn(
                name: "WinPctInLast5Games",
                table: "Standings",
                newName: "WinPctInLast10Games");

            migrationBuilder.RenameColumn(
                name: "LossesInLast5Games",
                table: "Standings",
                newName: "LossesInLast10Games");

            migrationBuilder.RenameColumn(
                name: "GamesPlayedInLast5Games",
                table: "Standings",
                newName: "GamesPlayedInLast10Games");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WinsInLast10Games",
                table: "Standings",
                newName: "WinsInLast5Games");

            migrationBuilder.RenameColumn(
                name: "WinPctInLast10Games",
                table: "Standings",
                newName: "WinPctInLast5Games");

            migrationBuilder.RenameColumn(
                name: "LossesInLast10Games",
                table: "Standings",
                newName: "LossesInLast5Games");

            migrationBuilder.RenameColumn(
                name: "GamesPlayedInLast10Games",
                table: "Standings",
                newName: "GamesPlayedInLast5Games");
        }
    }
}
