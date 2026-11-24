using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class ApprovalReassignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignmentStamp",
                table: "OvertimeRequest",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AssignmentStamp",
                table: "LeaveRequest",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AssignmentStamp",
                table: "AttendanceCorrection",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AssignmentStamp",
                table: "AdvanceRequest",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ApprovalAssignment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    RequestId = table.Column<int>(type: "integer", nullable: false),
                    StageIndex = table.Column<int>(type: "integer", nullable: false),
                    ToUserId = table.Column<int>(type: "integer", nullable: false),
                    ToEmail = table.Column<string>(type: "text", nullable: false),
                    Actor = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    At = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Revision = table.Column<string>(type: "text", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalAssignment", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalAssignment_Kind_RequestId_StageIndex",
                table: "ApprovalAssignment",
                columns: new[] { "Kind", "RequestId", "StageIndex" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalAssignment");

            migrationBuilder.DropColumn(
                name: "AssignmentStamp",
                table: "OvertimeRequest");

            migrationBuilder.DropColumn(
                name: "AssignmentStamp",
                table: "LeaveRequest");

            migrationBuilder.DropColumn(
                name: "AssignmentStamp",
                table: "AttendanceCorrection");

            migrationBuilder.DropColumn(
                name: "AssignmentStamp",
                table: "AdvanceRequest");
        }
    }
}
