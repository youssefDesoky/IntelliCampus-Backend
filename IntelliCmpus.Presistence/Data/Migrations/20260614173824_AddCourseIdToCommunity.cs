using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliCampus.Presistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseIdToCommunity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CourseId",
                table: "Communities",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Communities_CourseId",
                table: "Communities",
                column: "CourseId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Communities_Courses_CourseId",
                table: "Communities",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "CourseId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Communities_Courses_CourseId",
                table: "Communities");

            migrationBuilder.DropIndex(
                name: "IX_Communities_CourseId",
                table: "Communities");

            migrationBuilder.DropColumn(
                name: "CourseId",
                table: "Communities");
        }
    }
}
