using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndCompanion.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSkillChecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SkillChecks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CharacterId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Skill = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Roll = table.Column<int>(type: "INTEGER", nullable: false),
                    Modifier = table.Column<int>(type: "INTEGER", nullable: false),
                    DifficultyClass = table.Column<int>(type: "INTEGER", nullable: false),
                    IsPhysicalRoll = table.Column<bool>(type: "INTEGER", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillChecks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillChecks_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SkillChecks_SessionId",
                table: "SkillChecks",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillChecks_SessionId_Timestamp",
                table: "SkillChecks",
                columns: new[] { "SessionId", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SkillChecks");
        }
    }
}
