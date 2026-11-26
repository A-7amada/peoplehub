using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Sqlite
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
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    AddressedTo = table.Column<string>(type: "TEXT", nullable: false),
                    IncludeSalary = table.Column<bool>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    RequestedByUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    RequestedBy = table.Column<string>(type: "TEXT", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DecidedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    DecidedBy = table.Column<string>(type: "TEXT", nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DecisionNote = table.Column<string>(type: "TEXT", nullable: false),
                    Reference = table.Column<string>(type: "TEXT", nullable: false),
                    BodyEn = table.Column<string>(type: "TEXT", nullable: false),
                    BodyAr = table.Column<string>(type: "TEXT", nullable: false),
                    Revision = table.Column<string>(type: "TEXT", nullable: true, defaultValue: ""),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1)
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
