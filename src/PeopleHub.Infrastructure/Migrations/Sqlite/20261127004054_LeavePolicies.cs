using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Sqlite
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
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LeaveTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    FromServiceYears = table.Column<int>(type: "INTEGER", nullable: false),
                    FromAge = table.Column<int>(type: "INTEGER", nullable: true),
                    Days = table.Column<int>(type: "INTEGER", nullable: false),
                    Revision = table.Column<string>(type: "TEXT", nullable: true, defaultValue: ""),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveEntitlementTier", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeavePolicy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LeaveTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    WaitingMonths = table.Column<int>(type: "INTEGER", nullable: false),
                    Accrual = table.Column<string>(type: "TEXT", nullable: false),
                    ProrateFirstYear = table.Column<bool>(type: "INTEGER", nullable: false),
                    CarryExpiryMonths = table.Column<int>(type: "INTEGER", nullable: false),
                    AllowHalfDay = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowHourly = table.Column<bool>(type: "INTEGER", nullable: false),
                    HoursPerDay = table.Column<int>(type: "INTEGER", nullable: false),
                    Revision = table.Column<string>(type: "TEXT", nullable: true, defaultValue: ""),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1)
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
