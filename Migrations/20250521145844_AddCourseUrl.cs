using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backendapi.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CourseUrl",
                table: "Course",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CourseUrl",
                table: "Course");
        }
    }
}
