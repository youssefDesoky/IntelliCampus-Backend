using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliCampus.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialFolders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FolderId",
                table: "Materials",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MaterialFolders",
                columns: table => new
                {
                    MaterialFolderId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    CreatedByInstructorId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialFolders", x => x.MaterialFolderId);
                    table.ForeignKey(
                        name: "FK_MaterialFolders_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "CourseId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialFolders_Instructors_CreatedByInstructorId",
                        column: x => x.CreatedByInstructorId,
                        principalTable: "Instructors",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Materials_FolderId",
                table: "Materials",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialFolders_CourseId",
                table: "MaterialFolders",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialFolders_CreatedByInstructorId",
                table: "MaterialFolders",
                column: "CreatedByInstructorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Materials_MaterialFolders_FolderId",
                table: "Materials",
                column: "FolderId",
                principalTable: "MaterialFolders",
                principalColumn: "MaterialFolderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Materials_MaterialFolders_FolderId",
                table: "Materials");

            migrationBuilder.DropTable(
                name: "MaterialFolders");

            migrationBuilder.DropIndex(
                name: "IX_Materials_FolderId",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "FolderId",
                table: "Materials");
        }
    }
}
