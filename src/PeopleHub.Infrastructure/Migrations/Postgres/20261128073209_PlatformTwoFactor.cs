using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class PlatformTwoFactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "LastTotpStep",
                table: "PlatformUser",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "RecoveryHashes",
                table: "PlatformUser",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "TotpEnabled",
                table: "PlatformUser",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TotpSecret",
                table: "PlatformUser",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastTotpStep",
                table: "PlatformUser");

            migrationBuilder.DropColumn(
                name: "RecoveryHashes",
                table: "PlatformUser");

            migrationBuilder.DropColumn(
                name: "TotpEnabled",
                table: "PlatformUser");

            migrationBuilder.DropColumn(
                name: "TotpSecret",
                table: "PlatformUser");
        }
    }
}
