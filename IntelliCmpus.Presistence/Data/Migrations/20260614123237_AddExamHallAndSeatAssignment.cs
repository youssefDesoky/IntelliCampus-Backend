using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliCampus.Presistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExamHallAndSeatAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExamHalls",
                columns: table => new
                {
                    ExamHallId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HallName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HallNameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Capacity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamHalls", x => x.ExamHallId);
                });

            migrationBuilder.CreateTable(
                name: "ExamSeatAssignments",
                columns: table => new
                {
                    ExamSeatAssignmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    ExamHallId = table.Column<int>(type: "int", nullable: false),
                    SeatNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamSeatAssignments", x => x.ExamSeatAssignmentId);
                    table.ForeignKey(
                        name: "FK_ExamSeatAssignments_ExamHalls_ExamHallId",
                        column: x => x.ExamHallId,
                        principalTable: "ExamHalls",
                        principalColumn: "ExamHallId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamSeatAssignments_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "ExamId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamSeatAssignments_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamSeatAssignments_ExamHallId",
                table: "ExamSeatAssignments",
                column: "ExamHallId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSeatAssignments_ExamId_StudentId",
                table: "ExamSeatAssignments",
                columns: new[] { "ExamId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamSeatAssignments_StudentId",
                table: "ExamSeatAssignments",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamSeatAssignments");

            migrationBuilder.DropTable(
                name: "ExamHalls");
        }
    }
}
