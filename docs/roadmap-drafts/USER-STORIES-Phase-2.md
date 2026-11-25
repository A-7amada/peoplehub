> **PROPOSED DRAFT, NOT APPROVED. DOCS ONLY.** Stories derived mechanically from `BRD-Phase-2-HR-Functional-Excellence.md` (itself an unapproved draft from the ChatGPT evaluation conversation, https://chatgpt.com/share/6b06e8ab-1b60-83ea-b893-80f4e267ce25). Not in scope for current BRD v3 work.
>
> **Assumptions to confirm:** (1) Ahmed's WhatsApp voice note of 2026-11-25 15:58 EET was partly garbled; it was read as "split the three phase BRDs into stories and keep them in the repo until current work is finished" and as meaning stories with their acceptance criteria. (2) No story actors were invented: each story keeps the draft's own "The system shall..." statement, its acceptance criteria and the phase-level priority (High); the draft carries no per-requirement priority, estimate or owner. (3) Story and requirement numbering follow the draft: story `US-PH2-NNN` maps to `PH2-BR-NNN`. (4) Before any phase starts, gap-analyse each story against `docs/TRACEABILITY.md` and the code, since many describe features that already exist in part.


# User stories: Phase 2 (High)

Source BRD: `BRD-Phase-2-HR-Functional-Excellence.md`

## US-PH2-001: Leave accrual policies

- Source requirement: PH2-BR-001
- Priority: High (phase-level)
- Status: proposed, not started

**Story:** The system shall calculate leave entitlement and accrual based on configurable employee and policy conditions.

**Acceptance criteria**
- Policies support accrual frequency and accrual amount.
- Eligibility can depend on employment type, grade, location, or service period where configured.
- Accrual start, pause, and end conditions are explicit.
- Proration is supported for joining, termination, and policy changes.
- Accrual transactions are traceable and are not silently overwritten.
- HR can preview the impact of a policy change before applying it.

## US-PH2-002: Hourly and partial-day leave

- Source requirement: PH2-BR-002
- Priority: High (phase-level)
- Status: proposed, not started

**Story:** The system shall support leave requests for a full day, half day, or configured hourly duration.

**Acceptance criteria**
- Allowed request units are configurable by leave type.
- Requests are validated against working schedules and non-working days.
- Balance deduction follows the configured policy.
- Overlapping requests and attendance conflicts are detected.
- Approval and cancellation history is retained.

## US-PH2-003: Leave carry-forward and expiry

- Source requirement: PH2-BR-003
- Priority: High (phase-level)
- Status: proposed, not started

**Story:** The system shall support policy-controlled carry-forward and expiry.

**Acceptance criteria**
- Carry-forward limits and expiry dates are configurable.
- The system identifies balances approaching expiry.
- Expired balances are recorded as explicit transactions.
- Employees and HR can view the balance breakdown.
- Retroactive policy changes require controlled recalculation and audit.

## US-PH2-004: Leave encashment

- Source requirement: PH2-BR-004
- Priority: High (phase-level)
- Status: proposed, not started

**Story:** The system shall support an approved process for converting eligible leave balance into a financial amount.

**Acceptance criteria**
- Eligibility, limits, valuation basis, and approval path are configurable.
- Encashment requests are validated against available eligible balance.
- Approved encashment creates a traceable payroll input or financial handoff.
- The same leave balance cannot be encashed twice.
- Applicable local legal rules are validated before production activation.

### Attendance Management

## US-PH2-005: Attendance policy and exception completion

- Source requirement: PH2-BR-005
- Priority: High (phase-level)
- Status: proposed, not started

**Story:** The system shall support configurable attendance policies and end-to-end exception resolution.

**Acceptance criteria**
- Policies cover grace periods, late arrival, early departure, missing punches, overtime eligibility, and rounding where applicable.
- Exceptions can be assigned, reviewed, corrected, and closed by authorized users.
- Original punch evidence remains preserved.
- Every correction records actor, reason, timestamp, and before/after values.
- Locked attendance periods cannot be silently modified.

## US-PH2-006: Mobile attendance capture

- Source requirement: PH2-BR-006
- Priority: High (phase-level)
- Status: proposed, not started

**Story:** The system shall support authorized mobile attendance capture where enabled by the employer.

**Acceptance criteria**
- Capture methods can include GPS/geofence and QR, subject to tenant policy and device capability.
- The system records the minimum evidence required for the configured method.
- Location collection is limited to the attendance action and disclosed to employees.
- Configured geofence radius and location accuracy rules are enforced.
- Spoofing and suspicious capture patterns are flagged where technically feasible.
- Manual override remains permission-controlled and auditable.

## US-PH2-007: Biometric device integration

- Source requirement: PH2-BR-007
- Priority: High (phase-level)
- Status: proposed, not started

**Story:** The system shall support integration with approved biometric attendance devices.

**Acceptance criteria**
- Device events are ingested through a documented adapter/API or controlled import process.
- External device identifiers map to employee identities without relying on names alone.
- Duplicate events are handled idempotently.
- Failed imports can be retried without duplicating punches.
- Device connectivity and last successful synchronization are visible to administrators.

## US-PH2-008: Overtime management

- Source requirement: PH2-BR-008
- Priority: High (phase-level)
- Status: proposed, not started

**Story:** The system shall support overtime request, approval, calculation input, and reporting.

**Acceptance criteria**
- Overtime rules are configurable by tenant and applicable employee group.
- Overtime can require pre-approval, post-approval, or both according to policy.
- Eligible overtime is derived from approved attendance evidence and policy.
- Overtime is transferred to payroll through a traceable input.
- Overtime cannot be paid twice for the same approved source period.

### Payroll Management

## US-PH2-009: Payroll proration

- Source requirement: PH2-BR-009
- Priority: High (phase-level)
- Status: proposed, not started

**Story:** The system shall calculate prorated salary and eligible components for partial payroll periods.

**Acceptance criteria**
- Proration supports joining, termination, unpaid leave, and approved salary changes.
- The divisor and calendar-day/working-day basis are explicit per policy.
- Each component identifies whether it is prorated.
- Calculation details are available for review.
- Boundary tests cover month length, leap years, and effective dates.

## US-PH2-010: Salary structures and compensation changes

- Source requirement: PH2-BR-010
- Priority: High (phase-level)
- Status: proposed, not started

**Story:** The system shall support structured compensation packages and effective-dated changes.

**Acceptance criteria**
- Salary structures support configurable components, earning/deduction classification, and calculation basis.
- Components can be fixed, percentage-based, or derived from approved inputs where supported.
- Salary changes have effective dates and approval history.
- Historical payroll uses the salary structure effective for the relevant period.
- Access to compensation data is restricted by role and scope.

## US-PH2-011: Retroactive payroll adjustments

- Source requirement: PH2-BR-011
- Priority: High (phase-level)
- Status: proposed, not started

**Story:** The system shall support controlled adjustments for prior periods.

**Acceptance criteria**
- Adjustments reference the original employee, period, and source reason.
- The system calculates or records the delta without rewriting a finalized payroll.
- Adjustments require the configured review and approval.
- Duplicate adjustment submissions are prevented.
- Reports distinguish current-period earnings from prior-period adjustments.

## US-PH2-012: Egypt payroll localization package

- Source requirement: PH2-BR-012
- Priority: High (phase-level)
- Status: proposed, not started

**Story:** The system shall support an approved Egypt payroll policy package for the defined release scope.

**Acceptance criteria**
- Tax and social insurance rules are effective-dated and versioned.
- Employee and employer contributions are represented separately.
- Applicable thresholds, limits, exemptions, and rounding rules are configurable.
- Payroll outputs can be reconciled to approved statutory reporting requirements.
- Every rule has a documented source, effective date, test cases, and specialist sign-off.
- No statutory rule is activated solely on an unverified assumption.

## US-PH2-013: End-of-service and termination settlement

- Source requirement: PH2-BR-013
- Priority: High (phase-level)
- Status: proposed, not started

**Story:** The system shall support termination-related financial calculation inputs and settlement workflow for the approved jurisdiction.

**Acceptance criteria**
- Eligibility and calculation rules are effective-dated and jurisdiction-specific.
- Inputs include relevant service dates, compensation basis, leave balances, and approved deductions.
- Calculation output is reviewable and traceable.
- Settlement requires authorized approval.
- Final values can be handed off to payroll/finance without duplicate payment.

### Recruitment and Benefits

## US-PH2-014: Recruitment workflow

- Source requirement: PH2-BR-014
- Priority: High (phase-level)
- Status: proposed, not started

**Story:** The system shall support a complete recruitment workflow for approved hiring processes.

**Acceptance criteria**
- HR can create vacancies with role, department, location, headcount, and approval status.
- Candidates can be created and linked to one or more vacancies.
- CVs and candidate documents are access-controlled.
- Interview stages, schedules, feedback, and decisions are recorded.
- Candidate status transitions follow configured rules.
- An accepted candidate can be handed off to onboarding without re-entering core data.

## US-PH2-015: Benefits administration

- Source requirement: PH2-BR-015
- Priority: High (phase-level)
- Status: proposed, not started

**Story:** The system shall support configurable employee benefits and enrollment.

**Acceptance criteria**
- Benefits plans define eligibility, enrollment windows, coverage, and employer/employee contribution where applicable.
- Eligibility is evaluated using approved employee attributes.
- Enrollment, change, and cancellation actions are auditable.
- Benefit deductions or contributions can be handed off to payroll.
- Sensitive dependent and beneficiary information has restricted access.

### Employee Experience and Reporting

## US-PH2-016: Employee self-service

- Source requirement: PH2-BR-016
- Priority: High (phase-level)
- Status: proposed, not started

**Story:** The system shall provide employees with self-service access to permitted HR information and requests.

**Acceptance criteria**
- Employees can view their profile, permitted documents, leave balances, attendance summary, payslips, and request history.
- Employees can submit and track supported HR requests.
- Employees cannot view another employee's private information unless explicitly authorized.
- Changes to personal information follow configurable review/approval rules.
- The experience is usable on supported mobile screen sizes.

## US-PH2-017: Mobile application or responsive mobile experience

- Source requirement: PH2-BR-017
- Priority: High (phase-level)
- Status: proposed, not started

**Story:** The system shall provide a supported mobile experience for employee and manager tasks.

**Acceptance criteria**
- The approved delivery approach is documented as native app, cross-platform app, or responsive web/PWA.
- Authentication, session revocation, and device security follow Phase 1 controls.
- Employees can complete priority self-service tasks.
- Managers can process assigned approvals.
- Offline actions, if supported, are queued and reconciled safely rather than silently lost.

## US-PH2-018: Notification delivery management

- Source requirement: PH2-BR-018
- Priority: High (phase-level)
- Status: proposed, not started

**Story:** The system shall deliver and track important HR notifications.

**Acceptance criteria**
- Supported channels are configurable by tenant and event type.
- Delivery attempts and provider outcomes are recorded.
- Retries are bounded and duplicate notifications are controlled.
- Templates support localization and tenant branding where approved.
- Users can manage permitted notification preferences without suppressing mandatory legal or security notices.

## US-PH2-019: HR operational reports

- Source requirement: PH2-BR-019
- Priority: High (phase-level)
- Status: proposed, not started

**Story:** The system shall provide standard reports for core HR operations.

**Acceptance criteria**
- Reports cover headcount, employee movements, leave balances, attendance exceptions, overtime, payroll summaries, and recruitment pipeline.
- Data access follows the same authorization and tenant boundaries as the source modules.
- Reports support agreed filters and export formats.
- Large exports run asynchronously and are audited.
- Scheduled reports are delivered only to authorized recipients.
