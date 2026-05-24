using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliCampus.Presistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AssignmentCourseId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Classes_ClassId",
                table: "Assignments");

            migrationBuilder.RenameColumn(
                name: "ClassId",
                table: "Assignments",
                newName: "CourseId");

            migrationBuilder.RenameIndex(
                name: "IX_Assignments_ClassId",
                table: "Assignments",
                newName: "IX_Assignments_CourseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Courses_CourseId",
                table: "Assignments",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "CourseId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Courses_CourseId",
                table: "Assignments");

            migrationBuilder.RenameColumn(
                name: "CourseId",
                table: "Assignments",
                newName: "ClassId");

            migrationBuilder.RenameIndex(
                name: "IX_Assignments_CourseId",
                table: "Assignments",
                newName: "IX_Assignments_ClassId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Classes_ClassId",
                table: "Assignments",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "ClassId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
