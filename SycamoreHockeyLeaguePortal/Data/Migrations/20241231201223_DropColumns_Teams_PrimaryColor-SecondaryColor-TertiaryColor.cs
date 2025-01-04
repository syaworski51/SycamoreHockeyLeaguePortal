using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SycamoreHockeyLeaguePortal.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropColumns_Teams_PrimaryColorSecondaryColorTertiaryColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrimaryColor",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "SecondaryColor",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "TertiaryColor",
                table: "Teams");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PrimaryColor",
                table: "Teams",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SecondaryColor",
                table: "Teams",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TertiaryColor",
                table: "Teams",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
