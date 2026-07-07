using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliCampus.Presistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFacultyToRoom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FacultyId",
                table: "Rooms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_FacultyId",
                table: "Rooms",
                column: "FacultyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rooms_Faculties_FacultyId",
                table: "Rooms",
                column: "FacultyId",
                principalTable: "Faculties",
                principalColumn: "FacultyId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rooms_Faculties_FacultyId",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_FacultyId",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "FacultyId",
                table: "Rooms");
        }
    }
}
