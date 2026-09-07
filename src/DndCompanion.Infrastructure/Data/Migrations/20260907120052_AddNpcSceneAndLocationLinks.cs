using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndCompanion.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNpcSceneAndLocationLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LocationId",
                table: "Npcs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SceneId",
                table: "Npcs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Npcs_LocationId",
                table: "Npcs",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Npcs_SceneId",
                table: "Npcs",
                column: "SceneId");

            migrationBuilder.AddForeignKey(
                name: "FK_Npcs_Locations_LocationId",
                table: "Npcs",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Npcs_Scenes_SceneId",
                table: "Npcs",
                column: "SceneId",
                principalTable: "Scenes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Npcs_Locations_LocationId",
                table: "Npcs");

            migrationBuilder.DropForeignKey(
                name: "FK_Npcs_Scenes_SceneId",
                table: "Npcs");

            migrationBuilder.DropIndex(
                name: "IX_Npcs_LocationId",
                table: "Npcs");

            migrationBuilder.DropIndex(
                name: "IX_Npcs_SceneId",
                table: "Npcs");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "Npcs");

            migrationBuilder.DropColumn(
                name: "SceneId",
                table: "Npcs");
        }
    }
}
