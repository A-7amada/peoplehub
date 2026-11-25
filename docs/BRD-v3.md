# PeopleHub BRD v3.0

## 1. Document control
Version3.0, 24 November2026. Owner: product owner. Status: demo business baseline plus proposed expansion. Preserves the existing combined specification as historical delivery evidence. Not a legal or production-readiness certificate.

## 2. Executive summary
Bilingual employee lifecycle, attendance, leave, approvals, performance and payroll in a single-company demonstration. Extend into a controlled HRMS using an evidence-first roadmap. All hosted records remain fictional.

## 3. Business objectives
Reduce manual HR tracking while preserving who changed each record, why, the applicable period and who approved it. Protect employee privacy and prevent silent changes to closed financial results.

## 4. Scope
Existing features remain. The complete expansion inventory, priorities and exact delivery status are in [TRACEABILITY.md](TRACEABILITY.md). Multi-company, banking, government, SSO and mobile applications are future requirements, not current products.

## 5. Stakeholders and roles
Employee, department manager, HR specialist, HR manager, attendance officer, payroll officer, finance manager, top management and system administrator. Functional permission, employee data scope and field visibility are separate checks. Payroll creator cannot act as final approver even if holding multiple roles.

## 6. Current processes
Single-company demo, current employee assignments, sequential approval routes, annual leave balances, calculated attendance and monthly payroll. Existing limitations remain visible in the feature matrix.

## 7. Proposed processes
Historical employee assignment changes; source attendance evidence; leave transaction ledger; calculation snapshots; controlled adjustments; documented lifecycle checklists and exception handling.

## 8. Functional requirements
Requirement IDs and priorities are normative in TRACEABILITY.md. No documented feature is removed. Incompatible behavior changes are recorded below: finalized payroll reopening is retired. Subsequent-period explicit-amount referenced correction proposals now retain an earlier locked source line/version, independent Finance decision and exactly-once draft application. Automatic retro calculation, tax-period restatement and reversal remain open. Draft manual adjustments are still available. Existing finalized/paid runs retain their values and lack fabricated snapshots.

## 9. Business rules
- Egyptian workweek excludes Friday and Saturday. Existing holiday list supplements it.
- Leave balance is checked at submission and deducted at final approval, not reserved. Final approval rechecks availability. Approved future leave cancellation requires a reason and restores exactly once. Started leave cannot be cancelled by this workflow. Accrual/negative balance policy beyond current no-negative rule requires explicit configuration design.
- Current assignment changes take effect on their recording date, from inclusive/to exclusive. Same-day entries preserve sequence. Future assignment/status changes and temporary assignment linked returns are implemented. Backdated changes and period cost allocations remain open.
- Current payroll includes active/on-leave employees hired by month-end. Monthly eligibility and proration expansion remains open and must not be described as implemented.
- Only Draft payroll can recalculate or adjust. HR review waits until referenced correction proposals are applied, rejected or withdrawn. Recalculation preserves earlier calculation evidence. HR review then Finance approval then final approval remain sequential. Final payroll cannot reopen. Creator and final approver must differ. Marking paid records demo state, not proof of bank receipt. Export never marks a run paid.
- EG-DEMO-2026-v1 is the sole currently implemented demo calculation policy. Its exact coefficients/brackets are recorded in each new snapshot. It is not specialist-validated, including high-income exceptions, termination and statutory limits. No automatic switch of tax law by calendar year is claimed.
- Published performance does not automatically pay or promote an employee. Explicit award handoff remains required.
- Pending approval routes retain their route snapshot, but current manager resolution is existing behavior; explicit administrator reassignment of the current pending stage handles an unavailable approver with retained history. Automatic absence detection and escalation remain open.

## 10. Workflow requirements
Sequential any-one-of-roles or direct-manager steps, no self-approval, dated delegation, immutable observed definition hashes, retained original routes, administrator pending-stage reassignment and per-kind required approval comments. Explicit opt-in captured-any/all/quorum role groups are implemented with frozen employee rosters and distinct-human votes. SLA escalation remains partial, with configured reminders but no automatic escalation. A label saying enterprise workflow does not implement these semantics.

## 11. Reporting
Preserve CSV/XLSX and builder reports. New snapshots retain organizational and financial inputs at calculation time. Full historical organizational reporting remains open.

## 12. Integration requirements
Use authenticated, versioned adapters, external reference IDs, idempotency, retries, logs and reconciliation. Real integrations require service credentials and test contracts. Device simulation is not a physical-device certification.

## 13. Non-functional requirements
Proposed production targets from the review, not measured service promises: normal-request p95<=2s, monthly availability99.9%,1000 concurrent users, RPO15min/RTO4h, daily backup with restore verification, WCAG2.2AA and agreed current major browsers. Each target needs an environment, workload, instrumented test and owner agreement before acceptance. Free hosting does not meet an enterprise availability promise.

## 14. Security and compliance
Sensitive fields must be independently authorized. The current batch closes employee-edit salary/bank leakage. Separate identity, bank-view/bank-edit and leave-evidence grants, sensitive profile/history/export redaction, session inventory/revocation and password lockout are implemented with the limits in TRACEABILITY.md. New sensitive grants remain unset for non-SystemAdmin roles until owner configuration. Broader privacy audit, encryption/key custody, malware scanning and encrypted backup remain open. Application append-only guards are not cryptographic protection against a database owner. Never expose passwords/secrets in exports beyond controlled backups.

## 15. Assumptions and constraints
Fictional single-company demo; no legal payroll certification; no production employee data. Keep modular monolith architecture. Do not add infrastructure merely for appearance.

## 16. Risks and decisions
Legal payroll/leave rules, retention/deletion periods, encryption-key custody, enterprise service contracts and paid infrastructure require product/business decisions. Review defaults and NFR examples are proposals, not proof of compliance.

## 17. Acceptance scenarios
- Given a draft payroll calculation, when recalculated, then old input/output snapshots remain immutable and a new version is recorded.
- Given a finalized payroll, changing current employee bank/name/department data must not silently alter its snapshotted bank export or payslip identity. Legacy runs show their evidence gap.
- Given the payroll creator, when attempting final approval, deny even with finalization permission.
- Given an HR specialist, the employee-edit response must not contain salary/bank values, and tampered POST fields cannot overwrite them.
- Given approved future leave, cancellation requires a reason, reverses its deduction exactly once and removes it from pending work.
- Given a salary/department change, preserve the prior assignment and record its effective boundary and reason. Never invent missing pre-upgrade history.
- Given immutable evidence, ordinary application writes/deletes are rejected.
- Given failed bank export, payroll paid state must not change.

## 18. Traceability
See TRACEABILITY.md for every review area, current status and remaining work. Automated tests and live scenario results are implementation evidence, not substitutes for production verification.

## 19. Baseline reconciliation, 24 November 2026
Updates above reconcile delivered scheduling/security/workflow behavior, not completion of the expansion roadmap. The feature matrix remains authoritative for Open and Partial requirements. Latest verification: 216 automated tests; PostgreSQL parallel race stress and production NFR targets remain unverified.

### Confidential case investigator access, 2026-11-25
The existing all-case relations manager may explicitly grant or revoke one active account per case, with a reason and recent password step-up. These changes are append-only; no automatic assignments or production role grants are seeded. A case investigator reads assigned case sources and events and appends investigation notes, but cannot create cases, propose/decide discipline, close cases or manage access. Linked affected employees, inactive accounts and employees no longer working cannot use investigator access. Revocation prevents future reads/writes, not information already read. Case revision contention serializes competing access/evidence writes; concurrency stress on PostgreSQL remains unverified. Internal evidence is not a delivered warning or legal finding and creates no payroll, employment or notice effect. Evidence attachments, employee submissions/appeals and legal process review remain open.

### Private case evidence files, 2026-11-25
Case managers and currently assigned investigators may retain PDF/JPEG/PNG evidence up to 2 MB with source/context, SHA-256 and actor/time. These files use a separate append-only case store, never employee-owned general attachments. Case lists carry metadata only. Downloads independently enforce current case access, force attachment disposition and no-store/private plus nosniff. Revoked or inactive investigators cannot download; closed cases preserve authorized reads but reject new files. Existing signature checks do not scan malware or authenticate evidence. Repository guards prevent update/delete, not database-owner tampering. No employee submission/appeal or legal chain-of-custody certification is implied.

### Benefit cost-centre planning snapshot
Authorized benefits managers may view full monthly planning shares of enrolments active on an explicit date, grouped by recorded cost centre and currency. Start/end dates are inclusive; cancellation excludes its effective date onward. Dependents are separate enrolments. Employee counts are distinct within each row, not additive across cost centres/currencies. Multiple plan enrolments are not unique-person counts. No currency conversion, daily proration, invoice, payroll deduction, transfer or insurer coverage verification. No grants or policy values are selected automatically.

## Employee grievance intake boundary
Active accounts linked to Active/OnLeave employees may submit their own immutable internal statement and view their own intake and explicit responses. Relations-management accounts may view intake and append employee-visible responses, excluding submitter and subject aliases. General HR, manager, company-scope and investigator case grants do not expose another employee's intake. Intake never creates a case or investigation grant; private case notes/files never appear in this view. No email, anonymous reporting, emergency response, deadline, legal finding, served notice, discipline or payroll/status action. Appeals/legal review remain open.


### Annual course estimate planning boundary
The training-management audience can inspect a read-only annual course-estimate summary by scheduled calendar year, approval state and currency. Each course counts once; missing or unusable estimates are reported separately, not treated as zero. Rejected plans and legacy approval-unknown records remain distinct. There is no currency conversion, department allocation, enforced cap, spending authorization or actual-expenditure claim. HR-002 remains Partial.


### Internal grievance response appeals
Original submitters may retain one immutable appeal for each employee-visible intake response. A different relations manager, excluding responder and subject identities, records one reasoned employee-visible disposition: response upheld or reconsideration recommended. A recommendation never executes a sanction or changes the original records. No private investigation details, case access, notification, legal deadline or served notice is created. Investigation/discipline appeals and legal process review remain open.


### Employee import validation boundary
Employee managers may upload a bounded UTF-8 CSV for dry-run checks only: exact eight-column nonfinancial schema, maximum256KiB/200data rows, format/duplicate/current-reference checks. Uploads use an in-memory multipart buffer and are not retained; no employee, account, salary, balance or history record is written. Results are not approval or an import guarantee. Execution, staging, mapping, historical reconciliation and rollback remain open.


### Salary template category eligibility
New immutable salary structures may restrict eligibility to FullTime, PartTime or Contractor, in addition to job grade. An omitted category, including every pre-upgrade template, stays unrestricted. Explicit individual application rechecks current grade/category and never changes employment category. Current salary/component effects remain explicit, today-only, with retained evidence; no automatic payroll recalculation or publication lifecycle is implied.
