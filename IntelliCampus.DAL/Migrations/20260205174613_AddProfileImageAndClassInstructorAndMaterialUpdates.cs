using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliCampus.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileImageAndClassInstructorAndMaterialUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfileImage",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClassId",
                table: "StudentCourses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredAt",
                table: "StudentCourses",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "ClassId",
                table: "Materials",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Materials",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "Materials",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileUrl",
                table: "Materials",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InstructorId",
                table: "Classes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentCourses_ClassId",
                table: "StudentCourses",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Materials_ClassId",
                table: "Materials",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_InstructorId",
                table: "Classes",
                column: "InstructorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Instructors_InstructorId",
                table: "Classes",
                column: "InstructorId",
                principalTable: "Instructors",
                principalColumn: "UserId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Materials_Classes_ClassId",
                table: "Materials",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "ClassId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentCourses_Classes_ClassId",
                table: "StudentCourses",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "ClassId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Instructors_InstructorId",
                table: "Classes");

            migrationBuilder.DropForeignKey(
                name: "FK_Materials_Classes_ClassId",
                table: "Materials");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentCourses_Classes_ClassId",
                table: "StudentCourses");

            migrationBuilder.DropIndex(
                name: "IX_StudentCourses_ClassId",
                table: "StudentCourses");

            migrationBuilder.DropIndex(
                name: "IX_Materials_ClassId",
                table: "Materials");

            migrationBuilder.DropIndex(
                name: "IX_Classes_InstructorId",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "ProfileImage",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ClassId",
                table: "StudentCourses");

            migrationBuilder.DropColumn(
                name: "RegisteredAt",
                table: "StudentCourses");

            migrationBuilder.DropColumn(
                name: "ClassId",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "FileUrl",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "InstructorId",
                table: "Classes");
        }
    }
}
