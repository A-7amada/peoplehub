using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Postgres
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    LastStartedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastFinishedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastStatus = table.Column<string>(type: "text", nullable: false),
                    LastError = table.Column<string>(type: "text", nullable: false),
                    ConsecutiveFailures = table.Column<int>(type: "integer", nullable: false),
                    RunCount = table.Column<long>(type: "bigint", nullable: false),
                    LeaseUntil = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Revision = table.Column<string>(type: "text", nullable: true, defaultValue: "")
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
