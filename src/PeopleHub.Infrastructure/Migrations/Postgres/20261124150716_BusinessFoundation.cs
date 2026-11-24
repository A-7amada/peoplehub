using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class BusinessFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AdvancesApplied",
                table: "PayrollRun",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CalculationVersion",
                table: "PayrollRun",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FinalizedBy",
                table: "PayrollRun",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankSnapshot",
                table: "PayrollLine",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CostCenterSnapshot",
                table: "PayrollLine",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DepartmentArSnapshot",
                table: "PayrollLine",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DepartmentSnapshot",
                table: "PayrollLine",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmployeeCodeSnapshot",
                table: "PayrollLine",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmployeeNameArSnapshot",
                table: "PayrollLine",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmployeeNameSnapshot",
                table: "PayrollLine",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "FixedDeduction",
                table: "PayrollLine",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "IbanSnapshot",
                table: "PayrollLine",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "EmployeeAssignment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DepartmentId = table.Column<int>(type: "integer", nullable: false),
                    BranchId = table.Column<int>(type: "integer", nullable: false),
                    JobTitleId = table.Column<int>(type: "integer", nullable: false),
                    ManagerId = table.Column<int>(type: "integer", nullable: true),
                    ShiftId = table.Column<int>(type: "integer", nullable: true),
                    BasicSalary = table.Column<double>(type: "double precision", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    Actor = table.Column<string>(type: "text", nullable: false),
                    Revision = table.Column<string>(type: "text", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeAssignment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeaveTransaction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    LeaveTypeId = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    RequestId = table.Column<int>(type: "integer", nullable: true),
                    Delta = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    Actor = table.Column<string>(type: "text", nullable: false),
                    At = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Revision = table.Column<string>(type: "text", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveTransaction", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayrollCalculationSnapshot",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RunId = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Event = table.Column<string>(type: "text", nullable: false),
                    PolicyVersion = table.Column<string>(type: "text", nullable: false),
                    At = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Actor = table.Column<string>(type: "text", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    Sha256 = table.Column<string>(type: "text", nullable: false),
                    Revision = table.Column<string>(type: "text", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollCalculationSnapshot", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollCalculationSnapshot_RunId_Version",
                table: "PayrollCalculationSnapshot",
                columns: new[] { "RunId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeAssignment");

            migrationBuilder.DropTable(
                name: "LeaveTransaction");

            migrationBuilder.DropTable(
                name: "PayrollCalculationSnapshot");

            migrationBuilder.DropColumn(
                name: "AdvancesApplied",
                table: "PayrollRun");

            migrationBuilder.DropColumn(
                name: "CalculationVersion",
                table: "PayrollRun");

            migrationBuilder.DropColumn(
                name: "FinalizedBy",
                table: "PayrollRun");

            migrationBuilder.DropColumn(
                name: "BankSnapshot",
                table: "PayrollLine");

            migrationBuilder.DropColumn(
                name: "CostCenterSnapshot",
                table: "PayrollLine");

            migrationBuilder.DropColumn(
                name: "DepartmentArSnapshot",
                table: "PayrollLine");

            migrationBuilder.DropColumn(
                name: "DepartmentSnapshot",
                table: "PayrollLine");

            migrationBuilder.DropColumn(
                name: "EmployeeCodeSnapshot",
                table: "PayrollLine");

            migrationBuilder.DropColumn(
                name: "EmployeeNameArSnapshot",
                table: "PayrollLine");

            migrationBuilder.DropColumn(
                name: "EmployeeNameSnapshot",
                table: "PayrollLine");

            migrationBuilder.DropColumn(
                name: "FixedDeduction",
                table: "PayrollLine");

            migrationBuilder.DropColumn(
                name: "IbanSnapshot",
                table: "PayrollLine");
        }
    }
}
