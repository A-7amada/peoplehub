using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Sqlite
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
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CaseId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Granted = table.Column<bool>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: false),
                    ActorUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Actor = table.Column<string>(type: "TEXT", nullable: false),
                    At = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Revision = table.Column<string>(type: "TEXT", nullable: true, defaultValue: "")
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
