using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class CaseInvestigatorAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployeeCaseAccess",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CaseId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Granted = table.Column<bool>(type: "boolean", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    ActorUserId = table.Column<int>(type: "integer", nullable: false),
                    Actor = table.Column<string>(type: "text", nullable: false),
                    At = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Revision = table.Column<string>(type: "text", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeCaseAccess", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCaseAccess_CaseId_UserId",
                table: "EmployeeCaseAccess",
                columns: new[] { "CaseId", "UserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeCaseAccess");
        }
    }
}
