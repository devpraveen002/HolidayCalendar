using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HolidayCalendar.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedUserCalendar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "UserCalendars",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "UserCalendars");
        }
    }
}
