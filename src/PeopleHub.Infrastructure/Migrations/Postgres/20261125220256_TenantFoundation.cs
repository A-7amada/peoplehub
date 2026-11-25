using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class TenantFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Role_Code",
                table: "Role");

            migrationBuilder.DropIndex(
                name: "IX_PayrollRun_Year_Month",
                table: "PayrollRun");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeImportBatch_FileHash",
                table: "EmployeeImportBatch");

            migrationBuilder.DropIndex(
                name: "IX_Employee_Code",
                table: "Employee");

            migrationBuilder.DropIndex(
                name: "IX_Employee_Email",
                table: "Employee");

            migrationBuilder.DropIndex(
                name: "IX_CompanyAsset_Tag",
                table: "CompanyAsset");

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "WorkflowStep",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "WorkflowRevision",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "WorkflowDefinition",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "TrainingOutcome",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "TrainingEnrollment",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "TrainingCourse",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "TrainingCertificateAlert",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "TrainingCertificate",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "StatusHistory",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "StagedEmployeeRow",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Shift",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "SalaryStructureApplication",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "SalaryStructure",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "SalaryComponent",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "RolePermission",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Role",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "RecruitmentEvent",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "PipProgress",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "PipMilestone",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "PerfReminder",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "PerformanceReview",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "PerformanceResponse",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "PerfGoal",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "PerfCycle",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "PerfBonus",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "PerfAppraisal",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "PayrollRun",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "PayrollLine",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "PayrollCalculationSnapshot",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "PayrollBankResult",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "PayrollAdjustmentEvent",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "PayrollAdjustment",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "PasswordReset",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "OvertimeRequest",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Notification",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "LoginSession",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "LifecycleTask",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "LeaveType",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "LeaveTransaction",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "LeaveRequest",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "LeaveBalance",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "JobTitle",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "JobOpening",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "IntegrationEvent",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "HrFieldDefinition",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Holiday",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Feedback360",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "EmploymentChange",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "EmployeeShiftDay",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "EmployeeSalaryComponent",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "EmployeeProbation",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "EmployeeImportBatch",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "EmployeeGrievanceResponse",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "EmployeeGrievanceAppealDecision",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "EmployeeGrievanceAppeal",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "EmployeeGrievance",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "EmployeeFieldValue",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "EmployeeDocument",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "EmployeeDependent",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "EmployeeCaseFile",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "EmployeeCaseEvent",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "EmployeeCaseAccess",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "EmployeeCase",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "EmployeeAssignment",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Employee",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "EmailOutbox",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "DevItem",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "DevicePunch",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Department",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Delegation",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Contract",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "CompetencyTemplate",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "CompetencyItem",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "CompetencyApplication",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "CompanyAsset",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "CareerEvent",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "CandidateOfferEvent",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "CandidateOffer",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "CandidateInterview",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Candidate",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Branch",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "BenefitPlan",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "BenefitEnrollment",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "BenefitCancellation",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "AuditLog",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "AttendanceRecord",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "AttendancePeriodLock",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "AttendanceEvidence",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "AttendanceCorrection",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Attachment",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "AssetMovement",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "AppUser",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "AppSetting",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "ApprovalVote",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "ApprovalReminder",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "ApprovalAssignment",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "ApprovalAction",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "AdvanceRequest",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "Tenant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Revision = table.Column<string>(type: "text", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenant", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Tenant",
                columns: new[] { "Id", "CreatedAt", "Name", "Slug", "Status" },
                values: new object[] { 1, new DateTime(2026, 11, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "Demo company", "demo", "Active" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStep_TenantId",
                table: "WorkflowStep",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRevision_TenantId",
                table: "WorkflowRevision",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinition_TenantId",
                table: "WorkflowDefinition",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingOutcome_TenantId",
                table: "TrainingOutcome",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingEnrollment_TenantId",
                table: "TrainingEnrollment",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingCourse_TenantId",
                table: "TrainingCourse",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingCertificateAlert_TenantId",
                table: "TrainingCertificateAlert",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingCertificate_TenantId",
                table: "TrainingCertificate",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StatusHistory_TenantId",
                table: "StatusHistory",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StagedEmployeeRow_TenantId",
                table: "StagedEmployeeRow",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Shift_TenantId",
                table: "Shift",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryStructureApplication_TenantId",
                table: "SalaryStructureApplication",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryStructure_TenantId",
                table: "SalaryStructure",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryComponent_TenantId",
                table: "SalaryComponent",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermission_TenantId",
                table: "RolePermission",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Role_TenantId",
                table: "Role",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Role_TenantId_Code",
                table: "Role",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecruitmentEvent_TenantId",
                table: "RecruitmentEvent",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PipProgress_TenantId",
                table: "PipProgress",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PipMilestone_TenantId",
                table: "PipMilestone",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PerfReminder_TenantId",
                table: "PerfReminder",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceReview_TenantId",
                table: "PerformanceReview",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceResponse_TenantId",
                table: "PerformanceResponse",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PerfGoal_TenantId",
                table: "PerfGoal",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PerfCycle_TenantId",
                table: "PerfCycle",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PerfBonus_TenantId",
                table: "PerfBonus",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PerfAppraisal_TenantId",
                table: "PerfAppraisal",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRun_TenantId",
                table: "PayrollRun",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRun_TenantId_Year_Month",
                table: "PayrollRun",
                columns: new[] { "TenantId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollLine_TenantId",
                table: "PayrollLine",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollCalculationSnapshot_TenantId",
                table: "PayrollCalculationSnapshot",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollBankResult_TenantId",
                table: "PayrollBankResult",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAdjustmentEvent_TenantId",
                table: "PayrollAdjustmentEvent",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAdjustment_TenantId",
                table: "PayrollAdjustment",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordReset_TenantId",
                table: "PasswordReset",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_OvertimeRequest_TenantId",
                table: "OvertimeRequest",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_TenantId",
                table: "Notification",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_LoginSession_TenantId",
                table: "LoginSession",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_LifecycleTask_TenantId",
                table: "LifecycleTask",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveType_TenantId",
                table: "LeaveType",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveTransaction_TenantId",
                table: "LeaveTransaction",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequest_TenantId",
                table: "LeaveRequest",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalance_TenantId",
                table: "LeaveBalance",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_JobTitle_TenantId",
                table: "JobTitle",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_JobOpening_TenantId",
                table: "JobOpening",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationEvent_TenantId",
                table: "IntegrationEvent",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_HrFieldDefinition_TenantId",
                table: "HrFieldDefinition",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Holiday_TenantId",
                table: "Holiday",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedback360_TenantId",
                table: "Feedback360",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentChange_TenantId",
                table: "EmploymentChange",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeShiftDay_TenantId",
                table: "EmployeeShiftDay",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSalaryComponent_TenantId",
                table: "EmployeeSalaryComponent",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProbation_TenantId",
                table: "EmployeeProbation",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeImportBatch_TenantId",
                table: "EmployeeImportBatch",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeImportBatch_TenantId_FileHash",
                table: "EmployeeImportBatch",
                columns: new[] { "TenantId", "FileHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeGrievanceResponse_TenantId",
                table: "EmployeeGrievanceResponse",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeGrievanceAppealDecision_TenantId",
                table: "EmployeeGrievanceAppealDecision",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeGrievanceAppeal_TenantId",
                table: "EmployeeGrievanceAppeal",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeGrievance_TenantId",
                table: "EmployeeGrievance",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeFieldValue_TenantId",
                table: "EmployeeFieldValue",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDocument_TenantId",
                table: "EmployeeDocument",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDependent_TenantId",
                table: "EmployeeDependent",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCaseFile_TenantId",
                table: "EmployeeCaseFile",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCaseEvent_TenantId",
                table: "EmployeeCaseEvent",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCaseAccess_TenantId",
                table: "EmployeeCaseAccess",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCase_TenantId",
                table: "EmployeeCase",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAssignment_TenantId",
                table: "EmployeeAssignment",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_TenantId",
                table: "Employee",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_TenantId_Code",
                table: "Employee",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employee_TenantId_Email",
                table: "Employee",
                columns: new[] { "TenantId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailOutbox_TenantId",
                table: "EmailOutbox",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DevItem_TenantId",
                table: "DevItem",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DevicePunch_TenantId",
                table: "DevicePunch",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Department_TenantId",
                table: "Department",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Delegation_TenantId",
                table: "Delegation",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Contract_TenantId",
                table: "Contract",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetencyTemplate_TenantId",
                table: "CompetencyTemplate",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetencyItem_TenantId",
                table: "CompetencyItem",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetencyApplication_TenantId",
                table: "CompetencyApplication",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyAsset_TenantId",
                table: "CompanyAsset",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyAsset_TenantId_Tag",
                table: "CompanyAsset",
                columns: new[] { "TenantId", "Tag" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CareerEvent_TenantId",
                table: "CareerEvent",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateOfferEvent_TenantId",
                table: "CandidateOfferEvent",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateOffer_TenantId",
                table: "CandidateOffer",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateInterview_TenantId",
                table: "CandidateInterview",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Candidate_TenantId",
                table: "Candidate",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Branch_TenantId",
                table: "Branch",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BenefitPlan_TenantId",
                table: "BenefitPlan",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BenefitEnrollment_TenantId",
                table: "BenefitEnrollment",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BenefitCancellation_TenantId",
                table: "BenefitCancellation",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_TenantId",
                table: "AuditLog",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecord_TenantId",
                table: "AttendanceRecord",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendancePeriodLock_TenantId",
                table: "AttendancePeriodLock",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceEvidence_TenantId",
                table: "AttendanceEvidence",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceCorrection_TenantId",
                table: "AttendanceCorrection",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Attachment_TenantId",
                table: "Attachment",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetMovement_TenantId",
                table: "AssetMovement",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AppUser_TenantId",
                table: "AppUser",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSetting_TenantId",
                table: "AppSetting",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalVote_TenantId",
                table: "ApprovalVote",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalReminder_TenantId",
                table: "ApprovalReminder",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalAssignment_TenantId",
                table: "ApprovalAssignment",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalAction_TenantId",
                table: "ApprovalAction",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceRequest_TenantId",
                table: "AdvanceRequest",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenant_Slug",
                table: "Tenant",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tenant");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowStep_TenantId",
                table: "WorkflowStep");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowRevision_TenantId",
                table: "WorkflowRevision");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowDefinition_TenantId",
                table: "WorkflowDefinition");

            migrationBuilder.DropIndex(
                name: "IX_TrainingOutcome_TenantId",
                table: "TrainingOutcome");

            migrationBuilder.DropIndex(
                name: "IX_TrainingEnrollment_TenantId",
                table: "TrainingEnrollment");

            migrationBuilder.DropIndex(
                name: "IX_TrainingCourse_TenantId",
                table: "TrainingCourse");

            migrationBuilder.DropIndex(
                name: "IX_TrainingCertificateAlert_TenantId",
                table: "TrainingCertificateAlert");

            migrationBuilder.DropIndex(
                name: "IX_TrainingCertificate_TenantId",
                table: "TrainingCertificate");

            migrationBuilder.DropIndex(
                name: "IX_StatusHistory_TenantId",
                table: "StatusHistory");

            migrationBuilder.DropIndex(
                name: "IX_StagedEmployeeRow_TenantId",
                table: "StagedEmployeeRow");

            migrationBuilder.DropIndex(
                name: "IX_Shift_TenantId",
                table: "Shift");

            migrationBuilder.DropIndex(
                name: "IX_SalaryStructureApplication_TenantId",
                table: "SalaryStructureApplication");

            migrationBuilder.DropIndex(
                name: "IX_SalaryStructure_TenantId",
                table: "SalaryStructure");

            migrationBuilder.DropIndex(
                name: "IX_SalaryComponent_TenantId",
                table: "SalaryComponent");

            migrationBuilder.DropIndex(
                name: "IX_RolePermission_TenantId",
                table: "RolePermission");

            migrationBuilder.DropIndex(
                name: "IX_Role_TenantId",
                table: "Role");

            migrationBuilder.DropIndex(
                name: "IX_Role_TenantId_Code",
                table: "Role");

            migrationBuilder.DropIndex(
                name: "IX_RecruitmentEvent_TenantId",
                table: "RecruitmentEvent");

            migrationBuilder.DropIndex(
                name: "IX_PipProgress_TenantId",
                table: "PipProgress");

            migrationBuilder.DropIndex(
                name: "IX_PipMilestone_TenantId",
                table: "PipMilestone");

            migrationBuilder.DropIndex(
                name: "IX_PerfReminder_TenantId",
                table: "PerfReminder");

            migrationBuilder.DropIndex(
                name: "IX_PerformanceReview_TenantId",
                table: "PerformanceReview");

            migrationBuilder.DropIndex(
                name: "IX_PerformanceResponse_TenantId",
                table: "PerformanceResponse");

            migrationBuilder.DropIndex(
                name: "IX_PerfGoal_TenantId",
                table: "PerfGoal");

            migrationBuilder.DropIndex(
                name: "IX_PerfCycle_TenantId",
                table: "PerfCycle");

            migrationBuilder.DropIndex(
                name: "IX_PerfBonus_TenantId",
                table: "PerfBonus");

            migrationBuilder.DropIndex(
                name: "IX_PerfAppraisal_TenantId",
                table: "PerfAppraisal");

            migrationBuilder.DropIndex(
                name: "IX_PayrollRun_TenantId",
                table: "PayrollRun");

            migrationBuilder.DropIndex(
                name: "IX_PayrollRun_TenantId_Year_Month",
                table: "PayrollRun");

            migrationBuilder.DropIndex(
                name: "IX_PayrollLine_TenantId",
                table: "PayrollLine");

            migrationBuilder.DropIndex(
                name: "IX_PayrollCalculationSnapshot_TenantId",
                table: "PayrollCalculationSnapshot");

            migrationBuilder.DropIndex(
                name: "IX_PayrollBankResult_TenantId",
                table: "PayrollBankResult");

            migrationBuilder.DropIndex(
                name: "IX_PayrollAdjustmentEvent_TenantId",
                table: "PayrollAdjustmentEvent");

            migrationBuilder.DropIndex(
                name: "IX_PayrollAdjustment_TenantId",
                table: "PayrollAdjustment");

            migrationBuilder.DropIndex(
                name: "IX_PasswordReset_TenantId",
                table: "PasswordReset");

            migrationBuilder.DropIndex(
                name: "IX_OvertimeRequest_TenantId",
                table: "OvertimeRequest");

            migrationBuilder.DropIndex(
                name: "IX_Notification_TenantId",
                table: "Notification");

            migrationBuilder.DropIndex(
                name: "IX_LoginSession_TenantId",
                table: "LoginSession");

            migrationBuilder.DropIndex(
                name: "IX_LifecycleTask_TenantId",
                table: "LifecycleTask");

            migrationBuilder.DropIndex(
                name: "IX_LeaveType_TenantId",
                table: "LeaveType");

            migrationBuilder.DropIndex(
                name: "IX_LeaveTransaction_TenantId",
                table: "LeaveTransaction");

            migrationBuilder.DropIndex(
                name: "IX_LeaveRequest_TenantId",
                table: "LeaveRequest");

            migrationBuilder.DropIndex(
                name: "IX_LeaveBalance_TenantId",
                table: "LeaveBalance");

            migrationBuilder.DropIndex(
                name: "IX_JobTitle_TenantId",
                table: "JobTitle");

            migrationBuilder.DropIndex(
                name: "IX_JobOpening_TenantId",
                table: "JobOpening");

            migrationBuilder.DropIndex(
                name: "IX_IntegrationEvent_TenantId",
                table: "IntegrationEvent");

            migrationBuilder.DropIndex(
                name: "IX_HrFieldDefinition_TenantId",
                table: "HrFieldDefinition");

            migrationBuilder.DropIndex(
                name: "IX_Holiday_TenantId",
                table: "Holiday");

            migrationBuilder.DropIndex(
                name: "IX_Feedback360_TenantId",
                table: "Feedback360");

            migrationBuilder.DropIndex(
                name: "IX_EmploymentChange_TenantId",
                table: "EmploymentChange");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeShiftDay_TenantId",
                table: "EmployeeShiftDay");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeSalaryComponent_TenantId",
                table: "EmployeeSalaryComponent");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeProbation_TenantId",
                table: "EmployeeProbation");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeImportBatch_TenantId",
                table: "EmployeeImportBatch");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeImportBatch_TenantId_FileHash",
                table: "EmployeeImportBatch");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeGrievanceResponse_TenantId",
                table: "EmployeeGrievanceResponse");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeGrievanceAppealDecision_TenantId",
                table: "EmployeeGrievanceAppealDecision");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeGrievanceAppeal_TenantId",
                table: "EmployeeGrievanceAppeal");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeGrievance_TenantId",
                table: "EmployeeGrievance");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeFieldValue_TenantId",
                table: "EmployeeFieldValue");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeDocument_TenantId",
                table: "EmployeeDocument");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeDependent_TenantId",
                table: "EmployeeDependent");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeCaseFile_TenantId",
                table: "EmployeeCaseFile");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeCaseEvent_TenantId",
                table: "EmployeeCaseEvent");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeCaseAccess_TenantId",
                table: "EmployeeCaseAccess");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeCase_TenantId",
                table: "EmployeeCase");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeAssignment_TenantId",
                table: "EmployeeAssignment");

            migrationBuilder.DropIndex(
                name: "IX_Employee_TenantId",
                table: "Employee");

            migrationBuilder.DropIndex(
                name: "IX_Employee_TenantId_Code",
                table: "Employee");

            migrationBuilder.DropIndex(
                name: "IX_Employee_TenantId_Email",
                table: "Employee");

            migrationBuilder.DropIndex(
                name: "IX_EmailOutbox_TenantId",
                table: "EmailOutbox");

            migrationBuilder.DropIndex(
                name: "IX_DevItem_TenantId",
                table: "DevItem");

            migrationBuilder.DropIndex(
                name: "IX_DevicePunch_TenantId",
                table: "DevicePunch");

            migrationBuilder.DropIndex(
                name: "IX_Department_TenantId",
                table: "Department");

            migrationBuilder.DropIndex(
                name: "IX_Delegation_TenantId",
                table: "Delegation");

            migrationBuilder.DropIndex(
                name: "IX_Contract_TenantId",
                table: "Contract");

            migrationBuilder.DropIndex(
                name: "IX_CompetencyTemplate_TenantId",
                table: "CompetencyTemplate");

            migrationBuilder.DropIndex(
                name: "IX_CompetencyItem_TenantId",
                table: "CompetencyItem");

            migrationBuilder.DropIndex(
                name: "IX_CompetencyApplication_TenantId",
                table: "CompetencyApplication");

            migrationBuilder.DropIndex(
                name: "IX_CompanyAsset_TenantId",
                table: "CompanyAsset");

            migrationBuilder.DropIndex(
                name: "IX_CompanyAsset_TenantId_Tag",
                table: "CompanyAsset");

            migrationBuilder.DropIndex(
                name: "IX_CareerEvent_TenantId",
                table: "CareerEvent");

            migrationBuilder.DropIndex(
                name: "IX_CandidateOfferEvent_TenantId",
                table: "CandidateOfferEvent");

            migrationBuilder.DropIndex(
                name: "IX_CandidateOffer_TenantId",
                table: "CandidateOffer");

            migrationBuilder.DropIndex(
                name: "IX_CandidateInterview_TenantId",
                table: "CandidateInterview");

            migrationBuilder.DropIndex(
                name: "IX_Candidate_TenantId",
                table: "Candidate");

            migrationBuilder.DropIndex(
                name: "IX_Branch_TenantId",
                table: "Branch");

            migrationBuilder.DropIndex(
                name: "IX_BenefitPlan_TenantId",
                table: "BenefitPlan");

            migrationBuilder.DropIndex(
                name: "IX_BenefitEnrollment_TenantId",
                table: "BenefitEnrollment");

            migrationBuilder.DropIndex(
                name: "IX_BenefitCancellation_TenantId",
                table: "BenefitCancellation");

            migrationBuilder.DropIndex(
                name: "IX_AuditLog_TenantId",
                table: "AuditLog");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecord_TenantId",
                table: "AttendanceRecord");

            migrationBuilder.DropIndex(
                name: "IX_AttendancePeriodLock_TenantId",
                table: "AttendancePeriodLock");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceEvidence_TenantId",
                table: "AttendanceEvidence");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceCorrection_TenantId",
                table: "AttendanceCorrection");

            migrationBuilder.DropIndex(
                name: "IX_Attachment_TenantId",
                table: "Attachment");

            migrationBuilder.DropIndex(
                name: "IX_AssetMovement_TenantId",
                table: "AssetMovement");

            migrationBuilder.DropIndex(
                name: "IX_AppUser_TenantId",
                table: "AppUser");

            migrationBuilder.DropIndex(
                name: "IX_AppSetting_TenantId",
                table: "AppSetting");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalVote_TenantId",
                table: "ApprovalVote");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalReminder_TenantId",
                table: "ApprovalReminder");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalAssignment_TenantId",
                table: "ApprovalAssignment");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalAction_TenantId",
                table: "ApprovalAction");

            migrationBuilder.DropIndex(
                name: "IX_AdvanceRequest_TenantId",
                table: "AdvanceRequest");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "WorkflowStep");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "WorkflowRevision");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "WorkflowDefinition");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "TrainingOutcome");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "TrainingEnrollment");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "TrainingCourse");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "TrainingCertificateAlert");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "TrainingCertificate");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "StatusHistory");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "StagedEmployeeRow");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Shift");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "SalaryStructureApplication");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "SalaryStructure");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "SalaryComponent");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "RolePermission");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Role");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "RecruitmentEvent");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PipProgress");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PipMilestone");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PerfReminder");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PerformanceReview");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PerformanceResponse");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PerfGoal");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PerfCycle");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PerfBonus");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PerfAppraisal");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PayrollRun");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PayrollLine");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PayrollCalculationSnapshot");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PayrollBankResult");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PayrollAdjustmentEvent");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PayrollAdjustment");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PasswordReset");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "OvertimeRequest");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Notification");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "LoginSession");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "LifecycleTask");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "LeaveType");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "LeaveTransaction");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "LeaveRequest");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "LeaveBalance");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "JobTitle");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "JobOpening");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "IntegrationEvent");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "HrFieldDefinition");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Holiday");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Feedback360");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "EmploymentChange");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "EmployeeShiftDay");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "EmployeeSalaryComponent");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "EmployeeProbation");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "EmployeeImportBatch");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "EmployeeGrievanceResponse");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "EmployeeGrievanceAppealDecision");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "EmployeeGrievanceAppeal");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "EmployeeGrievance");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "EmployeeFieldValue");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "EmployeeDocument");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "EmployeeDependent");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "EmployeeCaseFile");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "EmployeeCaseEvent");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "EmployeeCaseAccess");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "EmployeeCase");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "EmployeeAssignment");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "EmailOutbox");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "DevItem");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "DevicePunch");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Department");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Delegation");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Contract");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CompetencyTemplate");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CompetencyItem");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CompetencyApplication");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CompanyAsset");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CareerEvent");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CandidateOfferEvent");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CandidateOffer");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CandidateInterview");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Candidate");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Branch");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "BenefitPlan");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "BenefitEnrollment");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "BenefitCancellation");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AuditLog");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AttendanceRecord");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AttendancePeriodLock");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AttendanceEvidence");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AttendanceCorrection");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Attachment");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AssetMovement");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AppUser");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AppSetting");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ApprovalVote");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ApprovalReminder");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ApprovalAssignment");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ApprovalAction");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AdvanceRequest");

            migrationBuilder.CreateIndex(
                name: "IX_Role_Code",
                table: "Role",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRun_Year_Month",
                table: "PayrollRun",
                columns: new[] { "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeImportBatch_FileHash",
                table: "EmployeeImportBatch",
                column: "FileHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employee_Code",
                table: "Employee",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employee_Email",
                table: "Employee",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyAsset_Tag",
                table: "CompanyAsset",
                column: "Tag",
                unique: true);
        }
    }
}
