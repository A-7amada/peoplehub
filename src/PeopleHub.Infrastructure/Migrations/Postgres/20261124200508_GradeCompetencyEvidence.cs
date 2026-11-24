using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class GradeCompetencyEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChangeStamp",
                table: "CompetencyTemplate",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "JobGrade",
                table: "CompetencyTemplate",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CompetencyApplication",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppraisalId = table.Column<int>(type: "integer", nullable: false),
                    TemplateId = table.Column<int>(type: "integer", nullable: false),
                    JobGrade = table.Column<int>(type: "integer", nullable: true),
                    TemplateName = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Sha256 = table.Column<string>(type: "text", nullable: false),
                    Actor = table.Column<string>(type: "text", nullable: false),
                    At = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Revision = table.Column<string>(type: "text", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetencyApplication", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompetencyApplication_AppraisalId",
                table: "CompetencyApplication",
                column: "AppraisalId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompetencyApplication");

            migrationBuilder.DropColumn(
                name: "ChangeStamp",
                table: "CompetencyTemplate");

            migrationBuilder.DropColumn(
                name: "JobGrade",
                table: "CompetencyTemplate");
        }
    }
}
