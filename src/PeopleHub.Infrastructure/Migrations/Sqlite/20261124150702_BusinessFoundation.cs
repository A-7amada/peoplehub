using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Sqlite
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
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CalculationVersion",
                table: "PayrollRun",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FinalizedBy",
                table: "PayrollRun",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankSnapshot",
                table: "PayrollLine",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CostCenterSnapshot",
                table: "PayrollLine",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DepartmentArSnapshot",
                table: "PayrollLine",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DepartmentSnapshot",
                table: "PayrollLine",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmployeeCodeSnapshot",
                table: "PayrollLine",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmployeeNameArSnapshot",
                table: "PayrollLine",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmployeeNameSnapshot",
                table: "PayrollLine",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "FixedDeduction",
                table: "PayrollLine",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "IbanSnapshot",
                table: "PayrollLine",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "EmployeeAssignment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DepartmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    BranchId = table.Column<int>(type: "INTEGER", nullable: false),
                    JobTitleId = table.Column<int>(type: "INTEGER", nullable: false),
                    ManagerId = table.Column<int>(type: "INTEGER", nullable: true),
                    ShiftId = table.Column<int>(type: "INTEGER", nullable: true),
                    BasicSalary = table.Column<double>(type: "REAL", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: false),
                    Actor = table.Column<string>(type: "TEXT", nullable: false),
                    Revision = table.Column<string>(type: "TEXT", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeAssignment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeaveTransaction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    LeaveTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    RequestId = table.Column<int>(type: "INTEGER", nullable: true),
                    Delta = table.Column<int>(type: "INTEGER", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: false),
                    Actor = table.Column<string>(type: "TEXT", nullable: false),
                    At = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Revision = table.Column<string>(type: "TEXT", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveTransaction", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayrollCalculationSnapshot",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RunId = table.Column<int>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Event = table.Column<string>(type: "TEXT", nullable: false),
                    PolicyVersion = table.Column<string>(type: "TEXT", nullable: false),
                    At = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Actor = table.Column<string>(type: "TEXT", nullable: false),
                    Payload = table.Column<string>(type: "TEXT", nullable: false),
                    Sha256 = table.Column<string>(type: "TEXT", nullable: false),
                    Revision = table.Column<string>(type: "TEXT", nullable: true, defaultValue: "")
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
