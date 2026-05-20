using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliCampus.Presistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceExcuse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "EndTime",
                table: "Sessions",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SessionType",
                table: "Sessions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "StartTime",
                table: "Sessions",
                type: "time",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AttendanceExcuses",
                columns: table => new
                {
                    ExcuseId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DocumentPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocumentOriginalName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocumentContentType = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceExcuses", x => x.ExcuseId);
                    table.ForeignKey(
                        name: "FK_AttendanceExcuses_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttendanceExcuses_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceExcuses_SessionId",
                table: "AttendanceExcuses",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceExcuses_StudentId",
                table: "AttendanceExcuses",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceExcuses");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "SessionType",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "Sessions");
        }
    }
}
