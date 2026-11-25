using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Postgres
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GrievanceId = table.Column<int>(type: "integer", nullable: false),
                    ResponseId = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: false),
                    ActorUserId = table.Column<int>(type: "integer", nullable: false),
                    Actor = table.Column<string>(type: "text", nullable: false),
                    At = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Revision = table.Column<string>(type: "text", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeGrievanceAppeal", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeGrievanceAppealDecision",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppealId = table.Column<int>(type: "integer", nullable: false),
                    GrievanceId = table.Column<int>(type: "integer", nullable: false),
                    Decision = table.Column<string>(type: "text", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: false),
                    ActorUserId = table.Column<int>(type: "integer", nullable: false),
                    Actor = table.Column<string>(type: "text", nullable: false),
                    At = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Revision = table.Column<string>(type: "text", nullable: true, defaultValue: "")
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
