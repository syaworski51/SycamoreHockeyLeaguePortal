using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SycamoreHockeyLeaguePortal.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddColumns_PlayoffSeries_Seed1Seed2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Seed1",
                table: "PlayoffSeries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Seed2",
                table: "PlayoffSeries",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Seed1",
                table: "PlayoffSeries");

            migrationBuilder.DropColumn(
                name: "Seed2",
                table: "PlayoffSeries");
        }
    }
}
