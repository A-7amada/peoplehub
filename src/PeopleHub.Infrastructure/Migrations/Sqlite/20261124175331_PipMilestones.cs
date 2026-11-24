using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class PipMilestones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PipMilestone",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AppraisalId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    SuccessMeasure = table.Column<string>(type: "TEXT", nullable: false),
                    DueOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Revision = table.Column<string>(type: "TEXT", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PipMilestone", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PipProgress",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MilestoneId = table.Column<int>(type: "INTEGER", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    Progress = table.Column<int>(type: "INTEGER", nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: false),
                    Actor = table.Column<string>(type: "TEXT", nullable: false),
                    At = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Revision = table.Column<string>(type: "TEXT", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PipProgress", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PipMilestone_AppraisalId",
                table: "PipMilestone",
                column: "AppraisalId");

            migrationBuilder.CreateIndex(
                name: "IX_PipProgress_MilestoneId",
                table: "PipProgress",
                column: "MilestoneId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PipMilestone");

            migrationBuilder.DropTable(
                name: "PipProgress");
        }
    }
}
