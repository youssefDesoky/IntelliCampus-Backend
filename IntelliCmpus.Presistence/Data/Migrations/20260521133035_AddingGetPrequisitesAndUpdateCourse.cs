using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliCampus.Presistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddingGetPrequisitesAndUpdateCourse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop FK if it exists
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'FK_AnnouncementComments_Classes_ClassId', 'F') IS NOT NULL
                    ALTER TABLE [AnnouncementComments] DROP CONSTRAINT [FK_AnnouncementComments_Classes_ClassId];
                """);

            // Drop FK if it exists
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'FK_Attendances_Announcements_AnnouncementId', 'F') IS NOT NULL
                    ALTER TABLE [Attendances] DROP CONSTRAINT [FK_Attendances_Announcements_AnnouncementId];
                """);

            // Drop FK if it exists
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'FK_Attendances_Users_UserId', 'F') IS NOT NULL
                    ALTER TABLE [Attendances] DROP CONSTRAINT [FK_Attendances_Users_UserId];
                """);

            // Drop columns if they still exist
            migrationBuilder.Sql("""
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Attendances' AND COLUMN_NAME = 'AnnouncementId')
                    ALTER TABLE [Attendances] DROP COLUMN [AnnouncementId];
                """);

            migrationBuilder.Sql("""
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Attendances' AND COLUMN_NAME = 'UserId')
                    ALTER TABLE [Attendances] DROP COLUMN [UserId];
                """);

            migrationBuilder.Sql("""
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AnnouncementComments' AND COLUMN_NAME = 'ClassId')
                    ALTER TABLE [AnnouncementComments] DROP COLUMN [ClassId];
                """);

            // Add FK only if it doesn't already exist
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'FK_AnnouncementComments_Announcements_AnnouncementId', 'F') IS NULL
                    ALTER TABLE [AnnouncementComments] ADD CONSTRAINT [FK_AnnouncementComments_Announcements_AnnouncementId]
                        FOREIGN KEY ([AnnouncementId]) REFERENCES [Announcements] ([AnnouncementId]) ON DELETE CASCADE;
                """);

            // Add FK only if it doesn't already exist
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'FK_AnnouncementComments_Users_UserId', 'F') IS NULL
                    ALTER TABLE [AnnouncementComments] ADD CONSTRAINT [FK_AnnouncementComments_Users_UserId]
                        FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]);
                """);

            // Add FK only if it doesn't already exist
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'FK_Assignments_Classes_ClassId', 'F') IS NULL
                    ALTER TABLE [Assignments] ADD CONSTRAINT [FK_Assignments_Classes_ClassId]
                        FOREIGN KEY ([ClassId]) REFERENCES [Classes] ([ClassId]) ON DELETE CASCADE;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop FKs added above
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'FK_AnnouncementComments_Announcements_AnnouncementId', 'F') IS NOT NULL
                    ALTER TABLE [AnnouncementComments] DROP CONSTRAINT [FK_AnnouncementComments_Announcements_AnnouncementId];
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'FK_AnnouncementComments_Users_UserId', 'F') IS NOT NULL
                    ALTER TABLE [AnnouncementComments] DROP CONSTRAINT [FK_AnnouncementComments_Users_UserId];
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'FK_Assignments_Classes_ClassId', 'F') IS NOT NULL
                    ALTER TABLE [Assignments] DROP CONSTRAINT [FK_Assignments_Classes_ClassId];
                """);

            // Restore columns if not already present
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Attendances' AND COLUMN_NAME = 'AnnouncementId')
                    ALTER TABLE [Attendances] ADD [AnnouncementId] int NULL;
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Attendances' AND COLUMN_NAME = 'UserId')
                    ALTER TABLE [Attendances] ADD [UserId] int NULL;
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AnnouncementComments' AND COLUMN_NAME = 'ClassId')
                    ALTER TABLE [AnnouncementComments] ADD [ClassId] int NULL;
                """);

            // Restore FKs
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'FK_AnnouncementComments_Classes_ClassId', 'F') IS NULL
                    ALTER TABLE [AnnouncementComments] ADD CONSTRAINT [FK_AnnouncementComments_Classes_ClassId]
                        FOREIGN KEY ([ClassId]) REFERENCES [Classes] ([ClassId]) ON DELETE CASCADE;
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'FK_Attendances_Announcements_AnnouncementId', 'F') IS NULL
                    ALTER TABLE [Attendances] ADD CONSTRAINT [FK_Attendances_Announcements_AnnouncementId]
                        FOREIGN KEY ([AnnouncementId]) REFERENCES [Announcements] ([AnnouncementId]) ON DELETE CASCADE;
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'FK_Attendances_Users_UserId', 'F') IS NULL
                    ALTER TABLE [Attendances] ADD CONSTRAINT [FK_Attendances_Users_UserId]
                        FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]);
                """);
        }
    }
}
