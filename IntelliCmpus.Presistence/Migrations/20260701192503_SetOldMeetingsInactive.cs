using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliCampus.Presistence.Migrations
{
    /// <inheritdoc />
    public partial class SetOldMeetingsInactive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [Meetings] SET [IsActive] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [Meetings] SET [IsActive] = 1");
        }
    }
}
