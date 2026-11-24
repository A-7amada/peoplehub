# Requirement traceability and delivery register

24 November 2026. Each Open row is unfinished work, not a shipped capability. Partial means the exact remainder is still open. Sources and broader rationale: [external review](REVIEW-2026-11-24.md). Existing feature regressions: [module report](../MODULE-REGRESSION.md).

| ID | Requirement | Priority | Evidence/status |
|---|---|---|---|
| EMP-001 | Employee onboarding and offboarding checklists | P1 | Open |
| EMP-002 | Document repository and expiry alerts | P1 | Open |
| EMP-003 | Dependents | P1 | Open |
| EMP-004 | Effective-dated employee status transitions | P0 | Open |
| EMP-005 | Historical department, title, manager, shift and salary assignments | P0 | Partial: current-change history; future/backdated scheduling and historical allocations open |
| EMP-006 | Probation and confirmation | P1 | Open |
| EMP-007 | Temporary/permanent transfers | P0 | Partial: current transfer history, temporary schedules open |
| EMP-008 | Clearance and asset handover | P1 | Open |
| EMP-009 | HR custom fields | P1 | Open |
| ATT-001 | Rotating/multiple shifts | P1 | Open |
| ATT-002 | Overnight shifts | P1 | Partial: calculator supports, UI pairing open |
| ATT-003 | Missing punch exceptions | P1 | Open |
| ATT-004 | Raw punch ledger and idempotent processing | P0 | Open |
| ATT-005 | Flexible hours | P1 | Open |
| ATT-006 | Remote attendance/geofencing | P2 | Open |
| ATT-007 | Approved attendance period locking | P1 | Open |
| ATT-008 | Reasoned manual corrections with original evidence | P0 | Partial: approval correction/audit existing, immutable raw evidence open |
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
| LEV-009 | Simultaneous request/approval constraints | P0 | Partial: optimistic concurrency; overlap race still open |
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
| PER-004 | Separate feedback identity/comment/aggregate privacy and reviewer threshold | P0 | Open |
| PER-005 | PIP milestones and trends | P1 | Partial: development items existing |
| HR-001 | Asset assignment, handover, condition, return, maintenance | P1 | Open |
| HR-002 | Training needs, budgets, approval, certificates/expiry/effectiveness | P1 | Open |
| HR-003 | Requisitions, interviews, offers and onboarding | P1 | Open |
| HR-004 | Benefits plans/dependents/eligibility/enrolment/cost allocation | P1 | Open |
| HR-005 | Confidential grievances, investigation and discipline | P1 | Open |
| SEC-001 | Field-level identity/salary/bank/medical access | P0 | Partial: salary/bank edit boundary repaired; granular identity/medical grants open |
| SEC-002 | Separation of duties | P0 | Implemented creator cannot finalize own run |
| SEC-003 | Session expiry/revocation/device management | P0 | Partial:8h expiry/security stamp existing, session inventory open |
| SEC-004 | Sensitive-field encryption at rest | P0 | Open: key management required |
| SEC-005 | Append-only audit history | P0 | Implemented repository guard; database-owner tampering not prevented |
| SEC-006 | Failed-login lockout and suspicious detection | P0 | Partial: auth rate limit existing, per-account lockout open |
| SEC-007 | Retention/deletion/access policies | P0 | Open: owner policy decisions required |
| SEC-008 | Malware scan, signatures/private attachments | P0 | Partial: private scoped storage and size/extension checks existing, scan open |
| SEC-009 | Encrypted backup and controlled restore | P0 | Open: encryption/key custody/restore drill |
| SEC-010 | Step-up auth for sensitive admin actions | P0 | Open |
| ARC-001 | Modular monolith domain boundaries and policies | P1 | Existing layers; deeper policy extraction open |
| ARC-002 | PostgreSQL/SQL Server | P0 | PostgreSQL existing |
| ARC-003 | Explicit business transactions | P0 | Partial: new payroll build/recalc/adjust/finalize atomic |
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
