using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliCampus.Presistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorBylawToSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CourseWorkGrade",
                table: "Bylaws");

            migrationBuilder.DropColumn(
                name: "FinalExamGrade",
                table: "Bylaws");

            migrationBuilder.DropColumn(
                name: "LevelScales",
                table: "Bylaws");

            migrationBuilder.DropColumn(
                name: "MaxCreditHoursPerSemester",
                table: "Bylaws");

            migrationBuilder.DropColumn(
                name: "MinCreditHoursForGraduationProject",
                table: "Bylaws");

            migrationBuilder.DropColumn(
                name: "MinCreditHoursPerSemester",
                table: "Bylaws");

            migrationBuilder.DropColumn(
                name: "MinHoursToChooseDepartment",
                table: "Bylaws");

            migrationBuilder.DropColumn(
                name: "MinHoursToChooseSpecialization",
                table: "Bylaws");

            migrationBuilder.DropColumn(
                name: "ProbationRegistrationLimit",
                table: "Bylaws");

            migrationBuilder.DropColumn(
                name: "ProbationThreshold",
                table: "Bylaws");

            migrationBuilder.DropColumn(
                name: "SummerMaxCreditHours",
                table: "Bylaws");

            migrationBuilder.DropColumn(
                name: "TotalHoursToCompleteDegree",
                table: "Bylaws");

            migrationBuilder.AddColumn<string>(
                name: "Settings",
                table: "Bylaws",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Settings",
                table: "Bylaws");

            migrationBuilder.AddColumn<decimal>(
                name: "CourseWorkGrade",
                table: "Bylaws",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalExamGrade",
                table: "Bylaws",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LevelScales",
                table: "Bylaws",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxCreditHoursPerSemester",
                table: "Bylaws",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinCreditHoursForGraduationProject",
                table: "Bylaws",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinCreditHoursPerSemester",
                table: "Bylaws",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinHoursToChooseDepartment",
                table: "Bylaws",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinHoursToChooseSpecialization",
                table: "Bylaws",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProbationRegistrationLimit",
                table: "Bylaws",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ProbationThreshold",
                table: "Bylaws",
                type: "decimal(4,2)",
                precision: 4,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SummerMaxCreditHours",
                table: "Bylaws",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalHoursToCompleteDegree",
                table: "Bylaws",
                type: "int",
                nullable: true);
        }
    }
}
