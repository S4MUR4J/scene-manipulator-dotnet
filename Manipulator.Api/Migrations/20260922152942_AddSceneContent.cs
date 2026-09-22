using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manipulator.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSceneContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "content",
                table: "scenes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "content",
                table: "scenes");
        }
    }
}
