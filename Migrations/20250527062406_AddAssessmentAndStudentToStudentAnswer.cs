using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backendapi.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentAndStudentToStudentAnswer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add the new columns first
            migrationBuilder.AddColumn<Guid>(
                name: "AssessmentId",
                table: "StudentAnswer",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "StudentId",
                table: "StudentAnswer",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Update existing records to copy AssessmentId and UserId from Result table
            migrationBuilder.Sql(@"
                UPDATE sa
                SET sa.AssessmentId = r.AssessmentId,
                    sa.StudentId = r.UserId
                FROM StudentAnswer sa
                INNER JOIN Result r ON sa.ResultId = r.ResultId
            ");

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_StudentAnswer_AssessmentId",
                table: "StudentAnswer",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAnswer_StudentId",
                table: "StudentAnswer",
                column: "StudentId");

            // Add foreign key constraints
            migrationBuilder.AddForeignKey(
                name: "FK_StudentAnswer_Assessment",
                table: "StudentAnswer",
                column: "AssessmentId",
                principalTable: "Assessment",
                principalColumn: "AssessmentId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentAnswer_User",
                table: "StudentAnswer",
                column: "StudentId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentAnswer_Assessment",
                table: "StudentAnswer");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentAnswer_User",
                table: "StudentAnswer");

            migrationBuilder.DropIndex(
                name: "IX_StudentAnswer_AssessmentId",
                table: "StudentAnswer");

            migrationBuilder.DropIndex(
                name: "IX_StudentAnswer_StudentId",
                table: "StudentAnswer");

            migrationBuilder.DropColumn(
                name: "AssessmentId",
                table: "StudentAnswer");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "StudentAnswer");
        }
    }
}
