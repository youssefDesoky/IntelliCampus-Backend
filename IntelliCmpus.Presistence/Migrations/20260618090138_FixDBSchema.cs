using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliCampus.Presistence.Migrations
{
    /// <inheritdoc />
    public partial class FixDBSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add missing DepartmentId column to ElectiveBuckets (edited retroactively in InitialCreate)
            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "ElectiveBuckets",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_ElectiveBuckets_DepartmentId",
                table: "ElectiveBuckets",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_ElectiveBuckets_Departments_DepartmentId",
                table: "ElectiveBuckets",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "DepartmentId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ElectiveBuckets_Departments_DepartmentId",
                table: "ElectiveBuckets");

            migrationBuilder.DropIndex(
                name: "IX_ElectiveBuckets_DepartmentId",
                table: "ElectiveBuckets");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "ElectiveBuckets");
        }
    }
}
