using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectIsFinishedAndTechnologyCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Technologies",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsFinished",
                table: "Projects",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Technologies");

            migrationBuilder.DropColumn(
                name: "IsFinished",
                table: "Projects");
        }
    }
}
