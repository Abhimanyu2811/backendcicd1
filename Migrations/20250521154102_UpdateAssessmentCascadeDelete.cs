using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backendapi.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAssessmentCascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Result_Assessment",
                table: "Result");

            migrationBuilder.AddColumn<Guid>(
                name: "AssessmentId2",
                table: "Question",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddForeignKey(
                name: "FK_Result_Assessment",
                table: "Result",
                column: "AssessmentId",
                principalTable: "Assessment",
                principalColumn: "AssessmentId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Result_Assessment",
                table: "Result");

            migrationBuilder.DropColumn(
                name: "AssessmentId2",
                table: "Question");

            migrationBuilder.AddForeignKey(
                name: "FK_Result_Assessment",
                table: "Result",
                column: "AssessmentId",
                principalTable: "Assessment",
                principalColumn: "AssessmentId");
        }
    }
}
