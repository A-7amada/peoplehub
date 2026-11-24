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
| EMP-007 | Temporary/permanent transfers | P0 | Partial: current and future permanent transfer history, temporary return schedule open |
| EMP-008 | Clearance and asset handover | P1 | Implemented clearance tasks and separate asset assignment/return with condition and receipt evidence; clearance is not auto-approved |
| EMP-009 | HR custom fields | P1 | Implemented HR-only typed text/date/number fields, validation and audit |
| ATT-001 | Rotating/multiple shifts | P1 | Implemented dated employee shift assignments and configurable shift definitions; repeating rotation generator open |
| ATT-002 | Overnight shifts | P1 | Implemented cross-midnight device pairing, manual/correction checkout normalization and previous-night self checkout; post-shift delayed punches beyond end remain exceptions |
| ATT-003 | Missing punch exceptions | P1 | Partial: missing checkout displayed without invented time; exception inbox/reprocessing open |
| ATT-004 | Raw punch ledger and idempotent processing | P0 | Implemented: append-only punches, normalized batch/retry dedupe and unique idempotency key; concurrent losing request may require retry |
| ATT-005 | Flexible hours | P1 | Open |
| ATT-006 | Remote attendance/geofencing | P2 | Open |
| ATT-007 | Approved attendance period locking | P1 | Implemented completed-month lock, reason, no reopening, mutation guards and retained late raw punches; broader financial-period coordination remains open |
| ATT-008 | Reasoned manual corrections with original evidence | P0 | Implemented: approved correction retains before/after, reason and original raw punches |
| ATT-009 | Branch/group holiday calendars | P1 | Open |
| ATT-010 | Category/shift grace periods | P1 | Partial: shift grace existing, categories open |
| LEV-001 | Accrual by joining date/month and eligibility rules | P0 | Open: policy decision required |
| LEV-002 | Half-day and hourly leave | P1 | Open |
| LEV-003 | Carry-forward expiry | P1 | Open |
| LEV-004 | Append-only balance adjustment ledger | P0 | Implemented this batch |
| LEV-005 | Leave encashment | P1 | Partial: settlement estimate only |
| LEV-006 | Approved leave cancellation, reversal, reason and notification | P0 | Implemented this batch |
| LEV-007 | Notice-period and negative-balance policies | P0 | Partial: no negative balance; notice policy open |
| LEV-008 | Department calendar | P1 | Open |
| LEV-009 | Simultaneous request/approval constraints | P0 | Partial: employee revision serializes concurrent request creation, final balance check is atomic; PostgreSQL race stress remains unverified |
| PAY-001 | Grade/category salary structures | P0 | Open |
| PAY-002 | Payroll eligibility | P0 | Partial: active/on-leave employed by month end; terminal-period eligibility open |
| PAY-003 | Joining/termination proration | P0 | Partial: settlement fixed, regular payroll proration open |
| PAY-004 | Retroactive pay and referenced adjustments | P0 | Open |
| PAY-005 | Bonuses/commissions/incentives | P1 | Partial: bonus existing |
| PAY-006 | Deduction priority and statutory limits | P0 | Open: qualified specialist review required |
| PAY-007 | Loan cancellation/reschedule/repayment | P1 | Partial: early settlement existing |
| PAY-008 | Immutable calculation inputs, policy/version/hash and history | P0 | Implemented for new calculations; old history not reconstructed |
| PAY-009 | Approved payroll locking and separate final approver | P0 | Implemented this batch |
| PAY-010 | Bank-result reconciliation | P1 | Open |
| PAY-011 | Cost-centre journals | P2 | Open |
| PAY-012 | Complete final settlement and approval | P0 | Partial: estimator only |
| PAY-013 | Single versioned Egyptian policy, legal validation status | P0 | Documented EG-DEMO-2026-v1, not legally validated |
| WFL-001 | SLA, reminders and escalation | P1 | Open |
| WFL-002 | Parallel approvals and all/one/quorum rules | P1 | Open |
| WFL-003 | Dynamic hierarchy/department approvers | P1 | Partial: direct manager/role existing |
| WFL-004 | Explicit workflow versions | P1 | Partial: route snapshot existing |
| WFL-005 | Pending task reassignment | P1 | Open |
| WFL-006 | Recall/substitute/unavailable approver | P1 | Partial: pending cancel/delegation existing |
| WFL-007 | Required decision comments | P1 | Partial: rejection existing |
| PER-001 | Grade competency framework and rating configuration | P1 | Open |
| PER-002 | Multiple cycles | P1 | Existing |
| PER-003 | Employee result acknowledgement and appeal | P1 | Open |
| PER-004 | Separate feedback identity/comment/aggregate privacy and reviewer threshold | P0 | Implemented configurable separate grants and threshold gate; owner policy values remain unset. Existing finalized scores preserved |
| PER-005 | PIP milestones and trends | P1 | Partial: development items existing |
| HR-001 | Asset assignment, handover, condition, return, maintenance | P1 | Implemented inventory, unique tags, explicit assignment/return, maintenance/release/retirement and append-only movement evidence |
| HR-002 | Training needs, budgets, approval, certificates/expiry/effectiveness | P1 | Open |
| HR-003 | Requisitions, interviews, offers and onboarding | P1 | Open |
| HR-004 | Benefits plans/dependents/eligibility/enrolment/cost allocation | P1 | Open |
| HR-005 | Confidential grievances, investigation and discipline | P1 | Open |
| SEC-001 | Field-level identity/salary/bank/medical access | P0 | Partial: salary/bank edit boundary repaired; granular identity/medical grants open |
| SEC-002 | Separation of duties | P0 | Implemented creator cannot finalize own run |
| SEC-003 | Session expiry/revocation/device management | P0 | Partial:8h expiry/security stamp existing, session inventory open |
| SEC-004 | Sensitive-field encryption at rest | P0 | Open: key management required |
| SEC-005 | Append-only audit history | P0 | Implemented repository guard; database-owner tampering not prevented |
| SEC-006 | Failed-login lockout and suspicious detection | P0 | Implemented password lockout after five failures for 15 minutes plus audit; broader anomaly detection open |
| SEC-007 | Retention/deletion/access policies | P0 | Open: owner policy decisions required |
| SEC-008 | Malware scan, signatures/private attachments | P0 | Partial: private scoped storage, size/extension and file signature checks; malware scanner remains unconnected |
| SEC-009 | Encrypted backup and controlled restore | P0 | Open: encryption/key custody/restore drill |
| SEC-010 | Step-up auth for sensitive admin actions | P0 | Open |
| ARC-001 | Modular monolith domain boundaries and policies | P1 | Existing layers; deeper policy extraction open |
| ARC-002 | PostgreSQL/SQL Server | P0 | PostgreSQL existing |
| ARC-003 | Explicit business transactions | P0 | Partial: payroll, attendance, device ingestion, final approvals, leave request/adjustment and employee save atomic; remaining service paths under review |
| ARC-004 | Durable background scheduler | P1 | Open |
| ARC-005 | Distributed cache/multi-instance | P2 | Open |
| ARC-006 | Private object storage | P1 | Open |
| ARC-007 | Integration outbox/domain events/idempotency/logs/retry/reconciliation/versioning | P1 | Partial: email outbox existing |
| ARC-008 | Optimistic concurrency/database constraints | P0 | Existing, further race tests open |
| ARC-009 | Read projections/metrics/tracing/health | P1 | Open |
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

Asset follow-up: 164 tests pass (160 regression plus 4 asset tests). Registration, assignment and receipt evidence exercised locally through MVC forms; EN/AR viewport screenshots inspected. HR controls inventory/movements; employee sees only own assigned assets/history. Return validates the holder. No automatic clearance, deduction, valuation or depreciation. PostgreSQL schema generated, live asset migration not yet deployed.
