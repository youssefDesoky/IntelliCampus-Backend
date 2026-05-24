using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliCampus.Presistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AttendanceQrModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QrTokens",
                columns: table => new
                {
                    QrTokenId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QrTokens", x => x.QrTokenId);
                    table.ForeignKey(
                        name: "FK_QrTokens_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QrTokens_StudentId_GeneratedAt",
                table: "QrTokens",
                columns: new[] { "StudentId", "GeneratedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_QrTokens_Token",
                table: "QrTokens",
                column: "Token");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QrTokens");
        }
    }
}
