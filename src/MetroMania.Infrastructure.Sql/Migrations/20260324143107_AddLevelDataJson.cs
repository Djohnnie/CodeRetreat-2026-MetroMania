using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetroMania.Infrastructure.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AddLevelDataJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GridHeight",
                table: "Levels",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GridWidth",
                table: "Levels",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LevelDataJson",
                table: "Levels",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GridHeight",
                table: "Levels");

            migrationBuilder.DropColumn(
                name: "GridWidth",
                table: "Levels");

            migrationBuilder.DropColumn(
                name: "LevelDataJson",
                table: "Levels");
        }
    }
}