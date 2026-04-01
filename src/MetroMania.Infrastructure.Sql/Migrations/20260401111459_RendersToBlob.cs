using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetroMania.Infrastructure.Sql.Migrations
{
    /// <inheritdoc />
    public partial class RendersToBlob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SvgContent",
                table: "SubmissionRenders");

            migrationBuilder.AddColumn<string>(
                name: "SvgLocation",
                table: "SubmissionRenders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SvgLocation",
                table: "SubmissionRenders");

            migrationBuilder.AddColumn<string>(
                name: "SvgContent",
                table: "SubmissionRenders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
