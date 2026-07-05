using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliCampus.Presistence.Migrations
{
    /// <inheritdoc />
    public partial class MigrateExamHallToRoom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExamSeatAssignments_ExamHalls_ExamHallId",
                table: "ExamSeatAssignments");

            migrationBuilder.Sql("DELETE FROM ExamSeatAssignments");

            migrationBuilder.DropTable(
                name: "ExamHalls");

            migrationBuilder.RenameColumn(
                name: "ExamHallId",
                table: "ExamSeatAssignments",
                newName: "RoomId");

            migrationBuilder.RenameIndex(
                name: "IX_ExamSeatAssignments_ExamHallId",
                table: "ExamSeatAssignments",
                newName: "IX_ExamSeatAssignments_RoomId");

            migrationBuilder.AddColumn<bool>(
                name: "IsExamHall",
                table: "Rooms",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamSeatAssignments_Rooms_RoomId",
                table: "ExamSeatAssignments",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "RoomId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExamSeatAssignments_Rooms_RoomId",
                table: "ExamSeatAssignments");

            migrationBuilder.DropColumn(
                name: "IsExamHall",
                table: "Rooms");

            migrationBuilder.RenameColumn(
                name: "RoomId",
                table: "ExamSeatAssignments",
                newName: "ExamHallId");

            migrationBuilder.RenameIndex(
                name: "IX_ExamSeatAssignments_RoomId",
                table: "ExamSeatAssignments",
                newName: "IX_ExamSeatAssignments_ExamHallId");

            migrationBuilder.CreateTable(
                name: "ExamHalls",
                columns: table => new
                {
                    ExamHallId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    HallName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HallNameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamHalls", x => x.ExamHallId);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_ExamSeatAssignments_ExamHalls_ExamHallId",
                table: "ExamSeatAssignments",
                column: "ExamHallId",
                principalTable: "ExamHalls",
                principalColumn: "ExamHallId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
