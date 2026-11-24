using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class TrainingPlanning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                table: "TrainingCourse",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "TrainingCourse",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "TrainingCourse",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DecidedAt",
                table: "TrainingCourse",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DecidedByUserId",
                table: "TrainingCourse",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DecisionReason",
                table: "TrainingCourse",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Need",
                table: "TrainingCourse",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "PlannedBudget",
                table: "TrainingCourse",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "TrainingCourse");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "TrainingCourse");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "TrainingCourse");

            migrationBuilder.DropColumn(
                name: "DecidedAt",
                table: "TrainingCourse");

            migrationBuilder.DropColumn(
                name: "DecidedByUserId",
                table: "TrainingCourse");

            migrationBuilder.DropColumn(
                name: "DecisionReason",
                table: "TrainingCourse");

            migrationBuilder.DropColumn(
                name: "Need",
                table: "TrainingCourse");

            migrationBuilder.DropColumn(
                name: "PlannedBudget",
                table: "TrainingCourse");
        }
    }
}
