using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetroMania.Infrastructure.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmissionMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Message",
                table: "Submissions",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Message",
                table: "Submissions");
        }
    }
}
