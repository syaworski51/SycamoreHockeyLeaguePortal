using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SycamoreHockeyLeaguePortal.Data.Migrations
{
    /// <inheritdoc />
    public partial class NewColums_Teams_ConferenceIdDivisionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ConferenceId",
                table: "Teams",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DivisionId",
                table: "Teams",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teams_ConferenceId",
                table: "Teams",
                column: "ConferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_DivisionId",
                table: "Teams",
                column: "DivisionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Conferences_ConferenceId",
                table: "Teams",
                column: "ConferenceId",
                principalTable: "Conferences",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Divisions_DivisionId",
                table: "Teams",
                column: "DivisionId",
                principalTable: "Divisions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Conferences_ConferenceId",
                table: "Teams");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Divisions_DivisionId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Teams_ConferenceId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Teams_DivisionId",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "ConferenceId",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "DivisionId",
                table: "Teams");
        }
    }
}
