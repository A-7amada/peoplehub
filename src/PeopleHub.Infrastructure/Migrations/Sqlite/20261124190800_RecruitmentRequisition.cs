using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class RecruitmentRequisition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalReason",
                table: "JobOpening",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                table: "JobOpening",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ApprovedByUserId",
                table: "JobOpening",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DecidedAt",
                table: "JobOpening",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Justification",
                table: "JobOpening",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RequestedByUserId",
                table: "JobOpening",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovalReason",
                table: "JobOpening");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "JobOpening");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "JobOpening");

            migrationBuilder.DropColumn(
                name: "DecidedAt",
                table: "JobOpening");

            migrationBuilder.DropColumn(
                name: "Justification",
                table: "JobOpening");

            migrationBuilder.DropColumn(
                name: "RequestedByUserId",
                table: "JobOpening");
        }
    }
}
