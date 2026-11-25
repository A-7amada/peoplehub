using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class ImportRollback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RollbackReason",
                table: "EmployeeImportBatch",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RolledBackAt",
                table: "EmployeeImportBatch",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RolledBackBy",
                table: "EmployeeImportBatch",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RollbackReason",
                table: "EmployeeImportBatch");

            migrationBuilder.DropColumn(
                name: "RolledBackAt",
                table: "EmployeeImportBatch");

            migrationBuilder.DropColumn(
                name: "RolledBackBy",
                table: "EmployeeImportBatch");
        }
    }
}
