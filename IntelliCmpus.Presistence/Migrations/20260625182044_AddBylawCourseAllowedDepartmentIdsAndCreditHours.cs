using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliCampus.Presistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBylawCourseAllowedDepartmentIdsAndCreditHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AllowedDepartmentIds",
                table: "BylawCourses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreditHours",
                table: "BylawCourses",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowedDepartmentIds",
                table: "BylawCourses");

            migrationBuilder.DropColumn(
                name: "CreditHours",
                table: "BylawCourses");
        }
    }
}
