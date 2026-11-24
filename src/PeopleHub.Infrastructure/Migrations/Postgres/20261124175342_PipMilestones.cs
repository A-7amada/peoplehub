using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Postgres
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppraisalId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    SuccessMeasure = table.Column<string>(type: "text", nullable: false),
                    DueOn = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Revision = table.Column<string>(type: "text", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PipMilestone", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PipProgress",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MilestoneId = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Progress = table.Column<int>(type: "integer", nullable: true),
                    Note = table.Column<string>(type: "text", nullable: false),
                    Actor = table.Column<string>(type: "text", nullable: false),
                    At = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Revision = table.Column<string>(type: "text", nullable: true, defaultValue: "")
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
