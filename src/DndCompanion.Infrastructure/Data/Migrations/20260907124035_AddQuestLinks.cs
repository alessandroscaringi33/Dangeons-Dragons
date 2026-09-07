using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndCompanion.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ChapterId",
                table: "Quests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LocationId",
                table: "Quests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "NpcId",
                table: "Quests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SceneId",
                table: "Quests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quests_ChapterId",
                table: "Quests",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_Quests_LocationId",
                table: "Quests",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Quests_NpcId",
                table: "Quests",
                column: "NpcId");

            migrationBuilder.CreateIndex(
                name: "IX_Quests_SceneId",
                table: "Quests",
                column: "SceneId");

            migrationBuilder.AddForeignKey(
                name: "FK_Quests_Chapters_ChapterId",
                table: "Quests",
                column: "ChapterId",
                principalTable: "Chapters",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Quests_Locations_LocationId",
                table: "Quests",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Quests_Npcs_NpcId",
                table: "Quests",
                column: "NpcId",
                principalTable: "Npcs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Quests_Scenes_SceneId",
                table: "Quests",
                column: "SceneId",
                principalTable: "Scenes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quests_Chapters_ChapterId",
                table: "Quests");

            migrationBuilder.DropForeignKey(
                name: "FK_Quests_Locations_LocationId",
                table: "Quests");

            migrationBuilder.DropForeignKey(
                name: "FK_Quests_Npcs_NpcId",
                table: "Quests");

            migrationBuilder.DropForeignKey(
                name: "FK_Quests_Scenes_SceneId",
                table: "Quests");

            migrationBuilder.DropIndex(
                name: "IX_Quests_ChapterId",
                table: "Quests");

            migrationBuilder.DropIndex(
                name: "IX_Quests_LocationId",
                table: "Quests");

            migrationBuilder.DropIndex(
                name: "IX_Quests_NpcId",
                table: "Quests");

            migrationBuilder.DropIndex(
                name: "IX_Quests_SceneId",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "ChapterId",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "NpcId",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "SceneId",
                table: "Quests");
        }
    }
}
