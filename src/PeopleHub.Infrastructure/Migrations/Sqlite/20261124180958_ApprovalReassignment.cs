using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Sqlite
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
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AssignmentStamp",
                table: "LeaveRequest",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AssignmentStamp",
                table: "AttendanceCorrection",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AssignmentStamp",
                table: "AdvanceRequest",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ApprovalAssignment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    RequestId = table.Column<int>(type: "INTEGER", nullable: false),
                    StageIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    ToUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    ToEmail = table.Column<string>(type: "TEXT", nullable: false),
                    Actor = table.Column<string>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: false),
                    At = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Revision = table.Column<string>(type: "TEXT", nullable: true, defaultValue: "")
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
