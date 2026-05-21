using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliCampus.Presistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class LocationAndPriorityReminder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Reminders' AND COLUMN_NAME = 'Location')
                BEGIN
                    ALTER TABLE [Reminders] ADD [Location] nvarchar(200) NULL;
                END
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Reminders' AND COLUMN_NAME = 'Priority')
                BEGIN
                    ALTER TABLE [Reminders] ADD [Priority] nvarchar(20) NOT NULL DEFAULT 'low';
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Reminders' AND COLUMN_NAME = 'Location')
                BEGIN
                    ALTER TABLE [Reminders] DROP COLUMN [Location];
                END
                """);

            migrationBuilder.Sql("""
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Reminders' AND COLUMN_NAME = 'Priority')
                BEGIN
                    ALTER TABLE [Reminders] DROP COLUMN [Priority];
                END
                """);
        }
    }
}
