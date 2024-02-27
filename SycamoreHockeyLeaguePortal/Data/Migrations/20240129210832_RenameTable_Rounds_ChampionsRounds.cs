using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SycamoreHockeyLeaguePortal.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameTable_Rounds_ChampionsRounds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rounds_Champions_ChampionId",
                table: "Rounds");

            migrationBuilder.DropForeignKey(
                name: "FK_Rounds_Teams_OpponentId",
                table: "Rounds");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Rounds",
                table: "Rounds");

            migrationBuilder.RenameTable(
                name: "Rounds",
                newName: "ChampionsRounds");

            migrationBuilder.RenameIndex(
                name: "IX_Rounds_OpponentId",
                table: "ChampionsRounds",
                newName: "IX_ChampionsRounds_OpponentId");

            migrationBuilder.RenameIndex(
                name: "IX_Rounds_ChampionId",
                table: "ChampionsRounds",
                newName: "IX_ChampionsRounds_ChampionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChampionsRounds",
                table: "ChampionsRounds",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ChampionsRounds_Champions_ChampionId",
                table: "ChampionsRounds",
                column: "ChampionId",
                principalTable: "Champions",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_ChampionsRounds_Teams_OpponentId",
                table: "ChampionsRounds",
                column: "OpponentId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChampionsRounds_Champions_ChampionId",
                table: "ChampionsRounds");

            migrationBuilder.DropForeignKey(
                name: "FK_ChampionsRounds_Teams_OpponentId",
                table: "ChampionsRounds");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChampionsRounds",
                table: "ChampionsRounds");

            migrationBuilder.RenameTable(
                name: "ChampionsRounds",
                newName: "Rounds");

            migrationBuilder.RenameIndex(
                name: "IX_ChampionsRounds_OpponentId",
                table: "Rounds",
                newName: "IX_Rounds_OpponentId");

            migrationBuilder.RenameIndex(
                name: "IX_ChampionsRounds_ChampionId",
                table: "Rounds",
                newName: "IX_Rounds_ChampionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Rounds",
                table: "Rounds",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Rounds_Champions_ChampionId",
                table: "Rounds",
                column: "ChampionId",
                principalTable: "Champions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Rounds_Teams_OpponentId",
                table: "Rounds",
                column: "OpponentId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
