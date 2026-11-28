using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Postgres
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Purpose = table.Column<string>(type: "text", nullable: false),
                    OwnerName = table.Column<string>(type: "text", nullable: false),
                    SupportedVersion = table.Column<string>(type: "text", nullable: false),
                    AuthMethod = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CredentialRef = table.Column<string>(type: "text", nullable: false),
                    CredentialRotatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Actor = table.Column<string>(type: "text", nullable: false),
                    At = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Revision = table.Column<string>(type: "text", nullable: true, defaultValue: ""),
                    TenantId = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
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
