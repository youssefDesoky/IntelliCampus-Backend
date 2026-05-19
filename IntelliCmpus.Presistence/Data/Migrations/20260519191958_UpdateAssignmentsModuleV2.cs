using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliCampus.Presistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAssignmentsModuleV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileUrl",
                table: "StudentAssignments");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "StudentAssignments",
                newName: "Note");

            migrationBuilder.AddColumn<string>(
                name: "Feedback",
                table: "StudentAssignments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GradedAt",
                table: "StudentAssignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GradedByInstructorId",
                table: "StudentAssignments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FullInstructions",
                table: "Assignments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AssignmentAttachments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AssignmentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssignmentAttachments_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "AssignmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubmissionFiles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    StudentAssignmentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmissionFiles_StudentAssignments_StudentAssignmentId",
                        column: x => x.StudentAssignmentId,
                        principalTable: "StudentAssignments",
                        principalColumn: "StudentAssignmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentAssignments_GradedByInstructorId",
                table: "StudentAssignments",
                column: "GradedByInstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentAttachments_AssignmentId",
                table: "AssignmentAttachments",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionFiles_StudentAssignmentId",
                table: "SubmissionFiles",
                column: "StudentAssignmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentAssignments_Instructors_GradedByInstructorId",
                table: "StudentAssignments",
                column: "GradedByInstructorId",
                principalTable: "Instructors",
                principalColumn: "UserId",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentAssignments_Instructors_GradedByInstructorId",
                table: "StudentAssignments");

            migrationBuilder.DropTable(
                name: "AssignmentAttachments");

            migrationBuilder.DropTable(
                name: "SubmissionFiles");

            migrationBuilder.DropIndex(
                name: "IX_StudentAssignments_GradedByInstructorId",
                table: "StudentAssignments");

            migrationBuilder.DropColumn(
                name: "Feedback",
                table: "StudentAssignments");

            migrationBuilder.DropColumn(
                name: "GradedAt",
                table: "StudentAssignments");

            migrationBuilder.DropColumn(
                name: "GradedByInstructorId",
                table: "StudentAssignments");

            migrationBuilder.DropColumn(
                name: "FullInstructions",
                table: "Assignments");

            migrationBuilder.RenameColumn(
                name: "Note",
                table: "StudentAssignments",
                newName: "Notes");

            migrationBuilder.AddColumn<string>(
                name: "FileUrl",
                table: "StudentAssignments",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }
    }
}
