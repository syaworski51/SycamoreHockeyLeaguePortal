using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SycamoreHockeyLeaguePortal.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddColumns_Seasons_PlayoffFormatPlayoffCutoff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlayoffCutoff",
                table: "Seasons",
                type: "int",
                nullable: false,
                defaultValue: 8);

            migrationBuilder.AddColumn<string>(
                name: "PlayoffFormat",
                table: "Seasons",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Conference");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlayoffCutoff",
                table: "Seasons");

            migrationBuilder.DropColumn(
                name: "PlayoffFormat",
                table: "Seasons");
        }
    }
}
