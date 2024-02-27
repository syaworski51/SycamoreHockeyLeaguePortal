using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SycamoreHockeyLeaguePortal.Data.Migrations
{
    /// <inheritdoc />
    public partial class NewColumns__Standings__DivisionGamesBehind_ConferenceGamesBehind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ConferenceGamesBehind",
                table: "Standings",
                type: "decimal(2,1)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LeagueGamesBehind",
                table: "Standings",
                type: "decimal(2,1)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConferenceGamesBehind",
                table: "Standings");

            migrationBuilder.DropColumn(
                name: "LeagueGamesBehind",
                table: "Standings");
        }
    }
}
