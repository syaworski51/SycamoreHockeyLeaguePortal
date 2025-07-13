using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SycamoreHockeyLeaguePortal.Migrations
{
    /// <inheritdoc />
    public partial class NewTable_RankedPlayoffSeries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RankedPlayoffSeries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoundId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatchupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinalScore = table.Column<decimal>(type: "decimal(6,3)", nullable: false),
                    SeriesCompetitivenessScore = table.Column<decimal>(type: "decimal(3,1)", nullable: false),
                    OvertimeImpactScore = table.Column<decimal>(type: "decimal(3,1)", nullable: false),
                    OverallGoalDiffScore = table.Column<decimal>(type: "decimal(3,1)", nullable: false),
                    SeriesTiesScore = table.Column<decimal>(type: "decimal(3,1)", nullable: false),
                    SeasonRanking = table.Column<int>(type: "int", nullable: false),
                    OverallRanking = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RankedPlayoffSeries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RankedPlayoffSeries_PlayoffRounds_RoundId",
                        column: x => x.RoundId,
                        principalTable: "PlayoffRounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RankedPlayoffSeries_PlayoffSeries_MatchupId",
                        column: x => x.MatchupId,
                        principalTable: "PlayoffSeries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RankedPlayoffSeries_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RankedPlayoffSeries_MatchupId",
                table: "RankedPlayoffSeries",
                column: "MatchupId");

            migrationBuilder.CreateIndex(
                name: "IX_RankedPlayoffSeries_RoundId",
                table: "RankedPlayoffSeries",
                column: "RoundId");

            migrationBuilder.CreateIndex(
                name: "IX_RankedPlayoffSeries_SeasonId",
                table: "RankedPlayoffSeries",
                column: "SeasonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RankedPlayoffSeries");
        }
    }
}
