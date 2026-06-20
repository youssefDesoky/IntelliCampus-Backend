using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliCampus.Presistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInstructorReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "StudentId",
                table: "Reminders",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "InstructorId",
                table: "Reminders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_InstructorId",
                table: "Reminders",
                column: "InstructorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reminders_Instructors_InstructorId",
                table: "Reminders",
                column: "InstructorId",
                principalTable: "Instructors",
                principalColumn: "UserId",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reminders_Instructors_InstructorId",
                table: "Reminders");

            migrationBuilder.DropIndex(
                name: "IX_Reminders_InstructorId",
                table: "Reminders");

            migrationBuilder.DropColumn(
                name: "InstructorId",
                table: "Reminders");

            migrationBuilder.AlterColumn<int>(
                name: "StudentId",
                table: "Reminders",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
