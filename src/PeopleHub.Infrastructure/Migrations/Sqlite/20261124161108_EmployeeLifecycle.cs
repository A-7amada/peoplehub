using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class EmployeeLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployeeDependent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Relationship = table.Column<string>(type: "TEXT", nullable: false),
                    BirthDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Actor = table.Column<string>(type: "TEXT", nullable: false),
                    At = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Revision = table.Column<string>(type: "TEXT", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeDependent", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeFieldValue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    FieldId = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    Revision = table.Column<string>(type: "TEXT", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeFieldValue", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeProbation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    EndsOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Decision = table.Column<string>(type: "TEXT", nullable: false),
                    DecisionAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Reason = table.Column<string>(type: "TEXT", nullable: false),
                    Actor = table.Column<string>(type: "TEXT", nullable: false),
                    Revision = table.Column<string>(type: "TEXT", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeProbation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HrFieldDefinition",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Label = table.Column<string>(type: "TEXT", nullable: false),
                    LabelAr = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false),
                    Revision = table.Column<string>(type: "TEXT", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HrFieldDefinition", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LifecycleTask",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Process = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    AssignedEmployeeId = table.Column<int>(type: "INTEGER", nullable: true),
                    DueOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CompletionNote = table.Column<string>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedBy = table.Column<string>(type: "TEXT", nullable: false),
                    Revision = table.Column<string>(type: "TEXT", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LifecycleTask", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeFieldValue_EmployeeId_FieldId",
                table: "EmployeeFieldValue",
                columns: new[] { "EmployeeId", "FieldId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProbation_EmployeeId",
                table: "EmployeeProbation",
                column: "EmployeeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HrFieldDefinition_Label",
                table: "HrFieldDefinition",
                column: "Label",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeDependent");

            migrationBuilder.DropTable(
                name: "EmployeeFieldValue");

            migrationBuilder.DropTable(
                name: "EmployeeProbation");

            migrationBuilder.DropTable(
                name: "HrFieldDefinition");

            migrationBuilder.DropTable(
                name: "LifecycleTask");
        }
    }
}
