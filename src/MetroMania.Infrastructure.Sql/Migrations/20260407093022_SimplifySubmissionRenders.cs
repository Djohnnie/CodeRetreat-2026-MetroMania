using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetroMania.Infrastructure.Sql.Migrations
{
    /// <inheritdoc />
    public partial class SimplifySubmissionRenders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubmissionRenders_SubmissionId_LevelId_Hour",
                table: "SubmissionRenders");

            migrationBuilder.DropColumn(
                name: "SvgLocation",
                table: "SubmissionRenders");

            migrationBuilder.RenameColumn(
                name: "Hour",
                table: "SubmissionRenders",
                newName: "TotalFrames");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionRenders_SubmissionId_LevelId",
                table: "SubmissionRenders",
                columns: new[] { "SubmissionId", "LevelId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubmissionRenders_SubmissionId_LevelId",
                table: "SubmissionRenders");

            migrationBuilder.RenameColumn(
                name: "TotalFrames",
                table: "SubmissionRenders",
                newName: "Hour");

            migrationBuilder.AddColumn<string>(
                name: "SvgLocation",
                table: "SubmissionRenders",
                type: "nvarchar(500)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionRenders_SubmissionId_LevelId_Hour",
                table: "SubmissionRenders",
                columns: new[] { "SubmissionId", "LevelId", "Hour" },
                unique: true);
        }
    }
}
