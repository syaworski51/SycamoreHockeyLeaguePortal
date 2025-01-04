using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SycamoreHockeyLeaguePortal.Data.Migrations
{
    /// <inheritdoc />
    public partial class NewColumn_Schedule_NextGame : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "NextGameId",
                table: "Standings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Standings_NextGameId",
                table: "Standings",
                column: "NextGameId");

            migrationBuilder.AddForeignKey(
                name: "FK_Standings_Schedule_NextGameId",
                table: "Standings",
                column: "NextGameId",
                principalTable: "Schedule",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Standings_Schedule_NextGameId",
                table: "Standings");

            migrationBuilder.DropIndex(
                name: "IX_Standings_NextGameId",
                table: "Standings");

            migrationBuilder.DropColumn(
                name: "NextGameId",
                table: "Standings");
        }
    }
}
