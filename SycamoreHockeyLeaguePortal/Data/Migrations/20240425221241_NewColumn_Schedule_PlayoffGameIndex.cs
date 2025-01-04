using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SycamoreHockeyLeaguePortal.Data.Migrations
{
    /// <inheritdoc />
    public partial class NewColumn_Schedule_PlayoffGameIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlayoffGameIndex",
                table: "Schedule",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlayoffGameIndex",
                table: "Schedule");
        }
    }
}
