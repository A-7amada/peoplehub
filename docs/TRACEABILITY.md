# Requirement traceability and delivery register

24 November 2026. Each Open row is unfinished work, not a shipped capability. Partial means the exact remainder is still open. Sources and broader rationale: [external review](REVIEW-2026-11-24.md). Existing feature regressions: [module report](../MODULE-REGRESSION.md).

| ID | Requirement | Priority | Evidence/status |
|---|---|---|---|
| EMP-001 | Employee onboarding and offboarding checklists | P1 | Implemented explicit task creation, assignee, due date, completion evidence and audit; no automatic employment/payroll effect |
| EMP-002 | Document repository and expiry alerts | P1 | Implemented private employee/HR documents with expiry and daily warning window; scheduler is single-instance and service-awake only |
| EMP-003 | Dependents | P1 | Implemented private employee/HR records, reasoned removal; benefit eligibility separate |
| EMP-004 | Effective-dated employee status transitions | P0 | Partial: future dated status schedule, cancellation and recorded effective date; broader transition policy and missed-run handling under review |
| EMP-005 | Historical department, title, manager, shift and salary assignments | P0 | Partial: current history plus future assignment scheduling/cancellation; backdating and full historical payroll allocations remain open |
| EMP-006 | Probation and confirmation | P1 | Implemented explicit probation end and reasoned final decision, no invented duration |
| EMP-007 | Temporary/permanent transfers | P0 | Implemented current/future permanent transfers and explicitly dated temporary assignment with linked snapshot return; cancellation before start cancels both, return cannot be removed separately, changes remain blocked until return. No backdated changes or automatic allowance/payroll recalculation |
| EMP-008 | Clearance and asset handover | P1 | Implemented clearance tasks and separate asset assignment/return with condition and receipt evidence; clearance is not auto-approved |
| EMP-009 | HR custom fields | P1 | Implemented HR-only typed text/date/number fields, validation and audit |
| ATT-001 | Rotating/multiple shifts | P1 | Implemented dated employee shift assignments, configurable definitions and explicit repeating calendar-day pattern generator; no automatic weekend/pay policy change |
| ATT-002 | Overnight shifts | P1 | Implemented cross-midnight device pairing, manual/correction checkout normalization and previous-night self checkout; post-shift delayed punches beyond end remain exceptions |
| ATT-003 | Missing punch exceptions | P1 | Implemented scoped missing-in/out recorded-day inbox after shift end, unmatched raw evidence and explicit idempotent reprocessing; arbitrary unknown-device mapping remains separate |
| ATT-004 | Raw punch ledger and idempotent processing | P0 | Implemented: append-only punches, normalized batch/retry dedupe and unique idempotency key; concurrent losing request may require retry |
| ATT-005 | Flexible hours | P1 | Implemented explicitly configured daytime arrival window and required minutes on new shift definitions. Lateness from latest arrival, early departure/overtime from personal expected end, existing grace/absence threshold retained. Explicit dated assignment, no retroactive recomputation, break deductions, overnight flex or payroll formula change |
| ATT-006 | Remote attendance/geofencing | P2 | Open |
| ATT-007 | Approved attendance period locking | P1 | Implemented completed-month lock, reason, no reopening, mutation guards and retained late raw punches; broader financial-period coordination remains open |
| ATT-008 | Reasoned manual corrections with original evidence | P0 | Implemented: approved correction retains before/after, reason and original raw punches |
| ATT-009 | Branch/group holiday calendars | P1 | Implemented future global/branch/department calendars and historical-assignment scope in new leave requests and attendance; arbitrary groups and retroactive recalculation remain open |
| ATT-010 | Category/shift grace periods | P1 | Partial: shift grace existing, categories open |
| LEV-001 | Accrual by joining date/month and eligibility rules | P0 | Open: policy decision required |
| LEV-002 | Half-day and hourly leave | P1 | Open |
| LEV-003 | Carry-forward expiry | P1 | Open |
| LEV-004 | Append-only balance adjustment ledger | P0 | Implemented this batch |
| LEV-005 | Leave encashment | P1 | Partial: settlement estimate only |
| LEV-006 | Approved leave cancellation, reversal, reason and notification | P0 | Implemented this batch |
| LEV-007 | Notice-period and negative-balance policies | P0 | Partial: no negative balance; notice policy open |
| LEV-008 | Department calendar | P1 | Implemented scoped month view of approved leave spans, historical department filter, no reason/type/medical detail; weekends shown as span context, not deducted leave days |
| LEV-009 | Simultaneous request/approval constraints | P0 | Partial: employee revision serializes concurrent request creation, final balance check is atomic; PostgreSQL race stress remains unverified |
| PAY-001 | Grade/category salary structures | P0 | Partial: immutable explicit grade templates with basic salary and listed fixed components, individual grade-match application from today, unchanged unlisted components and append-only before/after hash evidence; historical runs unchanged. Pending employment schedules block application. Category rules, approval/publication lifecycle and effective-dated component allocations remain open |
| PAY-002 | Payroll eligibility | P0 | Partial: active/on-leave employed by month end; terminal-period eligibility open |
| PAY-003 | Joining/termination proration | P0 | Partial: settlement fixed, regular payroll proration open |
| PAY-004 | Retroactive pay and referenced adjustments | P0 | Partial: append-only explicit-amount correction proposal from an earlier locked source line/version to a later draft for the same employee, independent non-subject Finance approval/rejection and explicit exactly-once apply to bonus/manual deduction through versioned calculation. Separate initially-unset grant, reason/amount/state guards and no negative-net deduction. Recalculation retains manual totals but later manual edits can replace them. Original payroll immutable; automatic retro calculation, tax-period restatement and correction reversal remain open |
| PAY-005 | Bonuses/commissions/incentives | P1 | Partial: bonus existing |
| PAY-006 | Deduction priority and statutory limits | P0 | Open: qualified specialist review required |
| PAY-007 | Loan cancellation/reschedule/repayment | P1 | Partial: early settlement existing |
| PAY-008 | Immutable calculation inputs, policy/version/hash and history | P0 | Implemented for new calculations; old history not reconstructed |
| PAY-009 | Approved payroll locking and separate final approver | P0 | Implemented this batch |
| PAY-010 | Bank-result reconciliation | P1 | Partial: separate initially-unset grant, manual append-only full-net paid/rejected/returned evidence per locked payroll line, reference/reason/amount/state/duplicate guards and current result view. No transfer, automatic status/advance changes, partial payment or bank file import; external verification/adapters remain open |
| PAY-011 | Cost-centre journals | P2 | Partial: locked-run cost-centre planning projection and CSV of captured gross expense, deductions clearing and net payable, balanced per line and group. Missing historical cost centres stay unallocated. Financial-view plus payroll-view required, export separate. No account mappings, employer costs, posted journals or accounting adapter; owner chart-of-accounts and integration remain open |
| PAY-012 | Complete final settlement and approval | P0 | Partial: estimator only |
| PAY-013 | Single versioned Egyptian policy, legal validation status | P0 | Documented EG-DEMO-2026-v1, not legally validated |
| WFL-001 | SLA, reminders and escalation | P1 | Partial: per-kind explicit elapsed-hour reminder policy (off by default), daily recipient/stage ledger, current approver/reassignment/delegation targeting and minimal private in-app reminders while service is awake. No automatic escalation, working-hours calendar or delivery-time guarantee; durable scheduling and escalation policy remain open |
| WFL-002 | Parallel approvals and all/one/quorum rules | P1 | Open |
| WFL-003 | Dynamic hierarchy/department approvers | P1 | Partial: direct manager/role existing |
| WFL-004 | Explicit workflow versions | P1 | Implemented content-addressed immutable observed definitions linked to new requests plus resolved route snapshot; legacy version unknown, separate draft/publish lifecycle not implemented |
| WFL-005 | Pending task reassignment | P1 | Implemented reasoned workflow-admin reassignment of current pending stage to an active account, immutable history and in-app notification; self-approval blocked, stage revision guards stale updates, original route unchanged |
| WFL-006 | Recall/substitute/unavailable approver | P1 | Implemented pending recall/cancellation, dated delegation and explicit administrator reassignment for unavailable approvers; no automatic absence inference or escalation |
| WFL-007 | Required decision comments | P1 | Implemented admin-configured required approval comments per request kind, server validation, retained visible decision history; rejection reasons always required. Changes explicitly apply to next decisions including pending requests |
| PER-001 | Grade competency framework and rating configuration | P1 | Partial: optional HR-chosen job-grade restriction, expected behaviours and explicit weights; complete frameworks copy into appraisals with append-only hashed content evidence, no retroactive source-template effects. Existing 1-5 rating scale retained; configurable rating scales and published framework lifecycle remain open |
| PER-002 | Multiple cycles | P1 | Existing |
| PER-003 | Employee result acknowledgement and appeal | P1 | Implemented immutable employee acknowledgement/appeal and independent HR disposition after closure; score correction workflow remains separate/open |
| PER-004 | Separate feedback identity/comment/aggregate privacy and reviewer threshold | P0 | Implemented configurable separate grants and threshold gate; owner policy values remain unset. Existing finalized scores preserved |
| PER-005 | PIP milestones and trends | P1 | Implemented explicit measurable milestones, dated progress evidence, independent completion/cancellation, immutable timeline and latest 12 closed-cycle descriptive score/milestone history; cycles may differ and no normalized comparison or employment decision is implied |
| HR-001 | Asset assignment, handover, condition, return, maintenance | P1 | Implemented inventory, unique tags, explicit assignment/return, maintenance/release/retirement and append-only movement evidence |
| HR-002 | Training needs, budgets, approval, certificates/expiry/effectiveness | P1 | Partial: append-only private completed-enrolment certificate files, issuer/reference/expiry, participant reflection and independent HR effectiveness evidence. New course proposals require stated need/expected application, optional currency-tagged planning budget and independent HR plan approval before enrolment; legacy courses explicitly lack historical approval evidence. Aggregate/department budget controls, needs prioritization and normalized effectiveness metrics remain open. Certificate expiry alerts are private in-app, employee/training-manager only, at most daily within 30 days and after expiry while service is awake; no email or delivery-time guarantee |
| HR-003 | Requisitions, interviews, offers and onboarding | P1 | Partial: reasoned headcount requisitions and independent HR approval before opening/candidate entry; rejected requests cannot reopen; legacy approval unknown. Existing candidate pipeline and separate onboarding checklist retained. Internal Cairo-time interview plans, participant conflict checks, reasoned cancellation and outcome evidence implemented without invitations. Internal immutable offer proposals, different-user approval and evidence-noted acceptance/decline/withdrawal implemented; hiring-stage change requires accepted offer and never creates an employee/payroll. External calendar coordination, offer letter delivery/signature and automatic onboarding handoff remain open |
| HR-004 | Benefits plans/dependents/eligibility/enrolment/cost allocation | P1 | Partial: immutable plan eligibility/coverage and per-person monthly planning shares; private employee/dependent enrolment with explicit reviewed eligibility evidence, dates, cost centre and append-only cancellation. Separate benefits-management grant, initially unset outside SystemAdmin. No insurer purchase/notification/coverage guarantee, payroll deduction/payment, invoice/proration or automatic external cancellation; provider integration and richer rules remain open |
| HR-005 | Confidential grievances, investigation and discipline | P1 | Partial: separate all-case relations grant initially unset outside SystemAdmin; private immutable source record and append-only investigation/proposal/decision/closure history. Discipline approval excludes case creator, proposer and subject. General audit omits contents; no notifications, served warning, employment/payroll changes or executed sanctions. Per-case investigator access, attachments, employee submissions/appeals and legal process review remain open |
| SEC-001 | Field-level identity/salary/bank/medical access | P0 | Partial: implemented separate identity, bank-view, bank-edit and leave-evidence grants alongside salary permission; profile/edit/list/history redaction and tampered POST/file download guards; leave ledger/balance exports restricted to owner or evidence grant. New grants are unset for existing non-SystemAdmin roles pending owner configuration; broader export/service privacy audit remains open |
| SEC-002 | Separation of duties | P0 | Implemented creator cannot finalize own run |
| SEC-003 | Session expiry/revocation/device management | P0 | Implemented hashed per-login sessions, 8h absolute expiry, owner inventory/revocation, logout and credential-change invalidation; no verified physical-device binding |
| SEC-004 | Sensitive-field encryption at rest | P0 | Open: key management required |
| SEC-005 | Append-only audit history | P0 | Implemented repository guard; database-owner tampering not prevented |
| SEC-006 | Failed-login lockout and suspicious detection | P0 | Implemented password lockout after five failures for 15 minutes plus audit; broader anomaly detection open |
| SEC-007 | Retention/deletion/access policies | P0 | Open: owner policy decisions required |
| SEC-008 | Malware scan, signatures/private attachments | P0 | Partial: private scoped storage, size/extension and file signature checks; malware scanner remains unconnected |
| SEC-009 | Encrypted backup and controlled restore | P0 | Open: encryption/key custody/restore drill |
| SEC-010 | Step-up auth for sensitive admin actions | P0 | Partial: five-minute current-session password + enabled TOTP challenge on settings/user/role/workflow writes, manual device/mail operations, database export, workflow reassignment, advance settlement, payroll manual adjustment/recalculation/Finance approval/paid state, referenced correction mutations and payroll finalization/bank/full snapshot exports; action is not replayed. Broader sensitive-action coverage remains under review |
| ARC-001 | Modular monolith domain boundaries and policies | P1 | Existing layers; deeper policy extraction open |
| ARC-002 | PostgreSQL/SQL Server | P0 | PostgreSQL existing |
| ARC-003 | Explicit business transactions | P0 | Partial: payroll, attendance, device ingestion, final approvals, leave request/adjustment and employee save atomic; remaining service paths under review |
| ARC-004 | Durable background scheduler | P1 | Open |
| ARC-005 | Distributed cache/multi-instance | P2 | Open |
| ARC-006 | Private object storage | P1 | Open |
| ARC-007 | Integration outbox/domain events/idempotency/logs/retry/reconciliation/versioning | P1 | Partial: email outbox existing |
| ARC-008 | Optimistic concurrency/database constraints | P0 | Existing, further race tests open |
| ARC-009 | Read projections/metrics/tracing/health | P1 | Partial: public minimal live/database-ready endpoints, bounded in-process action metrics and correlation IDs/logs; durable telemetry, domain projections and external tracing remain open |
| ENT-001 | Multiple legal entities, shared employees and intercompany payroll | P2 | Open |
| ENT-002 | Legacy imports/validation/dedup/reconciliation/rollback | P1 | Partial: legacy performance import existing |
| ENT-003 | Accounting/government/banking adapters | P2 | Open: external contracts and credentials |
| ENT-004 | SSO/federation | P2 | Open |
| ENT-005 | Mobile/GPS attendance | P2 | Open |
| ENT-006 | Workforce planning/succession/career/analytics/AI insights | P3 | Open |
| DOC-001 | Separate baseline BRD from implementation history | P0 | Implemented this batch |
| DOC-002 | Feature-level status matrix and traceability IDs | P0 | Implemented this batch |
| DOC-003 | Versioned assumptions, risk and decisions | P0 | Implemented this batch |
| DOC-004 | Scenario-based acceptance criteria | P0 | Implemented this batch |
| DOC-005 | Measurable NFR targets and test status | P0 | Proposed targets documented, not achieved guarantees |

## Delivery batches

1. Business foundation, current batch: snapshots, append-only evidence, same-actor finalization guard, immutable final runs, assignment-change history, leave ledger/cancellation and employee-edit financial boundary. Tests: BusinessFoundationTests.
2. Remaining P0: effective-dated scheduling and historical allocations, regular payroll eligibility/proration/retro, accrual policy and race constraints, 360 privacy, encryption/retention/key custody.
3. P1 operational modules and workflow maturity.
4. P2 integrations/enterprise infrastructure and P3 strategic HR.

Acceptance is feature-level. A successful deployment of batch1 does not close batches2-4.

Attendance evidence follow-up: 130 tests pass. New tests cover same-batch duplicate, database uniqueness, append-only punch/evidence guards, partial-ingestion rollback, retained original/history, read scope, correction evidence and failed final leave-approval rollback. Both provider migrations generated; local SQLite upgrade and EN/AR attendance evidence inspected. PostgreSQL integration race stress not run.

Privacy/auth follow-up: 138 tests pass. No role grants or reviewer threshold were assumed. Thresholds freeze after first submitted review. Password lockout is a technical demo safeguard (5 failures/15 minutes), not an owner business-policy decision or production security certification. Both provider migrations generated.

Employment scheduling follow-up: 143 tests pass. Future assignment/status schedules preserve the current employee until due, apply once, retain effective history and can be cancelled before due. One pending schedule per employee avoids ambiguous conflicting plans. Backdating, retro-pay, temporary transfer return, full historical allocations and durable multi-instance scheduler are not claimed complete.

Attendance lock follow-up: manual entries now require a reason. Completed months can be approved/locked with an append-only lock record. Pending corrections block locking. Locked summaries reject manual/excuse/correction changes; delayed device punches remain raw without rewriting the locked projection. Employee revision contention protects cross-request locking races; PostgreSQL parallel stress remains unverified.

Lifecycle follow-up: 152 tests pass. Employee lifecycle screen inspected in EN/AR. No default checklist, probation length, benefits eligibility or automatic termination/payment is assumed. Document repository/expiry, asset inventory and broader operational modules remain open.

Shift follow-up: 160 tests pass including private document and overnight tests. Dated shift override takes precedence over effective employment history, then current shift fallback. Raw timestamps stay unchanged. Shift pattern and grace/late limits are explicit configuration. No break deductions or flexible-hours formula was invented.

Asset follow-up: 164 tests pass (160 regression plus 4 asset tests). Registration, assignment and receipt evidence exercised locally through MVC forms; EN/AR viewport screenshots inspected. HR controls inventory/movements; employee sees only own assigned assets/history. Return validates the holder. No automatic clearance, deduction, valuation or depreciation. PostgreSQL schema deployed; live fictional asset registration, assignment and receipt history verified.

Operational follow-up: 169 tests pass (165 regression and 4 scoped-calendar tests). Assigned lifecycle tasks now have a private assignee inbox on the assignee profile, avoiding access to another employee profile. Calendar registration and task inbox inspected locally in EN/AR. Holiday scopes affect new leave duration and attendance totals, with existing approved request duration preserved. Department is the implemented group, not arbitrary employee groups.

Performance response follow-up: employee acknowledgement records receipt, not agreement; one appeal has a referenced HR disposition, with no self-review, no silent score rewrite and no payroll effect. Append-only response evidence and appraisal revision guard duplicate races. Local MVC appeal EN/AR inspected. Four dedicated tests pass; regular score correction after upheld appeal remains an explicit separate gap.

Session follow-up: 177 tests pass (173 regression plus 4 session tests). Two isolated browser contexts exercised: owner revokes the other, its next request rejects while current remains valid; current revoke then rejects current request. EN/AR security page inspected. Session tokens are stored only as hashes. Old cookies without a session record are intentionally signed out on rollout. User-agent/IP are displayed as unverified hints, not device identity.

Attendance exception follow-up: 181 tests pass (177 regression plus 4 exception tests). Missing punch inbox inspected in EN/AR. Overnight shifts are not flagged before next-day end. No record still uses the absence calendar. Reprocessing keeps manual, excused and locked records, preserves raw evidence and does not invent missing times. Unknown device IDs are visible only to attendance managers and are not silently remapped.

Leave calendar follow-up: 184 tests pass (181 regression plus 3 calendar tests). Month clipping, approved-only visibility, historical departments and scope boundaries verified. EN/AR and 390px viewport inspected. Narrow shell now uses a horizontal navigation strip rather than a full-height sidebar. Existing own/team/company permissions are not broadened.

Rotation follow-up: 187 tests pass (184 regression plus 3 rotation tests). Explicit shift-ID pattern generates at most 93 dated rows atomically, rejects any overlap without replacement, records actor/reason and preserves individual editing. Calendar-day progression includes Friday/Saturday but does not redefine working days. Local MVC save and EN/AR forms inspected.

Diagnostics follow-up: 190 tests pass (187 regression plus 3 counter tests). EN/AR dashboard and non-admin denial inspected. Health endpoints return minimal text, readiness checks database connectivity with timeout and no diagnostic secrets. Admin metrics are action-level aggregate counts/5xx/latency/in-flight, no raw path or query. State resets on process restart, no uptime or job-health guarantee.

Workflow revision follow-up: 193 tests pass (190 regression plus 3 revision tests). New leave/overtime/correction/advance requests retain definition hash and resolved route; immutable JSON revisions reuse content hash. Editing configured steps affects later requests only. Existing requests remain version unknown, not backfilled. Both provider migrations generated, local SQLite upgrade and new leave MVC flow plus EN/AR revision register inspected. Concurrent first use of one definition may return database contention and require retry; PostgreSQL parallel stress remains unverified.

PIP follow-up: 196 tests pass (193 regression plus 3 milestone tests). Reviewer chooses goals, success measures, due dates and support. Owner may record progress but not approve own completion. Completion requires 100% and reviewer evidence, no automatic disciplinary, score, salary or employment effect. Both provider migrations and local MVC creation/update verified, EN/AR inspected.

Performance history follow-up: 197 tests pass (193 regression and 4 PIP tests). Closed-cycle history is access-scoped, preserves missing scores and labels cross-cycle comparability limits. Employee progress-only controls and EN/AR history views inspected locally.

Approval-comment follow-up: 200 tests pass (197 regression, 3 new policy tests). Default approvals remain optional until an administrator enables the per-kind policy. Rejection/cancellation now use an explicit transaction. EN/AR policy save and required inbox controls inspected locally.

Approval reassignment follow-up: 203 tests pass. Both provider migrations generated, local SQLite upgrade and EN/AR current-assignee UI inspected. Reassignment replaces normal authority for only that stage, except SystemAdmin emergency override, and does not approve the request. Original routes remain unchanged. PostgreSQL parallel race stress still unverified.

Sensitive-field follow-up: 207 tests pass (192 remaining regressions and 15 privacy/foundation cases). General employee management, company scope and salary access no longer imply identity or bank access. Owner retains own profile/leave evidence access. New independent grants default to unassigned rather than guessing owner policy. Medical leave type, supporting attachment and decision comments are hidden without the leave-evidence grant; direct file retrieval is guarded. EN/AR general-HR profile/edit screens inspected. Existing unchanged legacy identity values are preserved without blocking unrelated edits; changed identity values remain validated.

Privacy follow-up: department leave cards and leave-balance report type labels are also masked without leave-evidence access; another employee's leave ledger is withheld because free-text reasons can contain private evidence. Broader privacy audit remains explicitly partial.

Privacy audit follow-up: sensitive free-text contract/status/career/assignment/scheduled-change notes are withheld without the identity grant (owner still sees their own). List and manager picker employee copies are redacted, approval rows carry name/code only, turnover exports mask reasons, and other employees' leave-balance report rows are omitted without evidence access. Broader service and attachment-category audit remains open.

Round 34: 211 tests pass, including historical-text redaction, list picker copies, leave ledger/balance service denial and report row omission without the evidence grant. EN/AR restricted profile/history inspected locally; SEC-001 remains Partial for broader service/category review.

Temporary assignment follow-up: explicit return date snapshots current department/branch/title/manager/shift/salary; applies start then return in effective order after downtime, retains historical interval, and never infers a return duration. Financial allocation across payroll periods remains open.

Round 35: 215 tests pass, including linked return restoration, cancellation atomicity, missed-start-and-return catch-up and invalid return validation. Both provider migrations generated, local SQLite upgrade and end-to-end temporary assignment staged, EN/AR form and linked return visually inspected. PostgreSQL parallel stress remains unverified.

Custom-report privacy follow-up: the selectable-column leave dataset now masks private leave types without the separate evidence grant. Nine focused sensitive-field tests pass; other datasets and new modules remain independently scoped.

Round 37: 220 tests pass, including private certificate download, completion/date/file validation, independent effectiveness reviewer, participant-only authorship and append-only outcomes. Both provider migrations generated; local SQLite upgrade, upload/download and EN/AR evidence/form screenshots inspected. Certificate issuer authenticity is not verified, no automated expiry notification or normalized training score is claimed.

Round 38: 224 tests pass. New training plans require a stated need, validated optional planning budget/currency and different-user HR approval before enrolment; decisions cannot be overwritten. Existing courses remain visibly Legacy, with unknown approval history. Local MVC proposal and independent decision plus EN/AR pending/approved form inspections passed. Both provider migrations generated. No vendor booking, spending authority or aggregate budget enforcement is implied.

Round 39: 227 tests pass (224 regression plus 3 requisition tests). New requisitions require stated headcount/need and independent HR approval, cannot receive candidates while pending/rejected, and rejected requests cannot be reopened. Both provider migrations generated; local proposal/approval MVC and EN/AR layout inspected, openings table expanded to full width after initial crowded rendering. Existing approvals remain unknown, not invented.

Round 40: 232 tests pass (227 regression plus 5 interview tests). Internal Cairo-time plans retain interviewer/candidate, duration/place/purpose, cancellation and post-end outcome evidence. Candidate/interviewer overlap and ambiguous/invalid local times are denied; employee/candidate revisions guard concurrent changes, PostgreSQL race stress still unverified. Both provider migrations and local SQLite upgrade passed; EN/AR plan/form inspected after datetime styling fix. No external calendar availability, invitations, room reservations or candidate messages were sent.

Upgrade regression follow-up: existing rows from before training/recruitment approval columns are migrated from empty status to Legacy without creating approvals. The upgrade test starts at the earlier SQLite schema, inserts existing courses/openings, migrates and verifies preserved unknown approval history. Both providers have the corrective migration.

Internal offer follow-up: immutable proposal terms, independent internal approval, source-noted candidate acceptance/decline and withdrawal history are implemented. Candidate rejection withdraws active proposals atomically; Offer-to-Hired requires the latest proposal's recorded acceptance. Nothing sends a letter, signs a contract, creates employment or starts payroll. Six offer tests pass, local MVC proposal and independent approval verified and final EN/AR pixels inspected. SQLite upgrade and both provider migrations generated. External delivery/signing and onboarding handoff remain open.

Certificate alert follow-up: append-only daily alert markers and unique database key prevent repeat daily notifications; only active owner/training managers receive minimal in-app certificate ID/expiry details, never issuer/reference/files or email. Three dedicated tests pass. Single-instance/service-awake limits remain explicit.

Payroll privacy follow-up: bank-file export now requires the separate bank-view grant in addition to payroll export; full historical calculation JSON additionally requires identity access because free-text notes can contain private details. Calculation history lists return metadata only, not payload copies, while stored evidence and hashes stay unchanged. Direct export guards and metadata redaction regression tested; EN/AR finance history inspected. Broader service privacy review remains partial.

Training approval boundary follow-up: appraisal development-course assignment now shares the same approved-or-legacy enrolment gate as the training screen, filters unavailable plans from its picker and creates the enrolment/development record/notification atomically. Dedicated test proves pending plans cannot bypass approval through appraisal linking.

Grade framework follow-up: grade match and 100% weight required on apply; application/content/actor/hash retained append-only and goals copied atomically under appraisal/template revision guards. Four dedicated tests and local MVC creation/application pass; source edits do not rewrite captured evidence. Old templates remain unrestricted, old appraisals have no invented framework evidence.

Benefits record follow-up: seven focused tests cover owner/grant privacy, dependent ownership/plan permission, eligibility evidence, overlap/date/cancellation guards and append-only records. Local MVC plan/enrolment and EN/AR inspected. Per-person cost shares are planning only, never automatic financial obligations. No owner eligibility formula or production grant was assumed.

Employee relations follow-up: five focused tests pass for separate-grant boundaries, audit-content minimization, independent approval, pending/closed transitions and source/event immutability. Local MVC fictional grievance/investigation and EN/AR inspected. Internal records only, never an accusation, legal finding, delivered notice or automatic sanction.

Session step-up follow-up: three focused tests cover password lockout, mandatory enabled authenticator, five-minute expiry, per-session binding and revocation. Local MVC verifies a role-creation POST is blocked and not replayed after verification. EN/AR challenge pixels inspected. No new permissions are granted; other sensitive workflows remain under review.

Bank reconciliation follow-up: four tests cover separate grant, locked-run/exact-net validation, paid/return/retry order and immutable evidence. Local MVC result recording and EN/AR inspected. This is manual recorded evidence, not independent confirmation that a bank transferred funds. Legacy lines without identity snapshots are shown by employee ID instead of inventing historical identity.

Approval reminder follow-up: five tests cover disabled defaults, elapsed stage timing, daily dedupe, new-stage clock, reassignment/inactive/subject restrictions, delegation, decisions stopping reminders and immutable markers. Policy saved through local MVC with session step-up; EN/AR workflow settings inspected. Does not email, approve, escalate or change request routes.

Salary structure follow-up: five tests cover salary/manage permission boundaries, grade and pending-schedule guards, invalid/duplicate components, explicit application with historical payroll unchanged and immutable source/application evidence. Local MVC creation/application and EN/AR inspected. No automatic grade-based update, backdating, real payment, compliance claim or assumed compensation value.

Flexible shift follow-up: nine domain/service tests cover arrival window edges, late/early arrival, personal expected end, overtime, invalid/overnight configurations, permission and no rewrite of earlier records. Local MVC definition and EN/AR shift calendar inspected. Dated assignment uses the existing explicit schedule.

Cost-centre journal follow-up: three focused tests cover permission/export boundaries, legacy unallocated snapshots, balanced totals, privacy-minimal output, spreadsheet-formula escaping and draft/unbalanced guards. EN/AR summary inspected. Internal EGP planning projection only, no posted accounting entries or bank instruction.

Referenced payroll correction follow-up: seven focused tests cover proposer-only unapplied withdrawal, separate permission/default grants, references, amount/date/state validation, independent non-subject decision, one decision sequence, exactly-once application, deduction safety, recalculate retention and immutable source/decision evidence. Local MVC proposal and EN/AR inspected. HR review waits for all proposals to be applied, rejected or withdrawn. Target-run revision guards proposal/decision versus review races, unique proposal/sequence guards competing decisions; PostgreSQL race stress still unverified. No payment or automatic legal backpay calculation.

Advance input hardening: explicit cents, calendar month/year bounds, working employee and non-past start month validated. Request creation, route evidence and notifications are atomic; employee revision serializes competing requests. Nine focused tests including service guards; PostgreSQL contention stress remains unverified. This does not implement loan cancellation or rescheduling.

Lifecycle privacy follow-up: company/team profile scope no longer exposes other employees' general checklist details or probation reasons. Employee/HR retain their private records; another assignee sees only their assigned tasks. Dedicated scope regression added, EN/AR read-only profile inspected. This is a privacy boundary, not a change to employment or payroll.

Financial step-up coverage follow-up: current-session verification now also guards draft recalculation/manual adjustments, Finance approval and paid-state recording, referenced correction proposal/decision/apply/withdrawal, advance settlement and workflow reassignment. UI rejects without replay before any service call; ordinary functional grants remain required.
