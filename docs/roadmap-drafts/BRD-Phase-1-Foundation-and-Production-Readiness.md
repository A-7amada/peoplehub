> **PROPOSED DRAFT, NOT APPROVED.** Generated in a ChatGPT evaluation conversation (https://chatgpt.com/share/6b06e8ab-1b60-83ea-b893-80f4e267ce25, 2026-11-25). It is not part of BRD v3, has not been reviewed against the repository, and is not in scope for current work. Per Ahmed Hamada's WhatsApp voice note of 2026-11-25 15:57 EET: take no action on the evaluation itself, keep these drafts in the repo, and apply the architecture step first once the current BRD v3 work is finished.

# BRD — Phase 1: Foundation and Production Readiness

**Product:** PeopleHub  
**Phase:** 1 of 3  
**Priority:** Critical  
**Document status:** Proposed  
**Primary outcome:** Establish a secure, reliable, tenant-aware foundation that can support real customer organizations and production HR operations.

---

## 1. Executive Summary

PeopleHub has a broad HR functional foundation. Before expanding the feature set, the product needs stronger tenant isolation, security controls, financial correctness, background processing reliability, and production operations.

This phase focuses on product trustworthiness rather than adding a large number of visible HR features. It establishes the platform capabilities required by the later phases and reduces the risk of cross-company data exposure, incorrect payroll results, lost scheduled work, and unrecoverable production incidents.

## 2. Business Objectives

1. Enable multiple customer organizations to use the same PeopleHub deployment without accessing one another's data.
2. Protect employee, payroll, identity, and document data.
3. Ensure payroll calculations use precise financial types and traceable, versioned policies.
4. Make scheduled and asynchronous work reliable, observable, and safe to retry.
5. Establish repeatable deployment, backup, restore, monitoring, and incident-response processes.
6. Provide evidence that the platform is ready for controlled production onboarding.

## 3. Scope

### In scope

- Tenant model and tenant context propagation.
- Tenant-aware authentication, authorization, and data access.
- Tenant-specific roles, configuration, policies, workflows, and organizational settings.
- Sensitive-data protection and security hardening.
- Financial precision and payroll calculation integrity.
- Durable background processing and idempotent job execution.
- Logging, monitoring, health checks, backups, restore testing, deployment, and rollback.
- Automated security, integration, concurrency, and recovery tests.

### Out of scope

- Full implementation of new recruitment, benefits, or employee mobile features.
- Advanced AI or predictive analytics.
- Full enterprise integration marketplace.
- Microservices migration.
- Country expansion beyond the initially approved localization scope.
- Production payroll certification without specialist legal/payroll validation.

## 4. Stakeholders and User Classes

| Stakeholder | Interest / responsibility |
|---|---|
| SaaS Platform Administrator | Platform-level tenant lifecycle and operational administration |
| Customer Administrator | Company configuration, users, roles, and policies within their tenant |
| HR Administrator | Employee and HR operations within the tenant |
| Payroll Specialist | Payroll configuration, calculation, review, and processing |
| Finance Approver | Financial approval and reconciliation |
| Employee | Access to own permitted information and requests |
| Security / Compliance Officer | Access reviews, audit, retention, and security evidence |
| Operations / Support | Monitoring, backups, incident handling, and deployment |

## 5. Business Requirements

### PH1-BR-001 — Tenant lifecycle

The system shall support the creation, activation, suspension, and deactivation of customer tenants.

**Acceptance criteria**
- A platform administrator can create a tenant with a unique tenant identifier.
- A tenant can be activated or suspended without changing another tenant's state.
- Suspended tenants cannot perform normal business operations.
- Tenant lifecycle actions are audited with actor, timestamp, and reason.
- Tenant deletion follows an approved retention and legal-hold policy; deletion is not an unrestricted hard delete.

### PH1-BR-002 — Tenant data isolation

The system shall enforce tenant isolation for all tenant-owned data and operations.

**Acceptance criteria**
- Every tenant-scoped request resolves a trusted tenant context.
- Users cannot select or override another tenant identifier to gain access.
- Tenant filters are enforced at the data-access boundary, not only in the UI.
- Unique business constraints are tenant-aware where appropriate.
- Background jobs, exports, reports, file access, and integrations execute within an explicit tenant context.
- Automated tests prove that cross-tenant reads, writes, updates, deletes, and indirect-reference access are denied.

### PH1-BR-003 — Tenant-aware identity and authorization

The system shall support user membership and authorization within the correct tenant context.

**Acceptance criteria**
- A user can belong to one or more tenants only if the approved identity model permits it.
- Tenant membership is validated on every authenticated tenant-scoped operation.
- Roles and permissions can be configured per tenant.
- Platform administration permissions are separate from customer administration permissions.
- Permission changes and privileged actions are auditable.
- Authorization is enforced server-side for APIs, exports, downloads, and background operations.

### PH1-BR-004 — Tenant-specific configuration

The system shall allow approved settings to vary by tenant without code changes.

**Acceptance criteria**
- Each tenant can configure its approved work calendar, time zone, currency, leave settings, attendance settings, and workflow definitions.
- Configuration changes have effective dates or version history where they affect historical calculations.
- Defaults are explicit and documented.
- One tenant's configuration cannot affect another tenant.

### PH1-BR-005 — Sensitive-data protection

The system shall protect sensitive employee, identity, payroll, authentication, and document data.

**Acceptance criteria**
- Sensitive data is classified and assigned access rules.
- Sensitive data is encrypted in transit and protected at rest using approved platform controls.
- Secrets and TOTP seeds are not stored in plaintext.
- Encryption keys and application secrets are managed outside source control and are rotatable.
- File upload validation includes content/type checks, size limits, safe storage, and malware scanning or an approved compensating control.
- Downloads require authorization and are audited for sensitive document categories.
- Logs do not expose passwords, tokens, TOTP seeds, or unnecessary personal/financial data.

### PH1-BR-006 — Authentication and session security

The system shall provide secure authentication and session lifecycle controls.

**Acceptance criteria**
- Authentication endpoints have rate limiting and abuse protection.
- Password and MFA secrets follow approved storage practices.
- Users and administrators can revoke active sessions according to their permissions.
- Session inventory records the minimum necessary device/session metadata.
- Account lockout or progressive throttling policies are configurable and tested.
- Security-sensitive actions require appropriate re-authentication or MFA where policy requires it.

### PH1-BR-007 — Payroll financial precision

The system shall use precise decimal arithmetic for all monetary calculations.

**Acceptance criteria**
- Monetary values and intermediate financial calculations use `decimal` or an equivalent exact decimal representation.
- Rounding rules are explicit, versioned, and applied consistently.
- Currency precision is configurable according to the applicable currency and policy.
- No binary floating-point conversion is used in payroll calculation paths.
- Regression tests cover rounding boundaries, negative adjustments, proration, and large values.
- Historical finalized payroll results remain reproducible from their recorded inputs and policy versions.

### PH1-BR-008 — Versioned payroll policies and calculation traceability

The system shall preserve the policy inputs used to calculate each payroll result.

**Acceptance criteria**
- Each payroll calculation references a specific policy/version set.
- Effective-dated rules are selected based on the applicable payroll period and employee context.
- Calculation results retain input references, applied rules, rounding decisions, and calculation version.
- Finalized payroll is immutable through normal business operations.
- Corrections are represented through traceable adjustment or reversal records.
- Changes to future policies do not silently change historical payroll results.

### PH1-BR-009 — Payroll control and approval integrity

The system shall enforce separation of duties for payroll preparation, review, approval, and finalization.

**Acceptance criteria**
- The configured preparer cannot approve their own payroll where separation of duties is required.
- Approval authority is checked server-side.
- Finalization is protected against duplicate execution.
- Rejected or returned payroll runs retain their decision history.
- All material payroll actions are auditable.
- Payroll export and bank-file generation are permission-controlled and logged.

### PH1-BR-010 — Durable background processing

The system shall execute scheduled and asynchronous operations reliably.

**Acceptance criteria**
- Jobs survive application restarts and deployment cycles.
- Job execution is protected against unintended concurrent processing.
- Retries use bounded policies and distinguish transient from permanent failures.
- Job handlers are idempotent or use a deduplication mechanism.
- Failed jobs are observable and can be safely reprocessed by authorized operators.
- Jobs execute with an explicit tenant and correlation context.
- Dead-letter or equivalent failure handling is available for unrecoverable messages.

### PH1-BR-011 — Operational observability

The system shall provide sufficient telemetry to detect and diagnose production issues.

**Acceptance criteria**
- Structured logs include correlation identifiers and relevant tenant context without leaking sensitive data.
- Health checks cover application readiness and critical dependencies.
- Metrics cover request failures, latency, job failures, queue depth, and key business processing outcomes.
- Alerts are defined for critical service and job failures.
- Support staff can trace a business operation across relevant application components.

### PH1-BR-012 — Backup and recovery

The system shall support tested backup and restore procedures.

**Acceptance criteria**
- Database and file-storage backup schedules are documented and automated.
- Backups are encrypted and stored outside the primary failure domain.
- Retention periods are configurable and documented.
- Restore tests are performed on a defined schedule.
- Recovery Point Objective (RPO) and Recovery Time Objective (RTO) are agreed and measured.
- Backup and restore outcomes are logged and reviewed.

### PH1-BR-013 — Release and deployment governance

The system shall support controlled and repeatable production releases.

**Acceptance criteria**
- CI/CD includes build, automated tests, security checks, and migration validation.
- Database migrations have reviewed deployment scripts and rollback/forward-recovery guidance.
- Environment-specific secrets are not embedded in artifacts.
- Releases support health verification and documented rollback or recovery procedures.
- Production configuration changes are auditable.

## 6. Non-Functional Requirements

| Area | Requirement |
|---|---|
| Security | No cross-tenant data access; least privilege; secure secret handling |
| Availability | Target availability and maintenance windows to be agreed before production SLA |
| Performance | Establish baseline load tests and measurable response-time targets |
| Scalability | Support tenant growth without requiring immediate service decomposition |
| Auditability | Record security-sensitive and financial actions with actor and timestamp |
| Recoverability | Approved and tested RPO/RTO |
| Maintainability | Keep modular monolith boundaries and automated regression coverage |
| Privacy | Data minimization, retention, export, and deletion controls per applicable law |

## 7. Key Business Rules

- Tenant identity must be derived from a trusted authentication and membership context.
- No API may rely solely on a client-supplied tenant identifier for authorization.
- Historical payroll results must not be recalculated implicitly after policy changes.
- Finalized financial records must be corrected through traceable adjustments.
- Background jobs must not run without an explicit tenant context when processing tenant-owned data.
- Production payroll rules require documented business-owner and specialist approval.

## 8. Dependencies

- SaaS tenant model and hosting strategy: confirmed by Ahmed on 2026-11-25 (SaaS multi-tenant with subscriptions; shared database + `TenantId` first, database-per-tenant later for large customers). Dedicated deployment remains possible for an early customer.
- Identity and authorization model.
- Data classification and retention policy.
- Payroll specialist review of financial and statutory rules.
- Selected database, object storage, secrets management, and job-processing infrastructure.
- Defined production SLO, RPO, and RTO.

## 9. Risks and Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Incomplete tenant filtering | Cross-company data exposure | Central enforcement plus negative isolation tests |
| Payroll conversion changes | Historical calculation differences | Golden-master regression tests and controlled migration |
| Incorrect legal assumptions | Payroll non-compliance | Specialist validation and effective-dated policy sign-off |
| Duplicate jobs | Duplicate payroll or notifications | Idempotency keys and distributed execution controls |
| Unverified backups | Extended or permanent data loss | Scheduled restore drills |
| Overengineering platform | Delivery delays | Keep modular monolith; introduce infrastructure only for proven needs |

## 10. Success Metrics

- 100% of tenant-scoped APIs and background jobs have tenant-isolation test coverage.
- Zero known cross-tenant access paths at release sign-off.
- 100% of payroll monetary calculations use exact decimal arithmetic.
- 100% of finalized payroll runs retain calculation and policy traceability.
- Critical background jobs have retry, failure visibility, and duplicate-execution protection.
- Backup restore test meets the agreed RPO/RTO.
- Production release checklist is completed for every release.

Targets should be confirmed with product, engineering, security, and operations owners before becoming contractual SLAs.

## 11. Release Gate

Phase 1 is complete only when:
1. Tenant isolation has passed API, database, export, file, and background-job tests.
2. Security review findings rated Critical or High are resolved or formally risk-accepted.
3. Payroll precision and traceability requirements pass regression tests.
4. Job retry and duplicate-execution scenarios pass.
5. Backup restore has been demonstrated.
6. Monitoring, alerting, runbooks, and rollback/recovery procedures are available.
7. Product and business owners approve the release.

## 12. Open Decisions

- RESOLVED 2026-11-25 by Ahmed: SaaS multi-tenant. Shared database with `TenantId` first, database-per-tenant later as an option for large customers. Details of the tier boundary remain for Phase 0.
- Whether a user may belong to multiple tenants.
- Tenant-level data residency and retention requirements.
- Initial production RPO/RTO and availability targets.
- Selected durable job-processing technology.
- Required MFA policy for employees, HR administrators, and platform administrators.
- Which payroll rules are in the first legally validated country package.
