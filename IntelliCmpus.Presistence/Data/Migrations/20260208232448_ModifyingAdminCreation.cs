using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliCampus.DAL.Migrations
{
    /// <inheritdoc />
    public partial class ModifyingAdminCreation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminCode",
                table: "Admins",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminCode",
                table: "Admins");
        }
    }
}
