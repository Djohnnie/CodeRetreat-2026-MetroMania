using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetroMania.Infrastructure.Sql.Migrations
{
    /// <inheritdoc />
    public partial class RenameSubmissionRenderDayToHour : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Day",
                table: "SubmissionRenders",
                newName: "Hour");

            migrationBuilder.RenameIndex(
                name: "IX_SubmissionRenders_SubmissionId_LevelId_Day",
                table: "SubmissionRenders",
                newName: "IX_SubmissionRenders_SubmissionId_LevelId_Hour");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Hour",
                table: "SubmissionRenders",
                newName: "Day");

            migrationBuilder.RenameIndex(
                name: "IX_SubmissionRenders_SubmissionId_LevelId_Hour",
                table: "SubmissionRenders",
                newName: "IX_SubmissionRenders_SubmissionId_LevelId_Day");
        }
    }
}
