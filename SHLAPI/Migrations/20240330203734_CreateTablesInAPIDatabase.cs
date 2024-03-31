using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SHLAPI.Migrations
{
    /// <inheritdoc />
    public partial class CreateTablesInAPIDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Conferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Divisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Divisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GameTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParameterValue = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValueSql: "(N'')"),
                    Index = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlayoffStatuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Index = table.Column<int>(type: "int", nullable: false),
                    ActiveFrom = table.Column<int>(type: "int", nullable: false),
                    ActiveTo = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayoffStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProgramFlags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    State = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramFlags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Seasons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    GamesPerTeam = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seasons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StandingsSortOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Parameter = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Index = table.Column<int>(type: "int", nullable: false),
                    FirstYear = table.Column<int>(type: "int", nullable: false),
                    LastYear = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StandingsSortOptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LogoPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AlternateName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrimaryColor = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValueSql: "(N'')"),
                    SecondaryColor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TertiaryColor = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlayoffRounds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Index = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayoffRounds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayoffRounds_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Alignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DivisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alignments_Conferences_ConferenceId",
                        column: x => x.ConferenceId,
                        principalTable: "Conferences",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Alignments_Divisions_DivisionId",
                        column: x => x.DivisionId,
                        principalTable: "Divisions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Alignments_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Alignments_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Champions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Champions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Champions_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Champions_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Standings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayoffStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Wins = table.Column<int>(type: "int", nullable: false),
                    Losses = table.Column<int>(type: "int", nullable: false),
                    OTLosses = table.Column<int>(type: "int", nullable: false),
                    DivisionGamesBehind = table.Column<decimal>(type: "decimal(3,1)", nullable: false),
                    GoalsFor = table.Column<int>(type: "int", nullable: false),
                    GoalsAgainst = table.Column<int>(type: "int", nullable: false),
                    Streak = table.Column<int>(type: "int", nullable: false),
                    WinsVsDivision = table.Column<int>(type: "int", nullable: false),
                    LossesVsDivision = table.Column<int>(type: "int", nullable: false),
                    OTLossesVsDivision = table.Column<int>(type: "int", nullable: false),
                    WinsVsConference = table.Column<int>(type: "int", nullable: false),
                    LossesVsConference = table.Column<int>(type: "int", nullable: false),
                    OTLossesVsConference = table.Column<int>(type: "int", nullable: false),
                    InterConfWins = table.Column<int>(type: "int", nullable: false),
                    InterConfLosses = table.Column<int>(type: "int", nullable: false),
                    InterConfOTLosses = table.Column<int>(type: "int", nullable: false),
                    RegPlusOTWins = table.Column<int>(type: "int", nullable: false),
                    RegulationWins = table.Column<int>(type: "int", nullable: false),
                    ConferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DivisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GamesPlayed = table.Column<int>(type: "int", nullable: false),
                    GamesPlayedVsConference = table.Column<int>(type: "int", nullable: false),
                    GamesPlayedVsDivision = table.Column<int>(type: "int", nullable: false),
                    InterConfGamesPlayed = table.Column<int>(type: "int", nullable: false),
                    InterConfWinPct = table.Column<decimal>(type: "decimal(4,1)", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    PointsPct = table.Column<decimal>(type: "decimal(4,1)", nullable: false),
                    WinPct = table.Column<decimal>(type: "decimal(4,1)", nullable: false),
                    WinPctVsConference = table.Column<decimal>(type: "decimal(4,1)", nullable: false),
                    WinPctVsDivision = table.Column<decimal>(type: "decimal(4,1)", nullable: false),
                    MaximumPossiblePoints = table.Column<int>(type: "int", nullable: false),
                    GoalDifferential = table.Column<int>(type: "int", nullable: false),
                    ConferenceGamesBehind = table.Column<decimal>(type: "decimal(3,1)", nullable: false),
                    LeagueGamesBehind = table.Column<decimal>(type: "decimal(3,1)", nullable: false),
                    StreakGoalDifferential = table.Column<int>(type: "int", nullable: false),
                    StreakGoalsAgainst = table.Column<int>(type: "int", nullable: false),
                    StreakGoalsFor = table.Column<int>(type: "int", nullable: false),
                    GamesPlayedInLast5Games = table.Column<int>(type: "int", nullable: false),
                    LossesInLast5Games = table.Column<int>(type: "int", nullable: false),
                    WinPctInLast5Games = table.Column<decimal>(type: "decimal(4,1)", nullable: false),
                    WinsInLast5Games = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Standings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Standings_Conferences_ConferenceId",
                        column: x => x.ConferenceId,
                        principalTable: "Conferences",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Standings_Divisions_DivisionId",
                        column: x => x.DivisionId,
                        principalTable: "Divisions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Standings_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Standings_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayoffSeries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoundId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Team1Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Team1Wins = table.Column<int>(type: "int", nullable: false),
                    Team2Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Team2Wins = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayoffSeries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayoffSeries_PlayoffRounds_RoundId",
                        column: x => x.RoundId,
                        principalTable: "PlayoffRounds",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PlayoffSeries_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PlayoffSeries_Teams_Team1Id",
                        column: x => x.Team1Id,
                        principalTable: "Teams",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PlayoffSeries_Teams_Team2Id",
                        column: x => x.Team2Id,
                        principalTable: "Teams",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Schedule",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AwayTeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AwayScore = table.Column<int>(type: "int", nullable: true),
                    HomeTeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HomeScore = table.Column<int>(type: "int", nullable: true),
                    Period = table.Column<int>(type: "int", nullable: false),
                    IsLive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GameIndex = table.Column<int>(type: "int", nullable: false),
                    PlayoffRoundId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsFinalized = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "(CONVERT([bit],(0)))")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schedule", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Schedule_PlayoffRounds_PlayoffRoundId",
                        column: x => x.PlayoffRoundId,
                        principalTable: "PlayoffRounds",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Schedule_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Schedule_Teams_AwayTeamId",
                        column: x => x.AwayTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Schedule_Teams_HomeTeamId",
                        column: x => x.HomeTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ChampionsRounds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChampionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoundIndex = table.Column<int>(type: "int", nullable: false),
                    OpponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeriesLength = table.Column<int>(type: "int", nullable: false),
                    BestOf = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChampionsRounds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChampionsRounds_Champions_ChampionId",
                        column: x => x.ChampionId,
                        principalTable: "Champions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChampionsRounds_Teams_OpponentId",
                        column: x => x.OpponentId,
                        principalTable: "Teams",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alignments_ConferenceId",
                table: "Alignments",
                column: "ConferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_Alignments_DivisionId",
                table: "Alignments",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_Alignments_SeasonId",
                table: "Alignments",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_Alignments_TeamId",
                table: "Alignments",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Champions_SeasonId",
                table: "Champions",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_Champions_TeamId",
                table: "Champions",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_ChampionsRounds_ChampionId",
                table: "ChampionsRounds",
                column: "ChampionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChampionsRounds_OpponentId",
                table: "ChampionsRounds",
                column: "OpponentId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoffRounds_SeasonId",
                table: "PlayoffRounds",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoffSeries_RoundId",
                table: "PlayoffSeries",
                column: "RoundId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoffSeries_SeasonId",
                table: "PlayoffSeries",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoffSeries_Team1Id",
                table: "PlayoffSeries",
                column: "Team1Id");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoffSeries_Team2Id",
                table: "PlayoffSeries",
                column: "Team2Id");

            migrationBuilder.CreateIndex(
                name: "IX_Schedule_AwayTeamId",
                table: "Schedule",
                column: "AwayTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedule_HomeTeamId",
                table: "Schedule",
                column: "HomeTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedule_PlayoffRoundId",
                table: "Schedule",
                column: "PlayoffRoundId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedule_SeasonId",
                table: "Schedule",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_Standings_ConferenceId",
                table: "Standings",
                column: "ConferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_Standings_DivisionId",
                table: "Standings",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_Standings_SeasonId",
                table: "Standings",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_Standings_TeamId",
                table: "Standings",
                column: "TeamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alignments");

            migrationBuilder.DropTable(
                name: "ChampionsRounds");

            migrationBuilder.DropTable(
                name: "GameTypes");

            migrationBuilder.DropTable(
                name: "PlayoffSeries");

            migrationBuilder.DropTable(
                name: "PlayoffStatuses");

            migrationBuilder.DropTable(
                name: "ProgramFlags");

            migrationBuilder.DropTable(
                name: "Schedule");

            migrationBuilder.DropTable(
                name: "Standings");

            migrationBuilder.DropTable(
                name: "StandingsSortOptions");

            migrationBuilder.DropTable(
                name: "Champions");

            migrationBuilder.DropTable(
                name: "PlayoffRounds");

            migrationBuilder.DropTable(
                name: "Conferences");

            migrationBuilder.DropTable(
                name: "Divisions");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "Seasons");
        }
    }
}
