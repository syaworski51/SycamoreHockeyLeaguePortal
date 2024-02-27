using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SycamoreHockeyLeaguePortal.Data.Migrations
{
    /// <inheritdoc />
    public partial class NewTable_ChampionsRounds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Rounds",
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
                    table.PrimaryKey("PK_Rounds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rounds_Champions_ChampionId",
                        column: x => x.ChampionId,
                        principalTable: "Champions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_Rounds_Teams_OpponentId",
                        column: x => x.OpponentId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rounds_ChampionId",
                table: "Rounds",
                column: "ChampionId");

            migrationBuilder.CreateIndex(
                name: "IX_Rounds_OpponentId",
                table: "Rounds",
                column: "OpponentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Rounds");
        }
    }
}
