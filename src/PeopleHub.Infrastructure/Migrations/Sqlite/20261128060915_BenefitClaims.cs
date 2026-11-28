using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class BenefitClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BenefitClaim",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EnrollmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    ServiceDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Amount = table.Column<double>(type: "REAL", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Outcome = table.Column<string>(type: "TEXT", nullable: false),
                    Actor = table.Column<string>(type: "TEXT", nullable: false),
                    At = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Revision = table.Column<string>(type: "TEXT", nullable: true, defaultValue: ""),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenefitClaim", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BenefitClaim_EnrollmentId",
                table: "BenefitClaim",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_BenefitClaim_TenantId",
                table: "BenefitClaim",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BenefitClaim");
        }
    }
}
