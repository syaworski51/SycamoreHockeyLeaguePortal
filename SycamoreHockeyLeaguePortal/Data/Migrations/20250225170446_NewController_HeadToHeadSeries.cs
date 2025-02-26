using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SycamoreHockeyLeaguePortal.Data.Migrations
{
    /// <inheritdoc />
    public partial class NewController_HeadToHeadSeries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HeadToHeadSeries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Team1Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Team1Wins = table.Column<int>(type: "int", nullable: false),
                    Team2Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Team2Wins = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HeadToHeadSeries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HeadToHeadSeries_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_HeadToHeadSeries_Teams_Team1Id",
                        column: x => x.Team1Id,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_HeadToHeadSeries_Teams_Team2Id",
                        column: x => x.Team2Id,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HeadToHeadSeries_SeasonId",
                table: "HeadToHeadSeries",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_HeadToHeadSeries_Team1Id",
                table: "HeadToHeadSeries",
                column: "Team1Id");

            migrationBuilder.CreateIndex(
                name: "IX_HeadToHeadSeries_Team2Id",
                table: "HeadToHeadSeries",
                column: "Team2Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HeadToHeadSeries");
        }
    }
}
