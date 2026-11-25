> **PROPOSED DRAFT, NOT APPROVED. DOCS ONLY.** Stories derived mechanically from `BRD-Phase-1-Foundation-and-Production-Readiness.md` (itself an unapproved draft from the ChatGPT evaluation conversation, https://chatgpt.com/share/6b06e8ab-1b60-83ea-b893-80f4e267ce25). Not in scope for current BRD v3 work.
>
> **Assumptions to confirm:** (1) Ahmed's WhatsApp voice note of 2026-11-25 15:58 EET was partly garbled; it was read as "split the three phase BRDs into stories and keep them in the repo until current work is finished" and as meaning stories with their acceptance criteria. (2) No story actors were invented: each story keeps the draft's own "The system shall..." statement, its acceptance criteria and the phase-level priority (Critical); the draft carries no per-requirement priority, estimate or owner. (3) Story and requirement numbering follow the draft: story `US-PH1-NNN` maps to `PH1-BR-NNN`. (4) Before any phase starts, gap-analyse each story against `docs/TRACEABILITY.md` and the code, since many describe features that already exist in part.

> **Country pack note (proposed, Ahmed direction 2026-11-25):** stories in this phase must specify the Egypt country pack values they depend on and read them from the active pack, not hardcode them. See `COUNTRY-PACKS-Addendum.md`. Draft, not approved.


# User stories: Phase 1 (Critical)

Source BRD: `BRD-Phase-1-Foundation-and-Production-Readiness.md`

## US-PH1-001: Tenant lifecycle

- Source requirement: PH1-BR-001
- Priority: Critical (phase-level)
- Status: proposed, not started

**Story:** The system shall support the creation, activation, suspension, and deactivation of customer tenants.

**Acceptance criteria**
- A platform administrator can create a tenant with a unique tenant identifier.
- A tenant can be activated or suspended without changing another tenant's state.
- Suspended tenants cannot perform normal business operations.
- Tenant lifecycle actions are audited with actor, timestamp, and reason.
- Tenant deletion follows an approved retention and legal-hold policy; deletion is not an unrestricted hard delete.

## US-PH1-002: Tenant data isolation

- Source requirement: PH1-BR-002
- Priority: Critical (phase-level)
- Status: proposed, not started

**Story:** The system shall enforce tenant isolation for all tenant-owned data and operations.

**Acceptance criteria**
- Every tenant-scoped request resolves a trusted tenant context.
- Users cannot select or override another tenant identifier to gain access.
- Tenant filters are enforced at the data-access boundary, not only in the UI.
- Unique business constraints are tenant-aware where appropriate.
- Background jobs, exports, reports, file access, and integrations execute within an explicit tenant context.
- Automated tests prove that cross-tenant reads, writes, updates, deletes, and indirect-reference access are denied.

## US-PH1-003: Tenant-aware identity and authorization

- Source requirement: PH1-BR-003
- Priority: Critical (phase-level)
- Status: proposed, not started

**Story:** The system shall support user membership and authorization within the correct tenant context.

**Acceptance criteria**
- A user can belong to one or more tenants only if the approved identity model permits it.
- Tenant membership is validated on every authenticated tenant-scoped operation.
- Roles and permissions can be configured per tenant.
- Platform administration permissions are separate from customer administration permissions.
- Permission changes and privileged actions are auditable.
- Authorization is enforced server-side for APIs, exports, downloads, and background operations.

## US-PH1-004: Tenant-specific configuration

- Source requirement: PH1-BR-004
- Priority: Critical (phase-level)
- Status: proposed, not started

**Story:** The system shall allow approved settings to vary by tenant without code changes.

**Acceptance criteria**
- Each tenant can configure its approved work calendar, time zone, currency, leave settings, attendance settings, and workflow definitions.
- Configuration changes have effective dates or version history where they affect historical calculations.
- Defaults are explicit and documented.
- One tenant's configuration cannot affect another tenant.

## US-PH1-005: Sensitive-data protection

- Source requirement: PH1-BR-005
- Priority: Critical (phase-level)
- Status: proposed, not started

**Story:** The system shall protect sensitive employee, identity, payroll, authentication, and document data.

**Acceptance criteria**
- Sensitive data is classified and assigned access rules.
- Sensitive data is encrypted in transit and protected at rest using approved platform controls.
- Secrets and TOTP seeds are not stored in plaintext.
- Encryption keys and application secrets are managed outside source control and are rotatable.
- File upload validation includes content/type checks, size limits, safe storage, and malware scanning or an approved compensating control.
- Downloads require authorization and are audited for sensitive document categories.
- Logs do not expose passwords, tokens, TOTP seeds, or unnecessary personal/financial data.

## US-PH1-006: Authentication and session security

- Source requirement: PH1-BR-006
- Priority: Critical (phase-level)
- Status: proposed, not started

**Story:** The system shall provide secure authentication and session lifecycle controls.

**Acceptance criteria**
- Authentication endpoints have rate limiting and abuse protection.
- Password and MFA secrets follow approved storage practices.
- Users and administrators can revoke active sessions according to their permissions.
- Session inventory records the minimum necessary device/session metadata.
- Account lockout or progressive throttling policies are configurable and tested.
- Security-sensitive actions require appropriate re-authentication or MFA where policy requires it.

## US-PH1-007: Payroll financial precision

- Source requirement: PH1-BR-007
- Priority: Critical (phase-level)
- Status: proposed, not started

**Story:** The system shall use precise decimal arithmetic for all monetary calculations.

**Acceptance criteria**
- Monetary values and intermediate financial calculations use `decimal` or an equivalent exact decimal representation.
- Rounding rules are explicit, versioned, and applied consistently.
- Currency precision is configurable according to the applicable currency and policy.
- No binary floating-point conversion is used in payroll calculation paths.
- Regression tests cover rounding boundaries, negative adjustments, proration, and large values.
- Historical finalized payroll results remain reproducible from their recorded inputs and policy versions.

## US-PH1-008: Versioned payroll policies and calculation traceability

- Source requirement: PH1-BR-008
- Priority: Critical (phase-level)
- Status: proposed, not started

**Story:** The system shall preserve the policy inputs used to calculate each payroll result.

**Acceptance criteria**
- Each payroll calculation references a specific policy/version set.
- Effective-dated rules are selected based on the applicable payroll period and employee context.
- Calculation results retain input references, applied rules, rounding decisions, and calculation version.
- Finalized payroll is immutable through normal business operations.
- Corrections are represented through traceable adjustment or reversal records.
- Changes to future policies do not silently change historical payroll results.

## US-PH1-009: Payroll control and approval integrity

- Source requirement: PH1-BR-009
- Priority: Critical (phase-level)
- Status: proposed, not started

**Story:** The system shall enforce separation of duties for payroll preparation, review, approval, and finalization.

**Acceptance criteria**
- The configured preparer cannot approve their own payroll where separation of duties is required.
- Approval authority is checked server-side.
- Finalization is protected against duplicate execution.
- Rejected or returned payroll runs retain their decision history.
- All material payroll actions are auditable.
- Payroll export and bank-file generation are permission-controlled and logged.

## US-PH1-010: Durable background processing

- Source requirement: PH1-BR-010
- Priority: Critical (phase-level)
- Status: proposed, not started

**Story:** The system shall execute scheduled and asynchronous operations reliably.

**Acceptance criteria**
- Jobs survive application restarts and deployment cycles.
- Job execution is protected against unintended concurrent processing.
- Retries use bounded policies and distinguish transient from permanent failures.
- Job handlers are idempotent or use a deduplication mechanism.
- Failed jobs are observable and can be safely reprocessed by authorized operators.
- Jobs execute with an explicit tenant and correlation context.
- Dead-letter or equivalent failure handling is available for unrecoverable messages.

## US-PH1-011: Operational observability

- Source requirement: PH1-BR-011
- Priority: Critical (phase-level)
- Status: proposed, not started

**Story:** The system shall provide sufficient telemetry to detect and diagnose production issues.

**Acceptance criteria**
- Structured logs include correlation identifiers and relevant tenant context without leaking sensitive data.
- Health checks cover application readiness and critical dependencies.
- Metrics cover request failures, latency, job failures, queue depth, and key business processing outcomes.
- Alerts are defined for critical service and job failures.
- Support staff can trace a business operation across relevant application components.

## US-PH1-012: Backup and recovery

- Source requirement: PH1-BR-012
- Priority: Critical (phase-level)
- Status: proposed, not started

**Story:** The system shall support tested backup and restore procedures.

**Acceptance criteria**
- Database and file-storage backup schedules are documented and automated.
- Backups are encrypted and stored outside the primary failure domain.
- Retention periods are configurable and documented.
- Restore tests are performed on a defined schedule.
- Recovery Point Objective (RPO) and Recovery Time Objective (RTO) are agreed and measured.
- Backup and restore outcomes are logged and reviewed.

## US-PH1-013: Release and deployment governance

- Source requirement: PH1-BR-013
- Priority: Critical (phase-level)
- Status: proposed, not started

**Story:** The system shall support controlled and repeatable production releases.

**Acceptance criteria**
- CI/CD includes build, automated tests, security checks, and migration validation.
- Database migrations have reviewed deployment scripts and rollback/forward-recovery guidance.
- Environment-specific secrets are not embedded in artifacts.
- Releases support health verification and documented rollback or recovery procedures.
- Production configuration changes are auditable.
