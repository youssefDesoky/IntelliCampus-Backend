using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliCampus.Presistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFacultyToBylaw : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FacultyId",
                table: "Bylaws",
                type: "int",
                nullable: true);

            // Backfill existing rows with the first faculty's ID
            migrationBuilder.Sql(@"
                UPDATE Bylaws
                SET FacultyId = (SELECT TOP 1 FacultyId FROM Faculties ORDER BY FacultyId)
                WHERE FacultyId IS NULL
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Bylaws_FacultyId",
                table: "Bylaws",
                column: "FacultyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bylaws_Faculties_FacultyId",
                table: "Bylaws",
                column: "FacultyId",
                principalTable: "Faculties",
                principalColumn: "FacultyId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bylaws_Faculties_FacultyId",
                table: "Bylaws");

            migrationBuilder.DropIndex(
                name: "IX_Bylaws_FacultyId",
                table: "Bylaws");

            migrationBuilder.DropColumn(
                name: "FacultyId",
                table: "Bylaws");
        }
    }
}
