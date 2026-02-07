using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliCampus.DAL.Migrations
{
    /// <inheritdoc />
    public partial class ChangeMaterialFromClassToCourse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Materials_Classes_ClassId",
                table: "Materials");

            migrationBuilder.RenameColumn(
                name: "ClassId",
                table: "Materials",
                newName: "CourseId");

            migrationBuilder.RenameIndex(
                name: "IX_Materials_ClassId",
                table: "Materials",
                newName: "IX_Materials_CourseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Materials_Courses_CourseId",
                table: "Materials",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "CourseId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Materials_Courses_CourseId",
                table: "Materials");

            migrationBuilder.RenameColumn(
                name: "CourseId",
                table: "Materials",
                newName: "ClassId");

            migrationBuilder.RenameIndex(
                name: "IX_Materials_CourseId",
                table: "Materials",
                newName: "IX_Materials_ClassId");

            migrationBuilder.AddForeignKey(
                name: "FK_Materials_Classes_ClassId",
                table: "Materials",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "ClassId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
