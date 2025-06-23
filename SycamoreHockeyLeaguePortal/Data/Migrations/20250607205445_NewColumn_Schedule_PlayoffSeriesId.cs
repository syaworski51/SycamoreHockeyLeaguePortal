using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SycamoreHockeyLeaguePortal.Data.Migrations
{
    /// <inheritdoc />
    public partial class NewColumn_Schedule_PlayoffSeriesId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PlayoffSeriesId",
                table: "Schedule",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Schedule_PlayoffSeriesId",
                table: "Schedule",
                column: "PlayoffSeriesId");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedule_PlayoffSeries_PlayoffSeriesId",
                table: "Schedule",
                column: "PlayoffSeriesId",
                principalTable: "PlayoffSeries",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedule_PlayoffSeries_PlayoffSeriesId",
                table: "Schedule");

            migrationBuilder.DropIndex(
                name: "IX_Schedule_PlayoffSeriesId",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "PlayoffSeriesId",
                table: "Schedule");
        }
    }
}
