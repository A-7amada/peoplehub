using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Postgres
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EnrollmentId = table.Column<int>(type: "integer", nullable: false),
                    EffectiveOn = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    Actor = table.Column<string>(type: "text", nullable: false),
                    At = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Revision = table.Column<string>(type: "text", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenefitCancellation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BenefitEnrollment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlanId = table.Column<int>(type: "integer", nullable: false),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    DependentId = table.Column<int>(type: "integer", nullable: true),
                    CoveredName = table.Column<string>(type: "text", nullable: false),
                    CoveredRelationship = table.Column<string>(type: "text", nullable: false),
                    StartsOn = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndsOn = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EligibilityEvidence = table.Column<string>(type: "text", nullable: false),
                    CostCenter = table.Column<string>(type: "text", nullable: false),
                    Actor = table.Column<string>(type: "text", nullable: false),
                    At = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Revision = table.Column<string>(type: "text", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenefitEnrollment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BenefitPlan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    Eligibility = table.Column<string>(type: "text", nullable: false),
                    Coverage = table.Column<string>(type: "text", nullable: false),
                    DependentsAllowed = table.Column<bool>(type: "boolean", nullable: false),
                    EmployerMonthly = table.Column<double>(type: "double precision", nullable: false),
                    EmployeeMonthly = table.Column<double>(type: "double precision", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Actor = table.Column<string>(type: "text", nullable: false),
                    At = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Revision = table.Column<string>(type: "text", nullable: true, defaultValue: "")
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
