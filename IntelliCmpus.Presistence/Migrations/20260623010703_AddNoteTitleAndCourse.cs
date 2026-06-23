using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliCampus.Presistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNoteTitleAndCourse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Notes",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSDATETIME()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<int>(
                name: "CourseId",
                table: "Notes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaterialFolderId",
                table: "Notes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "Notes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Notes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_CourseId",
                table: "Notes",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_MaterialFolderId",
                table: "Notes",
                column: "MaterialFolderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notes_Courses_CourseId",
                table: "Notes",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "CourseId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notes_MaterialFolders_MaterialFolderId",
                table: "Notes",
                column: "MaterialFolderId",
                principalTable: "MaterialFolders",
                principalColumn: "MaterialFolderId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notes_Courses_CourseId",
                table: "Notes");

            migrationBuilder.DropForeignKey(
                name: "FK_Notes_MaterialFolders_MaterialFolderId",
                table: "Notes");

            migrationBuilder.DropIndex(
                name: "IX_Notes_CourseId",
                table: "Notes");

            migrationBuilder.DropIndex(
                name: "IX_Notes_MaterialFolderId",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "CourseId",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "MaterialFolderId",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Notes");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Notes",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "SYSDATETIME()");
        }
    }
}
