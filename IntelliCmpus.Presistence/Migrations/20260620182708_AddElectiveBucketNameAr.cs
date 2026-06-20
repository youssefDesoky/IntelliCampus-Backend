using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliCampus.Presistence.Migrations
{
    /// <inheritdoc />
    public partial class AddElectiveBucketNameAr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BylawCourses_BylawId",
                table: "BylawCourses");

            migrationBuilder.AddColumn<string>(
                name: "NameAr",
                table: "ElectiveBuckets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BylawCourses_BylawId_CourseId",
                table: "BylawCourses",
                columns: new[] { "BylawId", "CourseId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BylawCourses_BylawId_CourseId",
                table: "BylawCourses");

            migrationBuilder.DropColumn(
                name: "NameAr",
                table: "ElectiveBuckets");

            migrationBuilder.CreateIndex(
                name: "IX_BylawCourses_BylawId",
                table: "BylawCourses",
                column: "BylawId");
        }
    }
}
