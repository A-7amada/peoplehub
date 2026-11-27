using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class TenantScopedUniques : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkflowRevision_Kind_Hash",
                table: "WorkflowRevision");

            migrationBuilder.DropIndex(
                name: "IX_IntegrationEvent_IdempotencyKey",
                table: "IntegrationEvent");

            migrationBuilder.DropIndex(
                name: "IX_HrFieldDefinition_Label",
                table: "HrFieldDefinition");

            migrationBuilder.DropIndex(
                name: "IX_DevicePunch_IdempotencyKey",
                table: "DevicePunch");

            migrationBuilder.DropIndex(
                name: "IX_AppSetting_Key",
                table: "AppSetting");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRevision_Kind_Hash",
                table: "WorkflowRevision",
                columns: new[] { "Kind", "Hash" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRevision_TenantId_Kind_Hash",
                table: "WorkflowRevision",
                columns: new[] { "TenantId", "Kind", "Hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationEvent_IdempotencyKey",
                table: "IntegrationEvent",
                column: "IdempotencyKey");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationEvent_TenantId_IdempotencyKey",
                table: "IntegrationEvent",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HrFieldDefinition_Label",
                table: "HrFieldDefinition",
                column: "Label");

            migrationBuilder.CreateIndex(
                name: "IX_HrFieldDefinition_TenantId_Label",
                table: "HrFieldDefinition",
                columns: new[] { "TenantId", "Label" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DevicePunch_IdempotencyKey",
                table: "DevicePunch",
                column: "IdempotencyKey");

            migrationBuilder.CreateIndex(
                name: "IX_DevicePunch_TenantId_IdempotencyKey",
                table: "DevicePunch",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppSetting_Key",
                table: "AppSetting",
                column: "Key");

            migrationBuilder.CreateIndex(
                name: "IX_AppSetting_TenantId_Key",
                table: "AppSetting",
                columns: new[] { "TenantId", "Key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkflowRevision_Kind_Hash",
                table: "WorkflowRevision");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowRevision_TenantId_Kind_Hash",
                table: "WorkflowRevision");

            migrationBuilder.DropIndex(
                name: "IX_IntegrationEvent_IdempotencyKey",
                table: "IntegrationEvent");

            migrationBuilder.DropIndex(
                name: "IX_IntegrationEvent_TenantId_IdempotencyKey",
                table: "IntegrationEvent");

            migrationBuilder.DropIndex(
                name: "IX_HrFieldDefinition_Label",
                table: "HrFieldDefinition");

            migrationBuilder.DropIndex(
                name: "IX_HrFieldDefinition_TenantId_Label",
                table: "HrFieldDefinition");

            migrationBuilder.DropIndex(
                name: "IX_DevicePunch_IdempotencyKey",
                table: "DevicePunch");

            migrationBuilder.DropIndex(
                name: "IX_DevicePunch_TenantId_IdempotencyKey",
                table: "DevicePunch");

            migrationBuilder.DropIndex(
                name: "IX_AppSetting_Key",
                table: "AppSetting");

            migrationBuilder.DropIndex(
                name: "IX_AppSetting_TenantId_Key",
                table: "AppSetting");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRevision_Kind_Hash",
                table: "WorkflowRevision",
                columns: new[] { "Kind", "Hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationEvent_IdempotencyKey",
                table: "IntegrationEvent",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HrFieldDefinition_Label",
                table: "HrFieldDefinition",
                column: "Label",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DevicePunch_IdempotencyKey",
                table: "DevicePunch",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppSetting_Key",
                table: "AppSetting",
                column: "Key",
                unique: true);
        }
    }
}
