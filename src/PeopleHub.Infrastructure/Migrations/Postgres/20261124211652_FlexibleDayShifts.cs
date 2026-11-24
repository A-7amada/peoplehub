using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class FlexibleDayShifts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Flexible",
                table: "Shift",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "LatestStart",
                table: "Shift",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequiredMinutes",
                table: "Shift",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Flexible",
                table: "Shift");

            migrationBuilder.DropColumn(
                name: "LatestStart",
                table: "Shift");

            migrationBuilder.DropColumn(
                name: "RequiredMinutes",
                table: "Shift");
        }
    }
}
