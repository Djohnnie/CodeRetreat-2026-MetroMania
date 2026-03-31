using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetroMania.Infrastructure.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmissionRenders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubmissionRenders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LevelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Day = table.Column<int>(type: "int", nullable: false),
                    SvgContent = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionRenders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmissionRenders_Levels_LevelId",
                        column: x => x.LevelId,
                        principalTable: "Levels",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SubmissionRenders_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionRenders_LevelId",
                table: "SubmissionRenders",
                column: "LevelId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionRenders_SubmissionId_LevelId_Day",
                table: "SubmissionRenders",
                columns: new[] { "SubmissionId", "LevelId", "Day" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubmissionRenders");
        }
    }
}
