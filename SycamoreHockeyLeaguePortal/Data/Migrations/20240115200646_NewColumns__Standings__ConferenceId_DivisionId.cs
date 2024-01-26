using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SycamoreHockeyLeaguePortal.Data.Migrations
{
    /// <inheritdoc />
    public partial class NewColumns__Standings__ConferenceId_DivisionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "GamesBehind",
                table: "Standings",
                type: "decimal(2,1)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(3,1)");

            migrationBuilder.AddColumn<Guid>(
                name: "ConferenceId",
                table: "Standings",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "DivisionId",
                table: "Standings",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Standings_ConferenceId",
                table: "Standings",
                column: "ConferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_Standings_DivisionId",
                table: "Standings",
                column: "DivisionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Standings_Conferences_ConferenceId",
                table: "Standings",
                column: "ConferenceId",
                principalTable: "Conferences",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Standings_Divisions_DivisionId",
                table: "Standings",
                column: "DivisionId",
                principalTable: "Divisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Standings_Conferences_ConferenceId",
                table: "Standings");

            migrationBuilder.DropForeignKey(
                name: "FK_Standings_Divisions_DivisionId",
                table: "Standings");

            migrationBuilder.DropIndex(
                name: "IX_Standings_ConferenceId",
                table: "Standings");

            migrationBuilder.DropIndex(
                name: "IX_Standings_DivisionId",
                table: "Standings");

            migrationBuilder.DropColumn(
                name: "ConferenceId",
                table: "Standings");

            migrationBuilder.DropColumn(
                name: "DivisionId",
                table: "Standings");

            migrationBuilder.AlterColumn<decimal>(
                name: "GamesBehind",
                table: "Standings",
                type: "decimal(3,1)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(2,1)");
        }
    }
}
