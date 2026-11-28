using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class LeaveEncashments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LeaveEncashment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    LeaveTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Days = table.Column<double>(type: "REAL", precision: 18, scale: 4, nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    RequestedBy = table.Column<string>(type: "TEXT", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DecidedBy = table.Column<string>(type: "TEXT", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DecisionNote = table.Column<string>(type: "TEXT", nullable: true),
                    Revision = table.Column<string>(type: "TEXT", nullable: true, defaultValue: ""),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveEncashment", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveEncashment_EmployeeId",
                table: "LeaveEncashment",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveEncashment_TenantId",
                table: "LeaveEncashment",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeaveEncashment");
        }
    }
}
