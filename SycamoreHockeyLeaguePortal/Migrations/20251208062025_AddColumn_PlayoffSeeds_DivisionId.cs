using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SycamoreHockeyLeaguePortal.Migrations
{
    /// <inheritdoc />
    public partial class AddColumn_PlayoffSeeds_DivisionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DivisionId",
                table: "PlayoffSeeds",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayoffSeeds_DivisionId",
                table: "PlayoffSeeds",
                column: "DivisionId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayoffSeeds_Divisions_DivisionId",
                table: "PlayoffSeeds",
                column: "DivisionId",
                principalTable: "Divisions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayoffSeeds_Divisions_DivisionId",
                table: "PlayoffSeeds");

            migrationBuilder.DropIndex(
                name: "IX_PlayoffSeeds_DivisionId",
                table: "PlayoffSeeds");

            migrationBuilder.DropColumn(
                name: "DivisionId",
                table: "PlayoffSeeds");
        }
    }
}
