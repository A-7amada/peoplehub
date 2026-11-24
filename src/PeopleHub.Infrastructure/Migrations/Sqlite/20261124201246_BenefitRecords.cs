using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class BenefitRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BenefitCancellation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EnrollmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    EffectiveOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: false),
                    Actor = table.Column<string>(type: "TEXT", nullable: false),
                    At = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Revision = table.Column<string>(type: "TEXT", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenefitCancellation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BenefitEnrollment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    DependentId = table.Column<int>(type: "INTEGER", nullable: true),
                    CoveredName = table.Column<string>(type: "TEXT", nullable: false),
                    CoveredRelationship = table.Column<string>(type: "TEXT", nullable: false),
                    StartsOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndsOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EligibilityEvidence = table.Column<string>(type: "TEXT", nullable: false),
                    CostCenter = table.Column<string>(type: "TEXT", nullable: false),
                    Actor = table.Column<string>(type: "TEXT", nullable: false),
                    At = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Revision = table.Column<string>(type: "TEXT", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenefitEnrollment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BenefitPlan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Provider = table.Column<string>(type: "TEXT", nullable: false),
                    Eligibility = table.Column<string>(type: "TEXT", nullable: false),
                    Coverage = table.Column<string>(type: "TEXT", nullable: false),
                    DependentsAllowed = table.Column<bool>(type: "INTEGER", nullable: false),
                    EmployerMonthly = table.Column<double>(type: "REAL", nullable: false),
                    EmployeeMonthly = table.Column<double>(type: "REAL", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", nullable: false),
                    Actor = table.Column<string>(type: "TEXT", nullable: false),
                    At = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Revision = table.Column<string>(type: "TEXT", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenefitPlan", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BenefitCancellation_EnrollmentId",
                table: "BenefitCancellation",
                column: "EnrollmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BenefitEnrollment_EmployeeId_PlanId",
                table: "BenefitEnrollment",
                columns: new[] { "EmployeeId", "PlanId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BenefitCancellation");

            migrationBuilder.DropTable(
                name: "BenefitEnrollment");

            migrationBuilder.DropTable(
                name: "BenefitPlan");
        }
    }
}
