using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Sqlite
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
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkflowRevisionHash",
                table: "LeaveRequest",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkflowRevisionHash",
                table: "AttendanceCorrection",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkflowRevisionHash",
                table: "AdvanceRequest",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WorkflowRevision",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    Hash = table.Column<string>(type: "TEXT", nullable: false),
                    DefinitionJson = table.Column<string>(type: "TEXT", nullable: false),
                    ObservedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Revision = table.Column<string>(type: "TEXT", nullable: true, defaultValue: "")
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
