> **PROPOSED DRAFT, NOT APPROVED. DOCS ONLY.** Gap analysis of the proposed phase stories (`USER-STORIES-Phase-1/2/3.md`, from the unapproved ChatGPT evaluation drafts) against the code and `docs/TRACEABILITY.md` on main as of commit e52c6a4 (2026-11-25). It follows the owner's WhatsApp voice note of 2026-11-25 15:59 EET (relayed): check every story against what exists so built work is not redone before any planning. Not part of BRD v3 scope.

# Gap analysis: proposed phase stories vs current PeopleHub

## Method and limits

- Evidence is TRACEABILITY.md rows, OPERATIONS.md, and targeted reads/greps of `src` (entity, service, controller and view names). I did not read every file. A "covered" call means the story's acceptance criteria are met by what the docs and code show; where I only have documentation evidence I say "per docs".
- Classes: **Covered** (acceptance criteria met in code), **Partial** (some criteria built, others not), **Gap** (nothing material built). A story can be Partial or Gap because of a missing owner decision rather than missing engineering; the last column says which.
- No story is classed Covered: none has all its acceptance criteria met. These stories were written for a multi-tenant commercial product, which the current single-company demo is not.
- TRACEABILITY ids in brackets (for example `[SEC-004]`) are rows in `docs/TRACEABILITY.md`.

## Summary

| Class | Phase 1 (13) | Phase 2 (19) | Phase 3 (18) | Total (50) |
|---|---|---|---|---|
| Covered | 0 | 0 | 0 | 0 |
| Partial | 7 | 15 | 7 | 29 |
| Gap | 6 | 4 | 11 | 21 |

Most Phase 2 stories have a real built core. Phase 1 splits between security and payroll items with a built core and platform items (tenancy, durable jobs, CI) that do not exist. Phase 3 is mostly platform capabilities (integrations, SSO, AI) that do not exist. Blocked-by-owner-decision items are listed in the last column and repeat the BRD v3 decision register.

## Phase 1: Foundation and production readiness

| Story | Class | Evidence | Blocker type |
|---|---|---|---|
| US-PH1-001 Tenant lifecycle | Gap | No `Tenant` or `TenantId` anywhere in `src` (grep). Single company. | Operating model decided 2026-11-25 (SaaS multi-tenant); design is Phase 0 work |
| US-PH1-002 Tenant data isolation | Gap | Same. One `AppDbContext`, no query filters by tenant; `[ENT-001]` Open. Existing per-user scope (`AccessScope`) is employee-scope within one company, not tenant isolation. | Same decision, then large cross-cutting build |
| US-PH1-003 Tenant-aware identity and authorization | Gap | Roles, permission grants and step-up exist (`Access.cs`, `[SEC-001]`, `[SEC-010]`) but are single-company. No tenant membership, no platform-admin vs customer-admin split beyond SystemAdmin. | Same |
| US-PH1-004 Tenant-specific configuration | Gap | Settings, workflows, holidays and shifts are global to the one company. | Same |
| US-PH1-005 Sensitive-data protection | Partial | Field-level grants for identity, bank, salary, medical evidence and private files exist `[SEC-001]`. Encryption at rest `[SEC-004]` Open; TOTP seeds and mail bodies stored in plaintext (OPERATIONS.md); no retention/deletion policy `[SEC-007]` Open; malware scan unconnected `[SEC-008]`. | Owner decisions: key custody, retention |
| US-PH1-006 Authentication and session security | Partial | Hashed per-login sessions, 8h expiry, inventory and revocation `[SEC-003]`; lockout after 5 failures `[SEC-006]`; TOTP step-up `[SEC-010]` Partial; password reset tokens hashed. Missing: broader anomaly detection, physical-device binding, SSO, TOTP seed encryption. | Partly engineering, partly key custody | **Update 2026-11-26:** lockout window, expiry, no enumeration state, audit and rate-limit policy now covered by AuthThrottlingTests. Thresholds are hardcoded (5 failures, 15 minutes, 12 requests per minute), not configurable; no MFA yet. Owner decisions.
| US-PH1-007 Payroll financial precision | Partial | Calculations use `decimal` with explicit rounding. But `AppDbContext.ConfigureConventions` converts every decimal to `double` for storage (SQLite REAL, Postgres double precision). Needs a provider-specific exact decimal mapping and migration. | Engineering, with a data-migration risk on live Neon data (backup first) |
| US-PH1-008 Versioned payroll policies and calculation traceability | Partial | Immutable inputs, policy version, hash and history for new runs `[PAY-008]`; single documented policy `EG-DEMO-2026-v1` `[PAY-013]`, not legally validated. Missing: multiple effective-dated policy versions and a specialist sign-off. | Owner decision: qualified Egyptian payroll sign-off | **Update 2026-11-26:** engine now resolves the country pack by period date (PayrollCalculator.PackFor); periods before the first pack keep the 2026 values and are flagged by IsFallback. Needs a second reviewed pack to exercise effective dating.
| US-PH1-009 Payroll control and approval integrity | Partial (9 criterion tests added in PayrollControlIntegrityTests; SoD only at Finalize, owner decision open for FinanceApprove) | Locking and separate final approver `[PAY-009]`, creator cannot finalize own run `[SEC-002]`, append-only audit `[SEC-005]`, retro adjustments via approved correction `[PAY-004]`. Criteria were not each re-tested for this analysis, so I class it Partial until checked. | Engineering review only |
| US-PH1-010 Durable background processing | Gap | No Hangfire, Quartz or job table. Two in-process `BackgroundService` workers (`PerfWorker`, `OperationsService`) that stop when Render free sleeps `[ARC-004]` Open. | Owner decision: hosting tier and scheduler choice |
| US-PH1-011 Operational observability | Partial | Live and database-ready health endpoints, bounded in-process request metrics, correlation IDs and logs `[ARC-009]`. No Serilog, OpenTelemetry, alerting or durable telemetry. | Hosting/telemetry choice | **Update 2026-11-26:** log scope carries tenant and trace id; /metrics serves OpenMetrics text (off unless METRICS_TOKEN is set). Alerting and durable telemetry remain open.
| US-PH1-012 Backup and recovery | Partial | Manual authenticated database export (SQLite online backup, Postgres COPY ZIP) and optional daily SQLite backup path; no scheduled off-host Postgres backup, no restore drill, no encryption `[SEC-009]` Open. | Owner decisions: RPO/RTO, key custody, funded storage |
| US-PH1-013 Release and deployment governance | Gap | No `.github` folder, so no CI workflow. Deploys are manual in the Render dashboard. Regression is run by hand in partitions and recorded in `docs/REGRESSION-2026-11-25.md`. | A GitHub Actions build+test workflow needs no owner decision |


## Phase 2: HR functional excellence

| Story | Class | Evidence | Blocker type |
|---|---|---|---|
| US-PH2-001 Leave accrual policies | Gap | `[LEV-001]` Open. Leave types carry a default number of days only (`LeaveType.DefaultDays`); no accrual frequency, eligibility, proration or preview. | Owner decision: accrual basis and eligibility |
| US-PH2-002 Hourly and partial-day leave | Gap | `[LEV-002]` Open. | Owner decision: units and rounding |
| US-PH2-003 Leave carry-forward and expiry | Gap | `[LEV-003]` Open. A `MaxCarryOver` field exists on leave types but no expiry rules. | Owner decision |
| US-PH2-004 Leave encashment | Partial | `[LEV-005]` settlement estimate only; no approval or payout cycle. | Owner decision: eligibility and formula |
| US-PH2-005 Attendance policy and exception completion | Partial | Missing-punch inbox `[ATT-003]`, reasoned corrections `[ATT-008]`, month lock `[ATT-007]`, flexible hours `[ATT-005]`, rotating shifts `[ATT-001]`, holiday calendars `[ATT-009]`. Open: category grace `[ATT-010]`. | Category grace needs a policy value |
| US-PH2-006 Mobile attendance capture | Gap | `[ATT-006]` and `[ENT-005]` Open. No geofence or GPS. | Product and privacy decision |
| US-PH2-007 Biometric device integration | Partial | ZKTeco ingestion with raw append-only punches and idempotent processing exists `[ATT-004]`, plus a device bridge; untested on real hardware (project memory and OPERATIONS.md). | Needs real device verification |
| US-PH2-008 Overtime management | Partial | `OvertimeRequest` with approval workflow, weighted hours into payroll (`AttendanceService`), weekend and holiday multiplier in the attendance domain. Missing: configurable policy caps and rates per category (not confirmed). | Policy values |
| US-PH2-009 Payroll proration | Partial | `[PAY-003]` settlement fixed, regular payroll proration open; `[PAY-002]` terminal-period eligibility open. | Owner decision |
| US-PH2-010 Salary structures and compensation changes | Partial | `[PAY-001]` immutable grade (and optional category) templates, application evidence, historical runs unchanged; `[EMP-005]` scheduled assignments. Missing: allowance bands and approval/publication lifecycle. | Partly engineering |
| US-PH2-011 Retroactive payroll adjustments | Partial | `[PAY-004]` append-only correction proposal, independent Finance approval, exactly-once apply. Missing: automatic multi-period retro recompute (deliberately not built). | Owner decision on automation |
| US-PH2-012 Egypt payroll localization package | Partial | Policy `EG-DEMO-2026-v1` illustrative, not legally validated `[PAY-013]`; deduction priority `[PAY-006]` Open. No pluggable country package. | Owner decision: specialist sign-off |
| US-PH2-013 End-of-service and termination settlement | Partial | `[PAY-012]` estimator only; Talent/Settlement view; no approval or payout. | Owner decision: final-settlement rules |
| US-PH2-014 Recruitment workflow | Partial | Candidate pipeline, requisitions with independent approval, internal interview planning, offers and onboarding checklists `[HR-003]`, `RecruitmentInterviews`/`RecruitmentOffers` services. Missing: CV upload and parsing, external calendar, offer e-sign. | Partly engineering |
| US-PH2-015 Benefits administration | Partial | Plans with eligibility and coverage, dependents `[EMP-003]`, enrolment with reviewed evidence, cost allocation planning `[HR-004]`. Missing: claims, payroll linkage. | Partly engineering |
| US-PH2-016 Employee self-service | Partial | Own profile, leave, attendance, advance and overtime requests, documents, grievances, appraisal acknowledgement `[PER-003]`. Missing: HR letter generation (only offer letters), announcements. | Engineering |
| US-PH2-017 Mobile application or responsive experience | Partial | Responsive CSS (media queries in `wwwroot/css`) and an EN/AR UI checked at desktop width in regression. No mobile app. `tests/browser/mobile-viewport.cjs` now checks every sidebar page of four roles in EN and AR at 390px for page-level horizontal overflow (128 pages: 25 overflowed, fixed in `site.css`, 0 now). Not covered: real devices, touch behaviour, pages reached only by links. | Engineering |
| US-PH2-018 Notification delivery management | Partial | In-app notifications and an email outbox exist; Brevo is not operational (credential issue, reset held) and the worker stops while Render sleeps (OPERATIONS.md). No push, no delivery tracking UI. | Owner decision: Brevo credential reset; hosting |
| US-PH2-019 HR operational reports | Partial | Dashboard plus report builder for employees, attendance, leave and payroll (`ReportService.BuildAsync`), CSV/Excel/PDF export, cost-centre journal CSV `[PAY-011]`. No scheduled reports, no turnover or headcount trend reports. | Engineering |

## Phase 3: Enterprise and intelligent HR

| Story | Class | Evidence | Blocker type |
|---|---|---|---|
| US-PH3-001 Integration catalog and administration | Gap | No integration registry. Only email outbox `[ARC-007]` Partial. | Hosting, contracts |
| US-PH3-002 Integration execution and reconciliation | Partial | Email outbox with retry; bank-result reconciliation is manual evidence `[PAY-010]`. No generic outbox, idempotent adapters or reconciliation runs. | Contracts, credentials |
| US-PH3-003 Enterprise identity integration | Gap | `[ENT-004]` Open. No OIDC or SAML. | Owner decision: provider |
| US-PH3-004 Finance and accounting integration | Partial | Cost-centre journal planning projection and CSV `[PAY-011]`; no account mappings or posting. | Owner decision: chart of accounts, system |
| US-PH3-005 Banking and payment integration | Gap | `[ENT-003]` Open; manual reconciliation evidence only, no bank file or API. | Bank contracts, credentials |
| US-PH3-006 Communication and device integrations | Partial | Email outbox; ZKTeco bridge. No SMS or push providers. | Provider choice, credentials |
| US-PH3-007 Workflow service levels and escalation | Partial | Per-kind reminder policy, daily ledger, targeting through delegation `[WFL-001]`; no escalation or business-hours calendar. | Escalation policy; durable scheduler |
| US-PH3-008 Advanced delegation and approval governance | Partial | Dated delegation `[WFL-006]`, reassignment `[WFL-005]`, parallel/quorum rules `[WFL-002]`, required comments `[WFL-007]`, versioned definitions `[WFL-004]`. Missing: draft/publish lifecycle, version comparison. | Engineering |
| US-PH3-009 Workflow simulation and analytics | Gap | No simulation; no approval analytics beyond request metrics. | Engineering |
| US-PH3-010 Executive HR analytics | Partial | Dashboard and four reports; no headcount, turnover or cost trend analytics `[ENT-006]` Open. | Engineering |
| US-PH3-011 Workforce cost and headcount planning | Partial | Target headcount on departments, annual training cost summary `[HR-002]`, cost-centre projection `[PAY-011]`; no planning module `[ENT-006]`. | Engineering |
| US-PH3-012 Skills and competency intelligence | Gap | Competency frameworks per grade `[PER-001]` exist; no skills inventory or gap analysis. | Engineering |
| US-PH3-013 Succession and talent planning | Gap | `[ENT-006]` Open. | Engineering and policy |
| US-PH3-014 AI-assisted HR knowledge and policy search | Gap | No AI component. | Owner decision: AI governance, provider, data handling |
| US-PH3-015 Payroll and attendance anomaly detection | Gap | No detection logic. Source data exists (payroll snapshots, raw punches). | Owner decision: approved AI/statistical use |
| US-PH3-016 Workforce forecasting | Gap | None. | Same |
| US-PH3-017 AI governance and human oversight | Gap | None. Audit log and separation-of-duties patterns could be reused. | Owner decision |
| US-PH3-018 Country policy packages | Gap | Egyptian rules are embedded in payroll code and settings; no package interface. | Depends on Phase 0 architecture and legal sign-off |

## Reading the result

1. **Already built, do not redo:** step-up and session security, field-level privacy grants, append-only audit, workflow engine features (versions, quorum, delegation, reassignment, reminders), payroll locking and traceability, correction and bank-evidence workflows, benefits and recruitment cores, grievance intake and appeals, attendance ledger and corrections, document and asset management.
2. **Real engineering gaps that need no owner decision:** CI workflow `[US-PH1-013]`, exact decimal storage `[US-PH1-007]`, report breadth and scheduling `[US-PH2-019]`, responsive verification `[US-PH2-017]`, HR letter generation `[US-PH2-016]`, workflow draft/publish and simulation `[US-PH3-008/009]`, generic outbox and idempotency `[US-PH3-002]`.
3. **Blocked on the owner:** (tenancy model now decided: SaaS multi-tenant, 2026-11-25; tenant-shaped stories PH1-001 to 004, PH3-018 wait for Phase 0 design) pricing and plan decisions, encryption and key custody, retention, hosting and scheduler tier, Egyptian payroll sign-off, leave policy values, Brevo credential, integration contracts, AI governance.
4. **Not verified:** real ZKTeco hardware, PostgreSQL race behavior and load, email delivery, mobile viewports, malware scanning.

## Verification of the ChatGPT review's own claims

The evaluation said money calculations use `double` conversions per OPERATIONS.md. Checked: arithmetic is `decimal`, storage conversion is `double` in `AppDbContext.ConfigureConventions`. It also said it could not read `src`, so its code-level statements were not verified by it.

Note (2026-11-25): the country-pack addendum (`COUNTRY-PACKS-Addendum.md`, CP-BR-001..005) is not yet gap-analysed. Expected: Gap, with Egyptian rules embedded in payroll code.
