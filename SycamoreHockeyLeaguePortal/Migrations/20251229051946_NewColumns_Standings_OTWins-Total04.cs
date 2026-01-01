using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SycamoreHockeyLeaguePortal.Migrations
{
    /// <inheritdoc />
    public partial class NewColumns_Standings_OTWinsTotal04 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InterConfOTWins",
                table: "Standings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OTLossesInLast10Games",
                table: "Standings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OTWinsInLast10Games",
                table: "Standings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OTWinsVsConference",
                table: "Standings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InterConfOTWins",
                table: "Standings");

            migrationBuilder.DropColumn(
                name: "OTLossesInLast10Games",
                table: "Standings");

            migrationBuilder.DropColumn(
                name: "OTWinsInLast10Games",
                table: "Standings");

            migrationBuilder.DropColumn(
                name: "OTWinsVsConference",
                table: "Standings");
        }
    }
}
