using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SycamoreHockeyLeaguePortal.Migrations
{
    /// <inheritdoc />
    public partial class DropColumns_Standings3210_DivisionIdDivisionRankingPlayoffRanking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Standings_3210_Divisions_DivisionId",
                table: "Standings_3210");

            migrationBuilder.DropIndex(
                name: "IX_Standings_3210_DivisionId",
                table: "Standings_3210");

            migrationBuilder.DropColumn(
                name: "DivisionId",
                table: "Standings_3210");

            migrationBuilder.DropColumn(
                name: "DivisionRanking",
                table: "Standings_3210");

            migrationBuilder.DropColumn(
                name: "PlayoffRanking",
                table: "Standings_3210");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DivisionId",
                table: "Standings_3210",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DivisionRanking",
                table: "Standings_3210",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PlayoffRanking",
                table: "Standings_3210",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Standings_3210_DivisionId",
                table: "Standings_3210",
                column: "DivisionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Standings_3210_Divisions_DivisionId",
                table: "Standings_3210",
                column: "DivisionId",
                principalTable: "Divisions",
                principalColumn: "Id");
        }
    }
}
