using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliCampus.Presistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChatMessageTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    MessageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SenderId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RecipientId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GroupName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.MessageId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessages");
        }
    }
}
