using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliCampus.Presistence.Migrations
{
    /// <inheritdoc />
    public partial class ScopeBroadcastToFaculty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Audience",
                table: "BroadcastAnnouncements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FacultyId",
                table: "BroadcastAnnouncements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TargetStudentType",
                table: "BroadcastAnnouncements",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BroadcastAnnouncements_FacultyId_Audience",
                table: "BroadcastAnnouncements",
                columns: new[] { "FacultyId", "Audience" });

            migrationBuilder.AddForeignKey(
                name: "FK_BroadcastAnnouncements_Faculties_FacultyId",
                table: "BroadcastAnnouncements",
                column: "FacultyId",
                principalTable: "Faculties",
                principalColumn: "FacultyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BroadcastAnnouncements_Faculties_FacultyId",
                table: "BroadcastAnnouncements");

            migrationBuilder.DropIndex(
                name: "IX_BroadcastAnnouncements_FacultyId_Audience",
                table: "BroadcastAnnouncements");

            migrationBuilder.DropColumn(
                name: "Audience",
                table: "BroadcastAnnouncements");

            migrationBuilder.DropColumn(
                name: "FacultyId",
                table: "BroadcastAnnouncements");

            migrationBuilder.DropColumn(
                name: "TargetStudentType",
                table: "BroadcastAnnouncements");
        }
    }
}
