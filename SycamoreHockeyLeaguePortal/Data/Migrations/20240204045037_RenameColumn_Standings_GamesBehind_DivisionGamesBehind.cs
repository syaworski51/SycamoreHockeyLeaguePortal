using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SycamoreHockeyLeaguePortal.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameColumn_Standings_GamesBehind_DivisionGamesBehind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "GamesBehind",
                table: "Standings",
                newName: "DivisionGamesBehind");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DivisionGamesBehind",
                table: "Standings",
                newName: "GamesBehind");
        }
    }
}
