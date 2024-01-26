using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SycamoreHockeyLeaguePortal.Data.Migrations
{
    /// <inheritdoc />
    public partial class NewColumn_Alignments_SeasonId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SeasonId",
                table: "Alignments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Alignments_SeasonId",
                table: "Alignments",
                column: "SeasonId");

            migrationBuilder.AddForeignKey(
                name: "FK_Alignments_Seasons_SeasonId",
                table: "Alignments",
                column: "SeasonId",
                principalTable: "Seasons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Alignments_Seasons_SeasonId",
                table: "Alignments");

            migrationBuilder.DropIndex(
                name: "IX_Alignments_SeasonId",
                table: "Alignments");

            migrationBuilder.DropColumn(
                name: "SeasonId",
                table: "Alignments");
        }
    }
}
