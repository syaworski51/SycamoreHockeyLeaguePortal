using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SycamoreHockeyLeaguePortal.Data.Migrations
{
    /// <inheritdoc />
    public partial class NewColumn_Schedule_PlayoffRoundId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PlayoffRoundId",
                table: "Schedule",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Schedule_PlayoffRoundId",
                table: "Schedule",
                column: "PlayoffRoundId");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedule_PlayoffRounds_PlayoffRoundId",
                table: "Schedule",
                column: "PlayoffRoundId",
                principalTable: "PlayoffRounds",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedule_PlayoffRounds_PlayoffRoundId",
                table: "Schedule");

            migrationBuilder.DropIndex(
                name: "IX_Schedule_PlayoffRoundId",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "PlayoffRoundId",
                table: "Schedule");
        }
    }
}
