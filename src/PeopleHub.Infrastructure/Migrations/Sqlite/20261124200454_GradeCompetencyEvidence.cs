using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Sqlite
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
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "JobGrade",
                table: "CompetencyTemplate",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CompetencyApplication",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AppraisalId = table.Column<int>(type: "INTEGER", nullable: false),
                    TemplateId = table.Column<int>(type: "INTEGER", nullable: false),
                    JobGrade = table.Column<int>(type: "INTEGER", nullable: true),
                    TemplateName = table.Column<string>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    Sha256 = table.Column<string>(type: "TEXT", nullable: false),
                    Actor = table.Column<string>(type: "TEXT", nullable: false),
                    At = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Revision = table.Column<string>(type: "TEXT", nullable: true, defaultValue: "")
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
