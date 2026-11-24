using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class LegacyPlanningStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"TrainingCourse\" SET \"ApprovalStatus\" = 'Legacy' WHERE \"ApprovalStatus\" = '' AND \"CreatedByUserId\" IS NULL");
            migrationBuilder.Sql("UPDATE \"JobOpening\" SET \"ApprovalStatus\" = 'Legacy' WHERE \"ApprovalStatus\" = '' AND \"RequestedByUserId\" IS NULL");
            migrationBuilder.AlterColumn<string>(
                name: "ApprovalStatus",
                table: "TrainingCourse",
                type: "text",
                nullable: false,
                defaultValue: "Legacy",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ApprovalStatus",
                table: "JobOpening",
                type: "text",
                nullable: false,
                defaultValue: "Legacy",
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ApprovalStatus",
                table: "TrainingCourse",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "Legacy");

            migrationBuilder.AlterColumn<string>(
                name: "ApprovalStatus",
                table: "JobOpening",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "Legacy");
        }
    }
}
