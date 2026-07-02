using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliCampus.Presistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupCodeAr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "Specialization",
                table: "Instructors");

            migrationBuilder.DropColumn(
                name: "CourseCode",
                table: "ExamSchedules");

            migrationBuilder.DropColumn(
                name: "CourseName",
                table: "ExamSchedules");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "ExamSchedules");

            migrationBuilder.DropColumn(
                name: "Room",
                table: "Classes");

            migrationBuilder.RenameColumn(
                name: "InstructorName",
                table: "Schedules",
                newName: "TitleAr");

            migrationBuilder.AddColumn<int>(
                name: "InstructorId",
                table: "Schedules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RoomId",
                table: "Schedules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SpecializationId",
                table: "Instructors",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CourseId",
                table: "ExamSchedules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RoomId",
                table: "ExamSchedules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GroupCodeAr",
                table: "Classes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RoomId",
                table: "Classes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_InstructorId",
                table: "Schedules",
                column: "InstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_RoomId",
                table: "Schedules",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Instructors_SpecializationId",
                table: "Instructors",
                column: "SpecializationId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSchedules_CourseId",
                table: "ExamSchedules",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSchedules_RoomId",
                table: "ExamSchedules",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_RoomId",
                table: "Classes",
                column: "RoomId");

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Rooms_RoomId",
                table: "Classes",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "RoomId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamSchedules_Courses_CourseId",
                table: "ExamSchedules",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "CourseId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamSchedules_Rooms_RoomId",
                table: "ExamSchedules",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "RoomId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Instructors_Specializations_SpecializationId",
                table: "Instructors",
                column: "SpecializationId",
                principalTable: "Specializations",
                principalColumn: "SpecializationId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Instructors_InstructorId",
                table: "Schedules",
                column: "InstructorId",
                principalTable: "Instructors",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Rooms_RoomId",
                table: "Schedules",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "RoomId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Rooms_RoomId",
                table: "Classes");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamSchedules_Courses_CourseId",
                table: "ExamSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamSchedules_Rooms_RoomId",
                table: "ExamSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_Instructors_Specializations_SpecializationId",
                table: "Instructors");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Instructors_InstructorId",
                table: "Schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Rooms_RoomId",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_InstructorId",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_RoomId",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Instructors_SpecializationId",
                table: "Instructors");

            migrationBuilder.DropIndex(
                name: "IX_ExamSchedules_CourseId",
                table: "ExamSchedules");

            migrationBuilder.DropIndex(
                name: "IX_ExamSchedules_RoomId",
                table: "ExamSchedules");

            migrationBuilder.DropIndex(
                name: "IX_Classes_RoomId",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "InstructorId",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "RoomId",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "SpecializationId",
                table: "Instructors");

            migrationBuilder.DropColumn(
                name: "CourseId",
                table: "ExamSchedules");

            migrationBuilder.DropColumn(
                name: "RoomId",
                table: "ExamSchedules");

            migrationBuilder.DropColumn(
                name: "GroupCodeAr",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "RoomId",
                table: "Classes");

            migrationBuilder.RenameColumn(
                name: "TitleAr",
                table: "Schedules",
                newName: "InstructorName");

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Schedules",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Specialization",
                table: "Instructors",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CourseCode",
                table: "ExamSchedules",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CourseName",
                table: "ExamSchedules",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "ExamSchedules",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Room",
                table: "Classes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
