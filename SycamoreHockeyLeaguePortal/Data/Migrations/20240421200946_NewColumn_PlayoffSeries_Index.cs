using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SycamoreHockeyLeaguePortal.Data.Migrations
{
    /// <inheritdoc />
    public partial class NewColumn_PlayoffSeries_Index : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Index",
                table: "PlayoffSeries",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Index",
                table: "PlayoffSeries");
        }
    }
}
