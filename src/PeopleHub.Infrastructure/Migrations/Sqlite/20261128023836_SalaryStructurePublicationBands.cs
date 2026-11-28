using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class SalaryStructurePublicationBands : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "BandMax",
                table: "SalaryComponent",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "BandMin",
                table: "SalaryComponent",
                type: "REAL",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SalaryStructurePublication",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StructureId = table.Column<int>(type: "INTEGER", nullable: false),
                    Sha256 = table.Column<string>(type: "TEXT", nullable: false),
                    Actor = table.Column<string>(type: "TEXT", nullable: false),
                    At = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Revision = table.Column<string>(type: "TEXT", nullable: true, defaultValue: ""),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryStructurePublication", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalaryStructurePublication_TenantId",
                table: "SalaryStructurePublication",
                column: "TenantId");

            // Structures recorded before the lifecycle existed were applicable, so they are back-filled as published.
            migrationBuilder.Sql("INSERT INTO \"SalaryStructurePublication\" (\"StructureId\", \"Sha256\", \"Actor\", \"At\", \"TenantId\") SELECT \"Id\", 'pre-lifecycle backfill', 'system', \"At\", \"TenantId\" FROM \"SalaryStructure\"");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryStructurePublication_TenantId_StructureId",
                table: "SalaryStructurePublication",
                columns: new[] { "TenantId", "StructureId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalaryStructurePublication");

            migrationBuilder.DropColumn(
                name: "BandMax",
                table: "SalaryComponent");

            migrationBuilder.DropColumn(
                name: "BandMin",
                table: "SalaryComponent");
        }
    }
}
