using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliCampus.Presistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameBaylawToBylaw : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_Baylaws_BaylawId",
                table: "Students");

            migrationBuilder.RenameColumn(
                name: "BaylawId",
                table: "Students",
                newName: "BylawId");

            migrationBuilder.RenameIndex(
                name: "IX_Students_BaylawId",
                table: "Students",
                newName: "IX_Students_BylawId");

            migrationBuilder.RenameTable(
                name: "Baylaws",
                newName: "Bylaws");

            migrationBuilder.RenameColumn(
                name: "BaylawId",
                table: "Bylaws",
                newName: "BylawId");

            migrationBuilder.RenameIndex(
                name: "IX_Baylaws_UploadedByAdminId",
                table: "Bylaws",
                newName: "IX_Bylaws_UploadedByAdminId");

            migrationBuilder.Sql("EXEC sp_rename N'PK_Baylaws', N'PK_Bylaws'");
            migrationBuilder.Sql("EXEC sp_rename N'FK_Baylaws_Admins_UploadedByAdminId', N'FK_Bylaws_Admins_UploadedByAdminId'");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Bylaws_BylawId",
                table: "Students",
                column: "BylawId",
                principalTable: "Bylaws",
                principalColumn: "BylawId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_Bylaws_BylawId",
                table: "Students");

            migrationBuilder.Sql("EXEC sp_rename N'FK_Bylaws_Admins_UploadedByAdminId', N'FK_Baylaws_Admins_UploadedByAdminId'");
            migrationBuilder.Sql("EXEC sp_rename N'PK_Bylaws', N'PK_Baylaws'");

            migrationBuilder.RenameIndex(
                name: "IX_Bylaws_UploadedByAdminId",
                table: "Bylaws",
                newName: "IX_Baylaws_UploadedByAdminId");

            migrationBuilder.RenameColumn(
                name: "BylawId",
                table: "Bylaws",
                newName: "BaylawId");

            migrationBuilder.RenameTable(
                name: "Bylaws",
                newName: "Baylaws");

            migrationBuilder.RenameIndex(
                name: "IX_Students_BylawId",
                table: "Students",
                newName: "IX_Students_BaylawId");

            migrationBuilder.RenameColumn(
                name: "BylawId",
                table: "Students",
                newName: "BaylawId");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Baylaws_BaylawId",
                table: "Students",
                column: "BaylawId",
                principalTable: "Baylaws",
                principalColumn: "BaylawId",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
