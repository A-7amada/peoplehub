# Backlog audit: all user stories vs code and tests (2026-11-27)

Scope: the 60 user stories in `docs/roadmap-drafts/USER-STORIES-Phase-1.md`, `-Phase-2.md`, `-Phase-3.md` and `-Subscriptions-Billing.md`, checked against the code, tests and migrations on the repo HEAD, with TRACEABILITY.md and ADRs for the requirement-level detail. Evidence names the source or the test class that covers it.

**Counts: 1 done, 41 partial, 18 not done (total 60).** Done means every acceptance criterion is met. Partial means part is built and tested and the remainder is listed. I confirmed the built part per story by reading the code and test names; I did not re-run each acceptance criterion as a separate test, and the stories were drafted as proposals (the story files say not approved), so the owner-approved scope is the decisions recorded in ADRs and TRACEABILITY.md.

Reason codes: FREE = free and decision-free (or already approved), can be built now. OWNER = needs Ahmed's decision. PAID = needs spend. DEFERRED = Ahmed deferred it (no paid infrastructure). SPECIALIST = needs labour lawyer or payroll accountant review. EXTERNAL = needs a contract, credentials or hardware.

| Story | Name | Status | Evidence | Remaining and reason |
|---|---|---|---|---|
| US-PH1-001 | Tenant lifecycle | Partial | TenantProvisioner/TenantAdminService, /Platform UI, migration PlatformUsers; TenantProvisioningTests, PlatformAuthTests | Lifecycle audit DONE (unit 1, 2026-11-28): PlatformAuditLog records actor, UTC time, company and a required reason (3 to 500 chars) for create, suspend, reactivate and archive; shown in an audit trail on /Platform; platform-staff only; tests in TenantProvisioningTests. REMAINING: OWNER-type default: retention and legal-hold policy for deletion (SEC-007); nothing is deleted today. |
| US-PH1-002 | Tenant data isolation | Partial | AppDbContext query filters + stamping; TenantAuditTests, TenantIsolationTests, MaintenanceIsolationTests; per-tenant unique indexes | FREE (approved, ADR-0023): stage 4 per-tenant settings cache, reset links per tenant, files and scheduler audit; stage 5 Postgres RLS. Postgres isolation not exercised yet. |
| US-PH1-003 | Tenant-aware identity and authorization | Partial | tenant_id claim, per-tenant AppUser email, platform vs customer sign-in split; TenantIsolationTests, PlatformAuthTests | FREE: platform staff 2FA, reset flow per tenant. DEFERRED/OWNER: SSO (OIDC provider). |
| US-PH1-004 | Tenant-specific configuration | Partial | Settings, workflows, leave types are tenant rows (TenantAuditTests same-key test); new tenants get their own roles, workflows, leave types | FREE (stage 4): SettingsService static cache is not tenant-keyed, holidays/shifts defaults per new tenant. |
| US-PH1-005 | Sensitive-data protection | Partial | AES-256-GCM field encryption (ADR-0021; FieldEncryptionTests, SensitiveFieldTests), field grants, private files | OWNER: retention/deletion policy (SEC-007), key custody and rotation. PAID/external: malware scanner (SEC-008). |
| US-PH1-006 | Authentication and session security | Partial | Sessions, 5-failure lock, TOTP with replay guard and recovery codes, step-up; SessionTests, AuthThrottlingTests, StepUpTests | FREE: platform 2FA. OWNER/DEFERRED: SSO, device binding, anomaly detection policy. |
| US-PH1-007 | Payroll financial precision | Done | Migration DecimalMoney (decimal(18,2) on Postgres), ADR-0015; Postgres.PostgresDecimalTests (skipped locally, runs in CI) | None. Restore drill and encrypted backup done (SEC-009). |
| US-PH1-008 | Versioned payroll policies and calculation traceability | Partial | Immutable inputs, policy version, hash; PayrollTests, PayrollPackResolutionTests, CountryPackTests | SPECIALIST: multiple effective-dated policy versions and sign-off need payroll accountant/labour lawyer review (PAY-013, PAY-006). |
| US-PH1-009 | Payroll control and approval integrity | Partial | PayrollControlIntegrityTests (9), separate final approver, append-only audit | OWNER: separation of duties at FinanceApprove (open decision). |
| US-PH1-010 | Durable background processing | Partial | ScheduledJobState with DB lease and run records (ARC-004); ScheduledJobTests | PAID/OWNER: an external scheduler that runs while the free host sleeps (Render Starter). Free-tier limit stays. |
| US-PH1-011 | Operational observability | Partial | Health endpoints, request metrics, OpenMetrics formatter, correlation IDs; OpenMetricsTests, RequestMetricsTests, OperationsProjectionTests | PAID/OWNER: durable telemetry, alerting, tracing service. |
| US-PH1-012 | Backup and recovery | Partial | Daily GPG-encrypted pg_dump from a private repo workflow, weekly restore drill (SEC-009, BLUEPRINT) | OWNER: RPO/RTO targets and key custody; off-site funded storage is not required today. |
| US-PH1-013 | Release and deployment governance | Partial | .github/workflows/ci.yml builds and runs the suite on every push; migrations for both providers; docs per unit | OWNER/PAID: staging environment (docs/STAGING.md), automated deploy approval. Deploys stay manual by design. |
| US-PH2-001 | Leave accrual policies | Partial | LeavePolicyService, admin page, annual policy sets opening entitlement; LeavePolicyTests | FREE (approved decision 1): monthly accrual, proration, eligibility tiers applied, preview per employee; needs decimal balances. |
| US-PH2-002 | Hourly and partial-day leave | Not done | None (LEV-002) | FREE (approved decision 1): half-day and hourly units with rounding; needs decimal balances first. |
| US-PH2-003 | Leave carry-forward and expiry | Partial | Policy fields and MaxCarryOver exist; not applied at year end | FREE (approved decision 1): apply carry-forward and expiry at rollover with audit. |
| US-PH2-004 | Leave encashment | Partial | Settlement estimate (LEV-005) | OWNER/SPECIALIST: payout rules and approval cycle. |
| US-PH2-005 | Attendance policy and exception completion | Partial | Missing-punch inbox, corrections, month lock, flexible hours, rotating shifts, holiday calendars; AttendanceExceptionTests, ShiftRotationTests, HolidayCalendarTests | FREE: category grace periods (ATT-010) as configurable values, unset by default. |
| US-PH2-006 | Mobile attendance capture | Not done | None (ATT-006, ENT-005) | DEFERRED by owner 2026-11-25 (no paid infrastructure); GPS. |
| US-PH2-007 | Biometric device integration | Partial | ZKTeco ingestion, append-only punches, idempotent processing, device bridge; AttendanceEvidenceTests | EXTERNAL: not tested on real hardware. |
| US-PH2-008 | Overtime management | Partial | OvertimeRequest with approval, weighted hours into payroll | FREE: configurable caps and rates per category (unset defaults). OWNER: values. |
| US-PH2-009 | Payroll proration | Partial | Settlement proration fixed (PAY-003) | SPECIALIST/OWNER: regular payroll proration and terminal-period eligibility (PAY-002, PAY-003). |
| US-PH2-010 | Salary structures and compensation changes | Partial | SalaryStructures.cs, SalaryStructureTests | FREE: allowance bands and draft/publish lifecycle. |
| US-PH2-011 | Retroactive payroll adjustments | Partial | PayrollAdjustments.cs, PayrollAdjustmentTests | OWNER: automatic multi-period retro recompute is deliberately not built. |
| US-PH2-012 | Egypt payroll localization package | Partial | CountryPacks.cs, pack EG-DEMO-2026-v1 (illustrative); CountryPackTests | SPECIALIST: legal validation, deduction priority (PAY-006). Law 12/2003 tiers are illustrative; replaced by Law 14/2025. |
| US-PH2-013 | End-of-service and termination settlement | Partial | Estimator and view (PAY-012); SettlementRegressionTests | OWNER/SPECIALIST: approval and payout. |
| US-PH2-014 | Recruitment workflow | Partial | Requisitions, pipeline, interviews, offers; RecruitmentRequisitionTests, RecruitmentInterviewTests, RecruitmentOfferTests | FREE: CV file upload (files in DB). EXTERNAL/PAID: CV parsing, external job portal. |
| US-PH2-015 | Benefits administration | Partial | BenefitService, plans, enrolment, allocation; BenefitTests, BenefitAllocationTests | FREE: claims record. OWNER: payroll linkage. |
| US-PH2-016 | Employee self-service | Partial | Profile, leave, attendance, advance, overtime, documents, grievances, letters; LetterTests | FREE: small remaining self-service views. |
| US-PH2-017 | Mobile application or responsive experience | Partial | Responsive CSS, tests/browser/mobile-viewport.cjs at 390px, EN/AR | DEFERRED/PAID: native mobile app. Design phase will improve the responsive look. |
| US-PH2-018 | Notification delivery management | Partial | In-app notifications, email outbox with delivery states; MailDeliveryStatusTests | OWNER: Brevo credential reset (live email stays off). PAID: push/SMS. |
| US-PH2-019 | HR operational reports | Partial | ReportService, ReportBuilderService, headcount/turnover, approval statistics; HeadcountReportTests, SpreadsheetExportTests | FREE: further report types on request. |
| US-PH3-001 | Integration catalog and administration | Partial | IntegrationOutbox.cs (generic outbox); IntegrationOutboxTests | FREE: registry/admin page. EXTERNAL: concrete adapters need contracts and credentials. |
| US-PH3-002 | Integration execution and reconciliation | Partial | Outbox with retry, dead-letter, reconciliation counts; BankReconciliationTests (manual evidence) | EXTERNAL: real adapters and scheduled worker (ARC-004). |
| US-PH3-003 | Enterprise identity integration | Not done | None (ENT-004) | DEFERRED by owner: needs an OIDC provider. |
| US-PH3-004 | Finance and accounting integration | Partial | Cost-centre journal projection and CSV; PayrollJournalTests | EXTERNAL/DEFERRED: account mappings and posting to an accounting system. |
| US-PH3-005 | Banking and payment integration | Not done | Manual reconciliation evidence only | DEFERRED by owner (ENT-003): bank files/APIs need contracts. |
| US-PH3-006 | Communication and device integrations | Partial | Email outbox, ZKTeco bridge | PAID: SMS and push providers. |
| US-PH3-007 | Workflow service levels and escalation | Partial | ApprovalReminders (WFL-001); ApprovalReminderTests | FREE (decision on escalation target is a default 'next approver or HR' to confirm): escalation and business-hours calendar. |
| US-PH3-008 | Advanced delegation and approval governance | Partial | Delegation, reassignment, quorum, versioned definitions; DelegationTests, ApprovalGroupTests, WorkflowRevisionTests | FREE: draft/publish lifecycle and version comparison. |
| US-PH3-009 | Workflow simulation and analytics | Partial | Approval statistics report; ApprovalStatsReportTests | FREE: workflow simulation (what-if route preview). |
| US-PH3-010 | Executive HR analytics | Partial | Headcount, turnover, payroll cost trend | DEFERRED (ENT-006): forecasting. |
| US-PH3-011 | Workforce cost and headcount planning | Partial | Target headcount, training cost summary, cost-centre projection | FREE: planning module (cost per target headcount); DEFERRED by owner ENT-006 is for forecasting/AI. |
| US-PH3-012 | Skills and competency intelligence | Not done | GradeCompetencyTests: frameworks per grade only | FREE: skills inventory and gap view. |
| US-PH3-013 | Succession and talent planning | Not done | None | DEFERRED by owner (ENT-006). |
| US-PH3-014 | AI-assisted HR knowledge and policy search | Not done | None | DEFERRED/PAID: needs a model service. |
| US-PH3-015 | Payroll and attendance anomaly detection | Not done | None; source data exists | OWNER: thresholds are policy; rule-based version is FREE once thresholds are approved. |
| US-PH3-016 | Workforce forecasting | Not done | None | DEFERRED by owner (ENT-006). |
| US-PH3-017 | AI governance and human oversight | Not done | None | DEFERRED with AI items (no AI component exists). |
| US-PH3-018 | Country policy packages | Partial | Pack interface and Egypt pack in Domain/Modules/Payroll/CountryPacks.cs; CountryPackTests, PayrollPackResolutionTests | SPECIALIST: second country packs and validation; leave and attendance rules not yet pack-based. |
| US-SUB-001 | Plan catalogue | Not done | None | OWNER: prices and entitlements; FREE after that (ADR-0023 stage 6). |
| US-SUB-002 | Entitlement enforcement | Not done | None | FREE after plans are decided (stage 6). |
| US-SUB-003 | Subscription lifecycle | Partial | Tenant lifecycle and /Platform console (stage 3) | Tenant suspend/reactivate exists (stage 3); billing states need stage 6 and owner pricing decisions. |
| US-SUB-004 | Usage metering | Not done | None | FREE: active-employee count per tenant, immutable monthly record (stage 6). |
| US-SUB-005 | Invoicing | Not done | None | SPECIALIST: tax lines and legal invoice format; OWNER: numbering. |
| US-SUB-006 | Payment collection | Not done | None | PAID/OWNER: a payment provider; manual bank-transfer record is FREE. |
| US-SUB-007 | Plan change | Not done | None | OWNER: proration rule. |
| US-SUB-008 | Platform admin console | Partial | Tenant lifecycle and /Platform console (stage 3) | Partial base: /Platform manages companies; plans, discounts, overrides need stage 6. FREE after decisions. |
| US-SUB-009 | Suspension and exit | Not done | None | OWNER: export window and deletion policy (SEC-007). |
| US-SUB-010 | Billing isolation | Not done | None | FREE with stage 6; tenant isolation tests exist for existing data. |

## Finish order for the free items (small pushed units)
1. US-PH1-001 lifecycle audit with reason: built 2026-11-28 (story stays Partial until the retention default is recorded). Next: ADR-0023 stages 4 and 5.
2. ADR-0023 stage 4: per-tenant settings cache and tenant reset links; stage 5 Postgres RLS.
3. ADR-0022 units 3+: decimal leave balances, then half-day/hourly, monthly accrual with proration, carry-forward and expiry at rollover (US-PH2-001, 002, 003).
4. US-PH2-010 salary allowance bands and publication; US-PH3-008 draft/publish and version comparison; US-PH3-009 simulation; US-PH3-007 SLA escalation (target confirmation).
5. US-PH2-005 category grace, US-PH2-008 overtime caps (configurable, unset defaults); US-PH3-012 skills inventory; US-PH3-001 integration registry; US-PH2-014 CV upload; US-PH2-015 claims record.

## Held (not built without a decision or spend)
Subscriptions and billing (US-SUB-001 to 010) wait for pricing and plan decisions and ADR-0023 stage 6. Payroll and leave-encashment legal rules wait for specialist review. AI, forecasting, succession, SSO, bank and accounting integrations, GPS and native mobile stay deferred. Render Starter, staging, a payment provider, SMS/push and a malware scanner are spend items.

## Progress log
- 2026-11-28 unit 1: US-PH1-001 lifecycle audit built (story stays Partial until the retention default is recorded).
- 2026-11-28 unit 2: ADR-0023 stage 4 built (per-company settings cache, company-scoped reset links). Stage 5 RLS script written but unapplied and unverified (see ADR-0023).
