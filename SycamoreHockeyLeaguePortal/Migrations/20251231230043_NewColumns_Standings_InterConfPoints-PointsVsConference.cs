using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SycamoreHockeyLeaguePortal.Migrations
{
    /// <inheritdoc />
    public partial class NewColumns_Standings_InterConfPointsPointsVsConference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InterConfPoints",
                table: "Standings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PointsVsConference",
                table: "Standings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InterConfPoints",
                table: "Standings");

            migrationBuilder.DropColumn(
                name: "PointsVsConference",
                table: "Standings");
        }
    }
}
