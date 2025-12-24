using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SycamoreHockeyLeaguePortal.Migrations
{
    /// <inheritdoc />
    public partial class AlterTable_Seasons_AddStatus_DeleteInTestModeIsLiveIsComplete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InTestMode",
                table: "Seasons");

            migrationBuilder.DropColumn(
                name: "IsComplete",
                table: "Seasons");

            migrationBuilder.DropColumn(
                name: "IsLive",
                table: "Seasons");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Seasons",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Seasons");

            migrationBuilder.AddColumn<bool>(
                name: "InTestMode",
                table: "Seasons",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsComplete",
                table: "Seasons",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLive",
                table: "Seasons",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
