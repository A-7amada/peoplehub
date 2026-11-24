# BRD apply-all regression working log

Portfolio demo with fictional data. Not a real company or client work.

This is an interim evidence log, not the final all-module acceptance report. The earlier MODULE-REGRESSION.md records a different, older build and must not be read as acceptance of later additions.

## Current automated evidence

After lifecycle privacy and financial session-guard review, 355 automated cases passed: 88 + 43 + 50 + 77 + 97 cases including advance validation and controller guard tests. Web build passed with compiler warnings. Tests use SQLite except explicitly environment-gated external paths; no PostgreSQL contention or load claim is made.

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
