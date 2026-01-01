using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SycamoreHockeyLeaguePortal.Migrations
{
    /// <inheritdoc />
    public partial class NewTables_StandingsNHLStandingsWLStandings3210 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Standings_3210",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DivisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DivisionRanking = table.Column<int>(type: "int", nullable: false),
                    ConferenceRanking = table.Column<int>(type: "int", nullable: false),
                    PlayoffRanking = table.Column<int>(type: "int", nullable: false),
                    LeagueRanking = table.Column<int>(type: "int", nullable: false),
                    TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GamesPlayed = table.Column<int>(type: "int", nullable: false),
                    Wins = table.Column<int>(type: "int", nullable: false),
                    OTWins = table.Column<int>(type: "int", nullable: false),
                    OTLosses = table.Column<int>(type: "int", nullable: false),
                    Losses = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    PointsPct = table.Column<decimal>(type: "decimal(4,3)", nullable: false),
                    PointsCeiling = table.Column<int>(type: "int", nullable: false),
                    RegPlusOTWins = table.Column<int>(type: "int", nullable: false),
                    Streak = table.Column<int>(type: "int", nullable: false),
                    GoalsFor = table.Column<int>(type: "int", nullable: false),
                    GoalsAgainst = table.Column<int>(type: "int", nullable: false),
                    GoalDifferential = table.Column<int>(type: "int", nullable: false),
                    Last10GamesPlayed = table.Column<int>(type: "int", nullable: false),
                    Last10Wins = table.Column<int>(type: "int", nullable: false),
                    Last10OTWins = table.Column<int>(type: "int", nullable: false),
                    Last10OTLosses = table.Column<int>(type: "int", nullable: false),
                    Last10Losses = table.Column<int>(type: "int", nullable: false),
                    Last10Points = table.Column<int>(type: "int", nullable: false),
                    Last10PointsPct = table.Column<decimal>(type: "decimal(4,3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Standings_3210", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Standings_3210_Conferences_ConferenceId",
                        column: x => x.ConferenceId,
                        principalTable: "Conferences",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Standings_3210_Divisions_DivisionId",
                        column: x => x.DivisionId,
                        principalTable: "Divisions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Standings_3210_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Standings_3210_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Standings_NHL",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DivisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DivisionRanking = table.Column<int>(type: "int", nullable: false),
                    ConferenceRanking = table.Column<int>(type: "int", nullable: false),
                    PlayoffRanking = table.Column<int>(type: "int", nullable: false),
                    LeagueRanking = table.Column<int>(type: "int", nullable: false),
                    TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GamesPlayed = table.Column<int>(type: "int", nullable: false),
                    Wins = table.Column<int>(type: "int", nullable: false),
                    Losses = table.Column<int>(type: "int", nullable: false),
                    OTLosses = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    PointsPct = table.Column<decimal>(type: "decimal(4,3)", nullable: false),
                    PointsCeiling = table.Column<int>(type: "int", nullable: false),
                    RegulationWins = table.Column<int>(type: "int", nullable: false),
                    RegPlusOTWins = table.Column<int>(type: "int", nullable: false),
                    Streak = table.Column<int>(type: "int", nullable: false),
                    GoalsFor = table.Column<int>(type: "int", nullable: false),
                    GoalsAgainst = table.Column<int>(type: "int", nullable: false),
                    GoalDifferential = table.Column<int>(type: "int", nullable: false),
                    DivisionGamesPlayed = table.Column<int>(type: "int", nullable: false),
                    DivisionWins = table.Column<int>(type: "int", nullable: false),
                    DivisionLosses = table.Column<int>(type: "int", nullable: false),
                    DivisionOTLosses = table.Column<int>(type: "int", nullable: false),
                    DivisionPoints = table.Column<int>(type: "int", nullable: false),
                    DivisionPointsPct = table.Column<decimal>(type: "decimal(4,3)", nullable: false),
                    ConferenceGamesPlayed = table.Column<int>(type: "int", nullable: false),
                    ConferenceWins = table.Column<int>(type: "int", nullable: false),
                    ConferenceLosses = table.Column<int>(type: "int", nullable: false),
                    ConferenceOTLosses = table.Column<int>(type: "int", nullable: false),
                    ConferencePoints = table.Column<int>(type: "int", nullable: false),
                    ConferencePointsPct = table.Column<decimal>(type: "decimal(4,3)", nullable: false),
                    InterConfGamesPlayed = table.Column<int>(type: "int", nullable: false),
                    InterConfWins = table.Column<int>(type: "int", nullable: false),
                    InterConfLosses = table.Column<int>(type: "int", nullable: false),
                    InterConfOTLosses = table.Column<int>(type: "int", nullable: false),
                    InterConfPoints = table.Column<int>(type: "int", nullable: false),
                    InterConfPointsPct = table.Column<decimal>(type: "decimal(4,3)", nullable: false),
                    Last10GamesPlayed = table.Column<int>(type: "int", nullable: false),
                    Last10Wins = table.Column<int>(type: "int", nullable: false),
                    Last10Losses = table.Column<int>(type: "int", nullable: false),
                    Last10OTLosses = table.Column<int>(type: "int", nullable: false),
                    Last10Points = table.Column<int>(type: "int", nullable: false),
                    Last10PointsPct = table.Column<decimal>(type: "decimal(4,3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Standings_NHL", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Standings_NHL_Conferences_ConferenceId",
                        column: x => x.ConferenceId,
                        principalTable: "Conferences",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Standings_NHL_Divisions_DivisionId",
                        column: x => x.DivisionId,
                        principalTable: "Divisions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Standings_NHL_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Standings_NHL_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Standings_WL",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DivisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DivisionRanking = table.Column<int>(type: "int", nullable: false),
                    ConferenceRanking = table.Column<int>(type: "int", nullable: false),
                    PlayoffRanking = table.Column<int>(type: "int", nullable: false),
                    LeagueRanking = table.Column<int>(type: "int", nullable: false),
                    TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GamesPlayed = table.Column<int>(type: "int", nullable: false),
                    Wins = table.Column<int>(type: "int", nullable: false),
                    Losses = table.Column<int>(type: "int", nullable: false),
                    WinPct = table.Column<decimal>(type: "decimal(4,3)", nullable: false),
                    DivisionGamesBehind = table.Column<decimal>(type: "decimal(3,1)", nullable: false),
                    ConferenceGamesBehind = table.Column<decimal>(type: "decimal(3,1)", nullable: false),
                    PlayoffGamesBehind = table.Column<decimal>(type: "decimal(3,1)", nullable: false),
                    LeagueGamesBehind = table.Column<decimal>(type: "decimal(3,1)", nullable: false),
                    RegulationWins = table.Column<int>(type: "int", nullable: false),
                    RegPlusOTWins = table.Column<int>(type: "int", nullable: false),
                    Streak = table.Column<int>(type: "int", nullable: false),
                    GoalsFor = table.Column<int>(type: "int", nullable: false),
                    GoalsAgainst = table.Column<int>(type: "int", nullable: false),
                    GoalDifferential = table.Column<int>(type: "int", nullable: false),
                    DivisionGamesPlayed = table.Column<int>(type: "int", nullable: false),
                    DivisionWins = table.Column<int>(type: "int", nullable: false),
                    DivisionLosses = table.Column<int>(type: "int", nullable: false),
                    DivisionWinPct = table.Column<decimal>(type: "decimal(4,3)", nullable: false),
                    ConferenceGamesPlayed = table.Column<int>(type: "int", nullable: false),
                    ConferenceWins = table.Column<int>(type: "int", nullable: false),
                    ConferenceLosses = table.Column<int>(type: "int", nullable: false),
                    ConferenceWinPct = table.Column<decimal>(type: "decimal(4,3)", nullable: false),
                    InterConfGamesPlayed = table.Column<int>(type: "int", nullable: false),
                    InterConfWins = table.Column<int>(type: "int", nullable: false),
                    InterConfLosses = table.Column<int>(type: "int", nullable: false),
                    InterConfWinPct = table.Column<decimal>(type: "decimal(4,3)", nullable: false),
                    Last10GamesPlayed = table.Column<int>(type: "int", nullable: false),
                    Last10Wins = table.Column<int>(type: "int", nullable: false),
                    Last10Losses = table.Column<int>(type: "int", nullable: false),
                    Last10WinPct = table.Column<decimal>(type: "decimal(4,3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Standings_WL", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Standings_WL_Conferences_ConferenceId",
                        column: x => x.ConferenceId,
                        principalTable: "Conferences",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Standings_WL_Divisions_DivisionId",
                        column: x => x.DivisionId,
                        principalTable: "Divisions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Standings_WL_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Standings_WL_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Standings_3210_ConferenceId",
                table: "Standings_3210",
                column: "ConferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_Standings_3210_DivisionId",
                table: "Standings_3210",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_Standings_3210_SeasonId",
                table: "Standings_3210",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_Standings_3210_TeamId",
                table: "Standings_3210",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Standings_NHL_ConferenceId",
                table: "Standings_NHL",
                column: "ConferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_Standings_NHL_DivisionId",
                table: "Standings_NHL",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_Standings_NHL_SeasonId",
                table: "Standings_NHL",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_Standings_NHL_TeamId",
                table: "Standings_NHL",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Standings_WL_ConferenceId",
                table: "Standings_WL",
                column: "ConferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_Standings_WL_DivisionId",
                table: "Standings_WL",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_Standings_WL_SeasonId",
                table: "Standings_WL",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_Standings_WL_TeamId",
                table: "Standings_WL",
                column: "TeamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Standings_3210");

            migrationBuilder.DropTable(
                name: "Standings_NHL");

            migrationBuilder.DropTable(
                name: "Standings_WL");
        }
    }
}
