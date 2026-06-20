using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliCampus.Presistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInstructorContractFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ContractEndDate",
                table: "Instructors",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ContractStartDate",
                table: "Instructors",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Secondment",
                table: "Instructors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LoanInstructors",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LoanFromDepartmentId = table.Column<int>(type: "int", nullable: true),
                    LoanProfessorId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanInstructors", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_LoanInstructors_Departments_LoanFromDepartmentId",
                        column: x => x.LoanFromDepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LoanInstructors_Instructors_UserId",
                        column: x => x.UserId,
                        principalTable: "Instructors",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoanInstructors_LoanFromDepartmentId",
                table: "LoanInstructors",
                column: "LoanFromDepartmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoanInstructors");

            migrationBuilder.DropColumn(
                name: "ContractEndDate",
                table: "Instructors");

            migrationBuilder.DropColumn(
                name: "ContractStartDate",
                table: "Instructors");

            migrationBuilder.DropColumn(
                name: "Secondment",
                table: "Instructors");
        }
    }
}
