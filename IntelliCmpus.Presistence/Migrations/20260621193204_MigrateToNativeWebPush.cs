using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliCampus.Presistence.Migrations
{
    /// <inheritdoc />
    public partial class MigrateToNativeWebPush : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Token",
                table: "DeviceTokens",
                newName: "Endpoint");

            migrationBuilder.RenameIndex(
                name: "IX_DeviceTokens_Token",
                table: "DeviceTokens",
                newName: "IX_DeviceTokens_Endpoint");

            migrationBuilder.AddColumn<string>(
                name: "Auth",
                table: "DeviceTokens",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "P256dh",
                table: "DeviceTokens",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Auth",
                table: "DeviceTokens");

            migrationBuilder.DropColumn(
                name: "P256dh",
                table: "DeviceTokens");

            migrationBuilder.RenameColumn(
                name: "Endpoint",
                table: "DeviceTokens",
                newName: "Token");

            migrationBuilder.RenameIndex(
                name: "IX_DeviceTokens_Endpoint",
                table: "DeviceTokens",
                newName: "IX_DeviceTokens_Token");
        }
    }
}
