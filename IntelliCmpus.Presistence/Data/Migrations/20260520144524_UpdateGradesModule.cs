using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliCampus.Presistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGradesModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentAssignments_Instructors_GradedByInstructorId",
                table: "StudentAssignments");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Grades",
                newName: "Status");

            migrationBuilder.AlterColumn<decimal>(
                name: "Score",
                table: "Grades",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AddColumn<string>(
                name: "GradeType",
                table: "Grades",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "GradedAt",
                table: "Grades",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "MaxScore",
                table: "Grades",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Grades",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Grades",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Weight",
                table: "Grades",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "GradeComplaints",
                columns: table => new
                {
                    ComplaintId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GradeId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    ComplaintType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeComplaints", x => x.ComplaintId);
                    table.ForeignKey(
                        name: "FK_GradeComplaints_Grades_GradeId",
                        column: x => x.GradeId,
                        principalTable: "Grades",
                        principalColumn: "GradeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GradeComplaints_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GradeComplaints_GradeId",
                table: "GradeComplaints",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeComplaints_StudentId",
                table: "GradeComplaints",
                column: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentAssignments_Instructors_GradedByInstructorId",
                table: "StudentAssignments",
                column: "GradedByInstructorId",
                principalTable: "Instructors",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentAssignments_Instructors_GradedByInstructorId",
                table: "StudentAssignments");

            migrationBuilder.DropTable(
                name: "GradeComplaints");

            migrationBuilder.DropColumn(
                name: "GradeType",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "GradedAt",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "MaxScore",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "Grades");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Grades",
                newName: "Type");

            migrationBuilder.AlterColumn<decimal>(
                name: "Score",
                table: "Grades",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldPrecision: 10,
                oldScale: 2);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentAssignments_Instructors_GradedByInstructorId",
                table: "StudentAssignments",
                column: "GradedByInstructorId",
                principalTable: "Instructors",
                principalColumn: "UserId",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
