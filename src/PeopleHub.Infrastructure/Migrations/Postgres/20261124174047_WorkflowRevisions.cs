using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class WorkflowRevisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WorkflowRevisionHash",
                table: "OvertimeRequest",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkflowRevisionHash",
                table: "LeaveRequest",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkflowRevisionHash",
                table: "AttendanceCorrection",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkflowRevisionHash",
                table: "AdvanceRequest",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WorkflowRevision",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Hash = table.Column<string>(type: "text", nullable: false),
                    DefinitionJson = table.Column<string>(type: "text", nullable: false),
                    ObservedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Revision = table.Column<string>(type: "text", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowRevision", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRevision_Kind_Hash",
                table: "WorkflowRevision",
                columns: new[] { "Kind", "Hash" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowRevision");

            migrationBuilder.DropColumn(
                name: "WorkflowRevisionHash",
                table: "OvertimeRequest");

            migrationBuilder.DropColumn(
                name: "WorkflowRevisionHash",
                table: "LeaveRequest");

            migrationBuilder.DropColumn(
                name: "WorkflowRevisionHash",
                table: "AttendanceCorrection");

            migrationBuilder.DropColumn(
                name: "WorkflowRevisionHash",
                table: "AdvanceRequest");
        }
    }
}
