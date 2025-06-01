using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backendapi.Migrations
{
    /// <inheritdoc />
    public partial class AddMCQAssessmentModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Questions",
                table: "Assessment");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AttemptDate",
                table: "Result",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Question",
                columns: table => new
                {
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionText = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Question", x => x.QuestionId);
                    table.ForeignKey(
                        name: "FK_Question_Assessment",
                        column: x => x.AssessmentId,
                        principalTable: "Assessment",
                        principalColumn: "AssessmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Option",
                columns: table => new
                {
                    OptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Option", x => x.OptionId);
                    table.ForeignKey(
                        name: "FK_Option_Question",
                        column: x => x.QuestionId,
                        principalTable: "Question",
                        principalColumn: "QuestionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentAnswer",
                columns: table => new
                {
                    AnswerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SelectedOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentAnswer", x => x.AnswerId);
                    table.ForeignKey(
                        name: "FK_StudentAnswer_Option",
                        column: x => x.SelectedOptionId,
                        principalTable: "Option",
                        principalColumn: "OptionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentAnswer_Question",
                        column: x => x.QuestionId,
                        principalTable: "Question",
                        principalColumn: "QuestionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentAnswer_Result",
                        column: x => x.ResultId,
                        principalTable: "Result",
                        principalColumn: "ResultId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Option_QuestionId",
                table: "Option",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_Question_AssessmentId",
                table: "Question",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAnswer_QuestionId",
                table: "StudentAnswer",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAnswer_ResultId",
                table: "StudentAnswer",
                column: "ResultId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAnswer_SelectedOptionId",
                table: "StudentAnswer",
                column: "SelectedOptionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentAnswer");

            migrationBuilder.DropTable(
                name: "Option");

            migrationBuilder.DropTable(
                name: "Question");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AttemptDate",
                table: "Result",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Questions",
                table: "Assessment",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
