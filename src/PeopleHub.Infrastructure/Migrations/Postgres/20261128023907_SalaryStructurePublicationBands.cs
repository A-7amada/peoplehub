using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class SalaryStructurePublicationBands : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BandMax",
                table: "SalaryComponent",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BandMin",
                table: "SalaryComponent",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SalaryStructurePublication",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StructureId = table.Column<int>(type: "integer", nullable: false),
                    Sha256 = table.Column<string>(type: "text", nullable: false),
                    Actor = table.Column<string>(type: "text", nullable: false),
                    At = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Revision = table.Column<string>(type: "text", nullable: true, defaultValue: ""),
                    TenantId = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
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
