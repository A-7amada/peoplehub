using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class EmployeeLetters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployeeLetter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    AddressedTo = table.Column<string>(type: "text", nullable: false),
                    IncludeSalary = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    RequestedByUserId = table.Column<int>(type: "integer", nullable: false),
                    RequestedBy = table.Column<string>(type: "text", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DecidedByUserId = table.Column<int>(type: "integer", nullable: true),
                    DecidedBy = table.Column<string>(type: "text", nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DecisionNote = table.Column<string>(type: "text", nullable: false),
                    Reference = table.Column<string>(type: "text", nullable: false),
                    BodyEn = table.Column<string>(type: "text", nullable: false),
                    BodyAr = table.Column<string>(type: "text", nullable: false),
                    Revision = table.Column<string>(type: "text", nullable: true, defaultValue: ""),
                    TenantId = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeLetter", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLetter_TenantId",
                table: "EmployeeLetter",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLetter_TenantId_EmployeeId_Status",
                table: "EmployeeLetter",
                columns: new[] { "TenantId", "EmployeeId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeLetter");
        }
    }
}
