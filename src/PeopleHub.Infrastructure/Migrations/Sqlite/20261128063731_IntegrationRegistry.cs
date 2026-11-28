using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class IntegrationRegistry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IntegrationEntry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Purpose = table.Column<string>(type: "TEXT", nullable: false),
                    OwnerName = table.Column<string>(type: "TEXT", nullable: false),
                    SupportedVersion = table.Column<string>(type: "TEXT", nullable: false),
                    AuthMethod = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CredentialRef = table.Column<string>(type: "TEXT", nullable: false),
                    CredentialRotatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Actor = table.Column<string>(type: "TEXT", nullable: false),
                    At = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Revision = table.Column<string>(type: "TEXT", nullable: true, defaultValue: ""),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationEntry", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationEntry_TenantId",
                table: "IntegrationEntry",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationEntry_TenantId_Name",
                table: "IntegrationEntry",
                columns: new[] { "TenantId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntegrationEntry");
        }
    }
}
