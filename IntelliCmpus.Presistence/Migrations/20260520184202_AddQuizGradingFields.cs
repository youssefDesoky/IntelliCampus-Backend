using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliCampus.Presistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizGradingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Feedback",
                table: "StudentQuizzes",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GradedAt",
                table: "StudentQuizzes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GradedByInstructorId",
                table: "StudentQuizzes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentQuizzes_GradedByInstructorId",
                table: "StudentQuizzes",
                column: "GradedByInstructorId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentQuizzes_Instructors_GradedByInstructorId",
                table: "StudentQuizzes",
                column: "GradedByInstructorId",
                principalTable: "Instructors",
                principalColumn: "UserId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentQuizzes_Instructors_GradedByInstructorId",
                table: "StudentQuizzes");

            migrationBuilder.DropIndex(
                name: "IX_StudentQuizzes_GradedByInstructorId",
                table: "StudentQuizzes");

            migrationBuilder.DropColumn(
                name: "Feedback",
                table: "StudentQuizzes");

            migrationBuilder.DropColumn(
                name: "GradedAt",
                table: "StudentQuizzes");

            migrationBuilder.DropColumn(
                name: "GradedByInstructorId",
                table: "StudentQuizzes");
        }
    }
}
