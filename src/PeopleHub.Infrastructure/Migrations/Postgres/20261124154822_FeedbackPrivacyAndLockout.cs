using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class FeedbackPrivacyAndLockout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MinimumFeedbackReviewers",
                table: "PerfCycle",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FailedLoginCount",
                table: "AppUser",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastFailedLoginAt",
                table: "AppUser",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUntil",
                table: "AppUser",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MinimumFeedbackReviewers",
                table: "PerfCycle");

            migrationBuilder.DropColumn(
                name: "FailedLoginCount",
                table: "AppUser");

            migrationBuilder.DropColumn(
                name: "LastFailedLoginAt",
                table: "AppUser");

            migrationBuilder.DropColumn(
                name: "LockedUntil",
                table: "AppUser");
        }
    }
}
