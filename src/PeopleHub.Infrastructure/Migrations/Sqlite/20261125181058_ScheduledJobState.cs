using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class ScheduledJobState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduledJobState",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    LastStartedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastFinishedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastStatus = table.Column<string>(type: "TEXT", nullable: false),
                    LastError = table.Column<string>(type: "TEXT", nullable: false),
                    ConsecutiveFailures = table.Column<int>(type: "INTEGER", nullable: false),
                    RunCount = table.Column<long>(type: "INTEGER", nullable: false),
                    LeaseUntil = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Revision = table.Column<string>(type: "TEXT", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledJobState", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledJobState_Name",
                table: "ScheduledJobState",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduledJobState");
        }
    }
}
