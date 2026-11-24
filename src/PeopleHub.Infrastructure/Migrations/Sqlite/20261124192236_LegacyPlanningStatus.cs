using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Sqlite
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
                type: "TEXT",
                nullable: false,
                defaultValue: "Legacy",
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "ApprovalStatus",
                table: "JobOpening",
                type: "TEXT",
                nullable: false,
                defaultValue: "Legacy",
                oldClrType: typeof(string),
                oldType: "TEXT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ApprovalStatus",
                table: "TrainingCourse",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldDefaultValue: "Legacy");

            migrationBuilder.AlterColumn<string>(
                name: "ApprovalStatus",
                table: "JobOpening",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldDefaultValue: "Legacy");
        }
    }
}
