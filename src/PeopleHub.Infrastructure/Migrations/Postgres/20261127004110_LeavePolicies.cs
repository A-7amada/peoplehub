using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class LeavePolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LeaveEntitlementTier",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeaveTypeId = table.Column<int>(type: "integer", nullable: false),
                    FromServiceYears = table.Column<int>(type: "integer", nullable: false),
                    FromAge = table.Column<int>(type: "integer", nullable: true),
                    Days = table.Column<int>(type: "integer", nullable: false),
                    Revision = table.Column<string>(type: "text", nullable: true, defaultValue: ""),
                    TenantId = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveEntitlementTier", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeavePolicy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeaveTypeId = table.Column<int>(type: "integer", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    WaitingMonths = table.Column<int>(type: "integer", nullable: false),
                    Accrual = table.Column<string>(type: "text", nullable: false),
                    ProrateFirstYear = table.Column<bool>(type: "boolean", nullable: false),
                    CarryExpiryMonths = table.Column<int>(type: "integer", nullable: false),
                    AllowHalfDay = table.Column<bool>(type: "boolean", nullable: false),
                    AllowHourly = table.Column<bool>(type: "boolean", nullable: false),
                    HoursPerDay = table.Column<int>(type: "integer", nullable: false),
                    Revision = table.Column<string>(type: "text", nullable: true, defaultValue: ""),
                    TenantId = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeavePolicy", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveEntitlementTier_TenantId",
                table: "LeaveEntitlementTier",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveEntitlementTier_TenantId_LeaveTypeId",
                table: "LeaveEntitlementTier",
                columns: new[] { "TenantId", "LeaveTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_LeavePolicy_TenantId",
                table: "LeavePolicy",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_LeavePolicy_TenantId_LeaveTypeId",
                table: "LeavePolicy",
                columns: new[] { "TenantId", "LeaveTypeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeaveEntitlementTier");

            migrationBuilder.DropTable(
                name: "LeavePolicy");
        }
    }
}
