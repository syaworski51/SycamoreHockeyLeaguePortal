using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SycamoreHockeyLeaguePortal.Data.Migrations
{
    /// <inheritdoc />
    public partial class PlayoffSeries__AddColumn_MatchupConfirmed__AlterColumns_Total04_Nullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayoffSeries_Teams_Team1Id",
                table: "PlayoffSeries");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayoffSeries_Teams_Team2Id",
                table: "PlayoffSeries");

            migrationBuilder.AlterColumn<Guid>(
                name: "Team2Id",
                table: "PlayoffSeries",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "Team1Id",
                table: "PlayoffSeries",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<bool>(
                name: "MatchupConfirmed",
                table: "PlayoffSeries",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayoffSeries_Teams_Team1Id",
                table: "PlayoffSeries",
                column: "Team1Id",
                principalTable: "Teams",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayoffSeries_Teams_Team2Id",
                table: "PlayoffSeries",
                column: "Team2Id",
                principalTable: "Teams",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayoffSeries_Teams_Team1Id",
                table: "PlayoffSeries");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayoffSeries_Teams_Team2Id",
                table: "PlayoffSeries");

            migrationBuilder.DropColumn(
                name: "MatchupConfirmed",
                table: "PlayoffSeries");

            migrationBuilder.AlterColumn<Guid>(
                name: "Team2Id",
                table: "PlayoffSeries",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Team1Id",
                table: "PlayoffSeries",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayoffSeries_Teams_Team1Id",
                table: "PlayoffSeries",
                column: "Team1Id",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayoffSeries_Teams_Team2Id",
                table: "PlayoffSeries",
                column: "Team2Id",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
