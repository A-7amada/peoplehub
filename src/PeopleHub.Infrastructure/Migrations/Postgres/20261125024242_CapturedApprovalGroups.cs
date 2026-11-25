using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class CapturedApprovalGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RequiredCount",
                table: "WorkflowStep",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Rule",
                table: "WorkflowStep",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ParticipantEmployeeId",
                table: "ApprovalAssignment",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ApprovalVote",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    RequestId = table.Column<int>(type: "integer", nullable: false),
                    StageIndex = table.Column<int>(type: "integer", nullable: false),
                    ParticipantEmployeeId = table.Column<int>(type: "integer", nullable: false),
                    ActorEmployeeId = table.Column<int>(type: "integer", nullable: false),
                    ActorUserId = table.Column<int>(type: "integer", nullable: false),
                    At = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Revision = table.Column<string>(type: "text", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalVote", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalVote_Kind_RequestId_StageIndex_ActorEmployeeId",
                table: "ApprovalVote",
                columns: new[] { "Kind", "RequestId", "StageIndex", "ActorEmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalVote_Kind_RequestId_StageIndex_ParticipantEmployeeId",
                table: "ApprovalVote",
                columns: new[] { "Kind", "RequestId", "StageIndex", "ParticipantEmployeeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalVote");

            migrationBuilder.DropColumn(
                name: "RequiredCount",
                table: "WorkflowStep");

            migrationBuilder.DropColumn(
                name: "Rule",
                table: "WorkflowStep");

            migrationBuilder.DropColumn(
                name: "ParticipantEmployeeId",
                table: "ApprovalAssignment");
        }
    }
}
