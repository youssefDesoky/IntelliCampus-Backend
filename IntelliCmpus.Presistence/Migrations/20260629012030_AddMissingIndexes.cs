using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliCampus.Presistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "GroupName",
                table: "ChatMessages",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_UserId_IsRead",
                table: "UserNotifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_Students_Level",
                table: "Students",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_Students_StudentCode",
                table: "Students",
                column: "StudentCode");

            migrationBuilder.CreateIndex(
                name: "IX_Students_StudentType",
                table: "Students",
                column: "StudentType");

            migrationBuilder.CreateIndex(
                name: "IX_StudentCourses_Semester",
                table: "StudentCourses",
                column: "Semester");

            migrationBuilder.CreateIndex(
                name: "IX_StudentCourses_Status",
                table: "StudentCourses",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_StudentCourses_StudentId",
                table: "StudentCourses",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_StudentId",
                table: "Schedules",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_Type",
                table: "Schedules",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CreatedAt",
                table: "Notifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Type",
                table: "Notifications",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_InternalMessages_ParentMessageId",
                table: "InternalMessages",
                column: "ParentMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_InternalMessages_RecipientId",
                table: "InternalMessages",
                column: "RecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_InternalMessages_SenderId",
                table: "InternalMessages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_Date",
                table: "Exams",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_ExamType",
                table: "Exams",
                column: "ExamType");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_Status",
                table: "Exams",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_CourseCode",
                table: "Courses",
                column: "CourseCode");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_Status",
                table: "Courses",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_ClassType",
                table: "Classes",
                column: "ClassType");

            migrationBuilder.CreateIndex(
                name: "IX_Bylaws_Type",
                table: "Bylaws",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Instructors_InstructorRole",
                table: "Instructors",
                column: "InstructorRole");

            migrationBuilder.CreateIndex(
                name: "IX_Grades_GradeType_Status",
                table: "Grades",
                columns: new[] { "GradeType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_GroupName",
                table: "ChatMessages",
                column: "GroupName");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_RecipientId",
                table: "ChatMessages",
                column: "RecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SenderId",
                table: "ChatMessages",
                column: "SenderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserNotifications_UserId_IsRead",
                table: "UserNotifications");

            migrationBuilder.DropIndex(
                name: "IX_Students_Level",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_StudentCode",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_StudentType",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_StudentCourses_Semester",
                table: "StudentCourses");

            migrationBuilder.DropIndex(
                name: "IX_StudentCourses_Status",
                table: "StudentCourses");

            migrationBuilder.DropIndex(
                name: "IX_StudentCourses_StudentId",
                table: "StudentCourses");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_StudentId",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_Type",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_CreatedAt",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_Type",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_InternalMessages_ParentMessageId",
                table: "InternalMessages");

            migrationBuilder.DropIndex(
                name: "IX_InternalMessages_RecipientId",
                table: "InternalMessages");

            migrationBuilder.DropIndex(
                name: "IX_InternalMessages_SenderId",
                table: "InternalMessages");

            migrationBuilder.DropIndex(
                name: "IX_Exams_Date",
                table: "Exams");

            migrationBuilder.DropIndex(
                name: "IX_Exams_ExamType",
                table: "Exams");

            migrationBuilder.DropIndex(
                name: "IX_Exams_Status",
                table: "Exams");

            migrationBuilder.DropIndex(
                name: "IX_Courses_CourseCode",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_Courses_Status",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_Classes_ClassType",
                table: "Classes");

            migrationBuilder.DropIndex(
                name: "IX_Bylaws_Type",
                table: "Bylaws");

            migrationBuilder.DropIndex(
                name: "IX_Instructors_InstructorRole",
                table: "Instructors");

            migrationBuilder.DropIndex(
                name: "IX_Grades_GradeType_Status",
                table: "Grades");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_GroupName",
                table: "ChatMessages");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_RecipientId",
                table: "ChatMessages");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_SenderId",
                table: "ChatMessages");

            migrationBuilder.AlterColumn<string>(
                name: "GroupName",
                table: "ChatMessages",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
