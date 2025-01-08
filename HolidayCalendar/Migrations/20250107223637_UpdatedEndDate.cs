using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HolidayCalendar.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedEndDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TestDate",
                table: "Events",
                newName: "EndDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EndDate",
                table: "Events",
                newName: "TestDate");
        }
    }
}
