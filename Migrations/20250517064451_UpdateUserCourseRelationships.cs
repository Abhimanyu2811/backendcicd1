using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backendapi.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserCourseRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assessment_Course",
                table: "Assessment");

            migrationBuilder.DropForeignKey(
                name: "FK_Course_User",
                table: "Course");

            migrationBuilder.DropForeignKey(
                name: "FK_Result_User",
                table: "Result");

            migrationBuilder.CreateTable(
                name: "UserEnrollments",
                columns: table => new
                {
                    EnrolledCoursesCourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnrolledStudentsUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEnrollments", x => new { x.EnrolledCoursesCourseId, x.EnrolledStudentsUserId });
                    table.ForeignKey(
                        name: "FK_UserEnrollments_Course_EnrolledCoursesCourseId",
                        column: x => x.EnrolledCoursesCourseId,
                        principalTable: "Course",
                        principalColumn: "CourseId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserEnrollments_User_EnrolledStudentsUserId",
                        column: x => x.EnrolledStudentsUserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserEnrollments_EnrolledStudentsUserId",
                table: "UserEnrollments",
                column: "EnrolledStudentsUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assessment_Course",
                table: "Assessment",
                column: "CourseId",
                principalTable: "Course",
                principalColumn: "CourseId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Course_User",
                table: "Course",
                column: "InstructorId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Result_User",
                table: "Result",
                column: "UserId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assessment_Course",
                table: "Assessment");

            migrationBuilder.DropForeignKey(
                name: "FK_Course_User",
                table: "Course");

            migrationBuilder.DropForeignKey(
                name: "FK_Result_User",
                table: "Result");

            migrationBuilder.DropTable(
                name: "UserEnrollments");

            migrationBuilder.AddForeignKey(
                name: "FK_Assessment_Course",
                table: "Assessment",
                column: "CourseId",
                principalTable: "Course",
                principalColumn: "CourseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Course_User",
                table: "Course",
                column: "InstructorId",
                principalTable: "User",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Result_User",
                table: "Result",
                column: "UserId",
                principalTable: "User",
                principalColumn: "UserId");
        }
    }
}
