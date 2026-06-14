using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliCampus.Presistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInstructorStatusAndOfficeHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OfficeHoursRoomId",
                table: "Instructors",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Instructors",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Instructors_OfficeHoursRoomId",
                table: "Instructors",
                column: "OfficeHoursRoomId");

            migrationBuilder.AddForeignKey(
                name: "FK_Instructors_Rooms_OfficeHoursRoomId",
                table: "Instructors",
                column: "OfficeHoursRoomId",
                principalTable: "Rooms",
                principalColumn: "RoomId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Instructors_Rooms_OfficeHoursRoomId",
                table: "Instructors");

            migrationBuilder.DropIndex(
                name: "IX_Instructors_OfficeHoursRoomId",
                table: "Instructors");

            migrationBuilder.DropColumn(
                name: "OfficeHoursRoomId",
                table: "Instructors");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Instructors");
        }
    }
}
