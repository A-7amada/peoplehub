# BRD apply-all regression working log

Portfolio demo with fictional data. Not a real company or client work.

This is an interim evidence log, not the final all-module acceptance report. The earlier MODULE-REGRESSION.md records a different, older build and must not be read as acceptance of later additions.

## Current automated evidence

After lifecycle privacy and financial session-guard review, 362 automated cases passed: 88 + 43 + 50 + 78 + 103 cases including advance validation and controller guard tests. Web build passed with compiler warnings. Tests use SQLite except explicitly environment-gated external paths; no PostgreSQL contention or load claim is made.

Referenced-correction coverage: separate initially-unset permission, source/target period and state, positive cent amounts, independent non-subject decision, unique decision sequence, proposer-only unapplied withdrawal, review gate, exactly-once draft application, source immutability, deduction safety and preservation on recalculation. Local MVC proposal, separate Finance approval and explicit preparer apply passed; temporary local Finance grant removed. EN/AR viewport screenshots inspected after application.

Advance coverage: decimal-cent validation, calendar bounds, non-past start period, working employee, duplicate open request, atomic creation and revision touch. Does not establish cancellation, loan rescheduling or repayment reconciliation.

## Cross-module route smoke

228 local page renders passed across eight seeded roles and both EN/AR languages for all navigation links each role could see. This confirms those routes rendered without an HTTP error under that fixture. It does not prove write workflows, every field permission, every localized label or every branch. Source result is retained as r55-route-checks.json in the working evidence, not shipped as private logs.

## Still required before final acceptance

- Complete the applicable Open/Partial inventory or identify a concrete owner-policy/integration dependency, without quietly treating partial work as complete.
- Repeat full-module functional workflows and negative permission cases on the final build, including all newly added workflows.
- Record current production readiness and integration limits separately from local tests.
- Verify PostgreSQL concurrency, external email, hardware, bank/accounting adapters, production security and operational targets only when the required environment and authority exist. These are not proven by a successful deployment.

Lifecycle privacy audit found that broad profile scope could read checklist/probation details. The service now returns only employee/HR private data or individually assigned tasks, with a dedicated scope regression and manager EN/AR visual check. Broader privacy review remains unfinished.

Financial session-guard review added 14 controller attribute and reject-before-action cases for correction mutations, payroll draft adjustment/recalculation, Finance/paid-state recording, advance settlement and reassignment. A local MVC proposal without elevation redirected before creating a correction; verification returned to Payroll without replay. EN/AR verification prompt pixels inspected. Existing session tests cover expiry, password lockout and enabled authenticator checks. This is not proof of every authorization path.

Null-identity privacy regression: a company-scoped account without an employee link cannot read or complete unassigned lifecycle tasks. Null is not treated as an assignment. No UI change.

Performance null-identity regression: an account without an employee link cannot be treated as manager of an employee whose manager is unset. Direct appraisal access, company-scoped listing and development mutation are denied. No UI change.

Applied correction integrity review: four added cases cover bounded cent manual totals, inability to reduce below applied amounts, retained floors after recalculation, rollback if an applied employee would disappear, and HR-review rejection of missing totals. 11 correction tests total. EN/AR updated guidance inspected. No correction reversal or automatic retro calculation is claimed.

Spreadsheet export review: 15 added serialization cases neutralize formula-prefixed/control-containing report cells and headers, retain CSV escaping and Arabic text, and preserve the exact-value bank interchange serializer separately. Cost-centre report uses the same spreadsheet helper. No report layout changed. This is serializer coverage, not bank adapter certification.

Salary-component write review: six added cases cover missing employee/component rejection, bounded nonnegative cent amounts, explicit zero, permission denial, one-row update and employee revision touch inside a transaction. Existing payroll lines remain unchanged. No UI change or historical component allocation claimed.

Contract/state integrity review: four added cases cover invalid employment status/contract enums, same-calendar-day time bypass, missing employee, atomic current-contract replacement and prevention of renewing a superseded contract twice. Contract writes touch employee revision. No contract template, notice or renewal-policy assumptions were introduced; no UI change.

Maintenance isolation review: each of the five existing jobs runs in its own disposed scope; one failure logs its job name and no longer skips unrelated later work. Four tests cover failure continuation, distinct scopes, disposal and shutdown cancellation. The 15-minute service-awake cadence is unchanged, not durable scheduling or a delivery guarantee. Larger regression partitions were split after a timed-out run; all five smaller partitions passed. No UI change.

Payroll MVC regression on a copied fictional SQLite fixture: preparer create, fresh-session rejection, explicit recalculation/manual adjustment, separate HR review, Finance approval, different final approver, locked UI, explicit paid-state recording and employee access denial passed. Local-only repeatable harness added. EN/AR locked page inspected; stale roadmap wording corrected. 228 navigation renders rerun with exact login-path and nonempty-menu assertions after rate limiting exposed a weak old harness check. The rate-limited partial sweep is not counted as passing full smoke. This does not close all-module functional acceptance.

Leave workflow regression on a copied fictional fixture: six MVC checks passed for working-day submission, unchanged pending balance, required decision comment, configured final manager approval, reasoned future approved cancellation and ledger reversal/no repeat control. EN/AR pixels exposed cramped request dates/actions; the leave review table now uses the full page width with the request form below. Six added integrity cases cover missing/insufficient cancellation balance, non-deducting cancellation, calendar-date semantics, prevention of negative balances from entitlement reduction and rollback without false cancellation evidence. All prior 355 cases reran in five passing partitions, plus six new cases. The initial combined build/test invocation timed out and is not counted as passing. Accrual, cross-year allocation policy and other owner decisions remain unresolved.

Attendance privacy and MVC regression: monthly attendance now uses a minimal detached leave display projection. Another employee needs the separate leave-evidence permission to see the category; reasons, files and employee entities are never returned in that projection. An added test verifies owner/granted/masked views and source preservation. Six local fictional-fixture workflow checks passed: reasoned manual attendance; employee correction; separate manager and HR approval preserving original/corrected times; completed-month lock; locked-period correction rejection; EN/AR team category masking. Final EN/AR private calendar and locked evidence screenshots inspected. 228 role/language navigation renders passed again. All 362 automated cases passed in five partitions. No live device, bank, email or PostgreSQL race proof is implied.

Performance functional workflow: eight local fictional-fixture checks passed for configured weights without 360, manager goal and support milestone, self rating/progress, manager rating and independent milestone completion, unpublished result hiding, HR calibration/closure, acknowledgement plus separate appeal, and independent change recommendation without score overwrite. EN/AR final pixels inspected. Exhausted acknowledgement/appeal form is now hidden. This scenario intentionally does not set owner 360 policy.

Recruitment and training functional workflows: six local fictional-fixture checks passed for pending requisition candidate gate, independent headcount approval and pipeline, independent internal offer approval plus evidence-noted acceptance before hired status, independent course-plan approval, employee enrolment and HR completion, participant reflection and independent effectiveness evidence. EN/AR recruitment and training sections inspected. Repeat enrol control is hidden after enrolment; server duplicate guard remains. No messages, contracts, real hires, calendar invitations or money were committed.

Employee lifecycle/assets functional workflow: four local fixture checks passed for assigned clearance completion with evidence, dependent owner/manager privacy, numeric HR-only custom field, and asset assignment/return plus maintenance/release availability. Employee assignment/return history verified in MVC. Maintenance/release remain unassigned inventory evidence, confirmed separately in the fixture database, not falsely expected in employee history. Initial incorrect harness assertion failed and was corrected before a fresh-copy successful run. EN lifecycle and AR assets pixels inspected.

Benefits/relations functional workflow: four local fixture checks passed for owner-only recorded eligibility versus general manager scope, reasoned append-only cancellation, case investigation/proposal/independent rejection/closure, and subject/manager denial. A temporary relations-management grant was added only to the disposable fictional fixture for the second reviewer, then removed. Production grants were not changed. EN/AR benefit and case evidence pixels inspected. No insurer notice, coverage purchase, actual grievance, disciplinary execution or payroll change was performed.

Documents/reports functional workflow: four local fixture checks passed for generated fictional PDF upload and owner download, broad manager profile/direct-file denial, nonempty EN/AR standard and custom CSV/XLSX responses (workbook signature checked), employee export bypass denial and malformed report rejection. File response checks do not prove every exported cell or workbook layout; serializer tests cover escaping separately. EN/AR document pixels inspected. No real documents or external disclosures were used.
