using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class GrievanceResponseAppeals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployeeGrievanceAppeal",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GrievanceId = table.Column<int>(type: "INTEGER", nullable: false),
                    ResponseId = table.Column<int>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: false),
                    ActorUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Actor = table.Column<string>(type: "TEXT", nullable: false),
                    At = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Revision = table.Column<string>(type: "TEXT", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeGrievanceAppeal", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeGrievanceAppealDecision",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AppealId = table.Column<int>(type: "INTEGER", nullable: false),
                    GrievanceId = table.Column<int>(type: "INTEGER", nullable: false),
                    Decision = table.Column<string>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: false),
                    ActorUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Actor = table.Column<string>(type: "TEXT", nullable: false),
                    At = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Revision = table.Column<string>(type: "TEXT", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeGrievanceAppealDecision", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeGrievanceAppeal_ResponseId",
                table: "EmployeeGrievanceAppeal",
                column: "ResponseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeGrievanceAppealDecision_AppealId",
                table: "EmployeeGrievanceAppealDecision",
                column: "AppealId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeGrievanceAppeal");

            migrationBuilder.DropTable(
                name: "EmployeeGrievanceAppealDecision");
        }
    }
}
