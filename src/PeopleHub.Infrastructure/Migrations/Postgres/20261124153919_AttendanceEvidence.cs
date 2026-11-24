using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AttendanceEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "DevicePunch",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceivedAt",
                table: "DevicePunch",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AttendanceEvidence",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Event = table.Column<string>(type: "text", nullable: false),
                    At = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Actor = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    BeforeIn = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    BeforeOut = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    AfterIn = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    AfterOut = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CorrectionId = table.Column<int>(type: "integer", nullable: true),
                    Revision = table.Column<string>(type: "text", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceEvidence", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DevicePunch_IdempotencyKey",
                table: "DevicePunch",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceEvidence_EmployeeId_Date",
                table: "AttendanceEvidence",
                columns: new[] { "EmployeeId", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceEvidence");

            migrationBuilder.DropIndex(
                name: "IX_DevicePunch_IdempotencyKey",
                table: "DevicePunch");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "DevicePunch");

            migrationBuilder.DropColumn(
                name: "ReceivedAt",
                table: "DevicePunch");
        }
    }
}
