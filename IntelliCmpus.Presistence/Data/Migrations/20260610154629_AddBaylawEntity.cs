using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliCampus.Presistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBaylawEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BaylawId",
                table: "Students",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Baylaws",
                columns: table => new
                {
                    BaylawId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedByAdminId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Baylaws", x => x.BaylawId);
                    table.ForeignKey(
                        name: "FK_Baylaws_Admins_UploadedByAdminId",
                        column: x => x.UploadedByAdminId,
                        principalTable: "Admins",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Students_BaylawId",
                table: "Students",
                column: "BaylawId");

            migrationBuilder.CreateIndex(
                name: "IX_Baylaws_UploadedByAdminId",
                table: "Baylaws",
                column: "UploadedByAdminId");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Baylaws_BaylawId",
                table: "Students",
                column: "BaylawId",
                principalTable: "Baylaws",
                principalColumn: "BaylawId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_Baylaws_BaylawId",
                table: "Students");

            migrationBuilder.DropTable(
                name: "Baylaws");

            migrationBuilder.DropIndex(
                name: "IX_Students_BaylawId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "BaylawId",
                table: "Students");
        }
    }
}
