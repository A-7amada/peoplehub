> **PROPOSED DRAFT, NOT APPROVED.** Generated in a ChatGPT evaluation conversation (https://chatgpt.com/share/6b06e8ab-1b60-83ea-b893-80f4e267ce25, 2026-11-25). It is not part of BRD v3, has not been reviewed against the repository, and is not in scope for current work. Per Ahmed Hamada's WhatsApp voice note of 2026-11-25 15:57 EET: take no action on the evaluation itself, keep these drafts in the repo, and apply the architecture step first once the current BRD v3 work is finished.

# BRD — Phase 2: HR Functional Excellence

**Product:** PeopleHub  
**Phase:** 2 of 3  
**Priority:** High  
**Document status:** Proposed  
**Primary outcome:** Complete and improve the high-frequency HR workflows required by small and mid-sized organizations, with reliable employee self-service and operational controls.

---

## 1. Executive Summary

After the platform foundation is secure and operationally reliable, PeopleHub should close the functional gaps that affect daily HR work. This phase prioritizes attendance, leave, payroll processing, recruitment, benefits, notifications, and employee experience.

The goal is not to add every possible HR feature. The goal is to deliver complete end-to-end workflows that HR teams and employees can use without spreadsheets or manual workarounds.

## 2. Business Objectives

1. Support accurate leave entitlement, accrual, requests, approvals, and balances.
2. Support common attendance patterns and device/mobile capture scenarios.
3. Complete payroll input and calculation scenarios needed by the initial target market.
4. Improve recruitment from vacancy management through candidate decision and onboarding handoff.
5. Introduce benefits administration where required by target customers.
6. Give employees a consistent self-service experience across web and mobile.
7. Improve notifications and reporting so users can act on HR events promptly.

## 3. Scope

### In scope

- Leave accrual, hourly/half-day leave, encashment, carry-forward, and policy eligibility.
- Attendance exception handling, geofencing/mobile capture, and device integration foundations.
- Payroll proration, retroactive adjustments, salary structures, and validated Egypt localization requirements.
- Recruitment workflow improvements.
- Benefits eligibility and enrollment.
- Notification delivery tracking and employee self-service.
- Core HR operational reports and scheduled report delivery.

### Out of scope

- Predictive AI and workforce forecasting.
- Full ERP/accounting suite.
- Broad integration marketplace.
- Advanced succession planning and enterprise talent marketplace.
- Country-specific payroll beyond the approved first localization package.

## 4. Stakeholders and User Classes

| Stakeholder | Interest / responsibility |
|---|---|
| Employee | Submit requests, review balances, view payslips and personal records |
| Line Manager | Review team attendance, leave, and assigned approvals |
| HR Administrator | Configure policies and manage employee lifecycle |
| Attendance Officer | Reconcile punches, shifts, and exceptions |
| Payroll Specialist | Manage payroll inputs, calculations, and adjustments |
| Recruiter / Hiring Manager | Manage vacancies, candidates, interviews, and offers |
| Benefits Administrator | Configure plans and manage enrollment |
| Finance | Review payroll totals and financial outputs |

## 5. Business Requirements

### Leave Management

#### PH2-BR-001 — Leave accrual policies

The system shall calculate leave entitlement and accrual based on configurable employee and policy conditions.

**Acceptance criteria**
- Policies support accrual frequency and accrual amount.
- Eligibility can depend on employment type, grade, location, or service period where configured.
- Accrual start, pause, and end conditions are explicit.
- Proration is supported for joining, termination, and policy changes.
- Accrual transactions are traceable and are not silently overwritten.
- HR can preview the impact of a policy change before applying it.

#### PH2-BR-002 — Hourly and partial-day leave

The system shall support leave requests for a full day, half day, or configured hourly duration.

**Acceptance criteria**
- Allowed request units are configurable by leave type.
- Requests are validated against working schedules and non-working days.
- Balance deduction follows the configured policy.
- Overlapping requests and attendance conflicts are detected.
- Approval and cancellation history is retained.

#### PH2-BR-003 — Leave carry-forward and expiry

The system shall support policy-controlled carry-forward and expiry.

**Acceptance criteria**
- Carry-forward limits and expiry dates are configurable.
- The system identifies balances approaching expiry.
- Expired balances are recorded as explicit transactions.
- Employees and HR can view the balance breakdown.
- Retroactive policy changes require controlled recalculation and audit.

#### PH2-BR-004 — Leave encashment

The system shall support an approved process for converting eligible leave balance into a financial amount.

**Acceptance criteria**
- Eligibility, limits, valuation basis, and approval path are configurable.
- Encashment requests are validated against available eligible balance.
- Approved encashment creates a traceable payroll input or financial handoff.
- The same leave balance cannot be encashed twice.
- Applicable local legal rules are validated before production activation.

### Attendance Management

#### PH2-BR-005 — Attendance policy and exception completion

The system shall support configurable attendance policies and end-to-end exception resolution.

**Acceptance criteria**
- Policies cover grace periods, late arrival, early departure, missing punches, overtime eligibility, and rounding where applicable.
- Exceptions can be assigned, reviewed, corrected, and closed by authorized users.
- Original punch evidence remains preserved.
- Every correction records actor, reason, timestamp, and before/after values.
- Locked attendance periods cannot be silently modified.

#### PH2-BR-006 — Mobile attendance capture

The system shall support authorized mobile attendance capture where enabled by the employer.

**Acceptance criteria**
- Capture methods can include GPS/geofence and QR, subject to tenant policy and device capability.
- The system records the minimum evidence required for the configured method.
- Location collection is limited to the attendance action and disclosed to employees.
- Configured geofence radius and location accuracy rules are enforced.
- Spoofing and suspicious capture patterns are flagged where technically feasible.
- Manual override remains permission-controlled and auditable.

#### PH2-BR-007 — Biometric device integration

The system shall support integration with approved biometric attendance devices.

**Acceptance criteria**
- Device events are ingested through a documented adapter/API or controlled import process.
- External device identifiers map to employee identities without relying on names alone.
- Duplicate events are handled idempotently.
- Failed imports can be retried without duplicating punches.
- Device connectivity and last successful synchronization are visible to administrators.

#### PH2-BR-008 — Overtime management

The system shall support overtime request, approval, calculation input, and reporting.

**Acceptance criteria**
- Overtime rules are configurable by tenant and applicable employee group.
- Overtime can require pre-approval, post-approval, or both according to policy.
- Eligible overtime is derived from approved attendance evidence and policy.
- Overtime is transferred to payroll through a traceable input.
- Overtime cannot be paid twice for the same approved source period.

### Payroll Management

#### PH2-BR-009 — Payroll proration

The system shall calculate prorated salary and eligible components for partial payroll periods.

**Acceptance criteria**
- Proration supports joining, termination, unpaid leave, and approved salary changes.
- The divisor and calendar-day/working-day basis are explicit per policy.
- Each component identifies whether it is prorated.
- Calculation details are available for review.
- Boundary tests cover month length, leap years, and effective dates.

#### PH2-BR-010 — Salary structures and compensation changes

The system shall support structured compensation packages and effective-dated changes.

**Acceptance criteria**
- Salary structures support configurable components, earning/deduction classification, and calculation basis.
- Components can be fixed, percentage-based, or derived from approved inputs where supported.
- Salary changes have effective dates and approval history.
- Historical payroll uses the salary structure effective for the relevant period.
- Access to compensation data is restricted by role and scope.

#### PH2-BR-011 — Retroactive payroll adjustments

The system shall support controlled adjustments for prior periods.

**Acceptance criteria**
- Adjustments reference the original employee, period, and source reason.
- The system calculates or records the delta without rewriting a finalized payroll.
- Adjustments require the configured review and approval.
- Duplicate adjustment submissions are prevented.
- Reports distinguish current-period earnings from prior-period adjustments.

#### PH2-BR-012 — Egypt payroll localization package

The system shall support an approved Egypt payroll policy package for the defined release scope.

**Acceptance criteria**
- Tax and social insurance rules are effective-dated and versioned.
- Employee and employer contributions are represented separately.
- Applicable thresholds, limits, exemptions, and rounding rules are configurable.
- Payroll outputs can be reconciled to approved statutory reporting requirements.
- Every rule has a documented source, effective date, test cases, and specialist sign-off.
- No statutory rule is activated solely on an unverified assumption.

#### PH2-BR-013 — End-of-service and termination settlement

The system shall support termination-related financial calculation inputs and settlement workflow for the approved jurisdiction.

**Acceptance criteria**
- Eligibility and calculation rules are effective-dated and jurisdiction-specific.
- Inputs include relevant service dates, compensation basis, leave balances, and approved deductions.
- Calculation output is reviewable and traceable.
- Settlement requires authorized approval.
- Final values can be handed off to payroll/finance without duplicate payment.

### Recruitment and Benefits

#### PH2-BR-014 — Recruitment workflow

The system shall support a complete recruitment workflow for approved hiring processes.

**Acceptance criteria**
- HR can create vacancies with role, department, location, headcount, and approval status.
- Candidates can be created and linked to one or more vacancies.
- CVs and candidate documents are access-controlled.
- Interview stages, schedules, feedback, and decisions are recorded.
- Candidate status transitions follow configured rules.
- An accepted candidate can be handed off to onboarding without re-entering core data.

#### PH2-BR-015 — Benefits administration

The system shall support configurable employee benefits and enrollment.

**Acceptance criteria**
- Benefits plans define eligibility, enrollment windows, coverage, and employer/employee contribution where applicable.
- Eligibility is evaluated using approved employee attributes.
- Enrollment, change, and cancellation actions are auditable.
- Benefit deductions or contributions can be handed off to payroll.
- Sensitive dependent and beneficiary information has restricted access.

### Employee Experience and Reporting

#### PH2-BR-016 — Employee self-service

The system shall provide employees with self-service access to permitted HR information and requests.

**Acceptance criteria**
- Employees can view their profile, permitted documents, leave balances, attendance summary, payslips, and request history.
- Employees can submit and track supported HR requests.
- Employees cannot view another employee's private information unless explicitly authorized.
- Changes to personal information follow configurable review/approval rules.
- The experience is usable on supported mobile screen sizes.

#### PH2-BR-017 — Mobile application or responsive mobile experience

The system shall provide a supported mobile experience for employee and manager tasks.

**Acceptance criteria**
- The approved delivery approach is documented as native app, cross-platform app, or responsive web/PWA.
- Authentication, session revocation, and device security follow Phase 1 controls.
- Employees can complete priority self-service tasks.
- Managers can process assigned approvals.
- Offline actions, if supported, are queued and reconciled safely rather than silently lost.

#### PH2-BR-018 — Notification delivery management

The system shall deliver and track important HR notifications.

**Acceptance criteria**
- Supported channels are configurable by tenant and event type.
- Delivery attempts and provider outcomes are recorded.
- Retries are bounded and duplicate notifications are controlled.
- Templates support localization and tenant branding where approved.
- Users can manage permitted notification preferences without suppressing mandatory legal or security notices.

#### PH2-BR-019 — HR operational reports

The system shall provide standard reports for core HR operations.

**Acceptance criteria**
- Reports cover headcount, employee movements, leave balances, attendance exceptions, overtime, payroll summaries, and recruitment pipeline.
- Data access follows the same authorization and tenant boundaries as the source modules.
- Reports support agreed filters and export formats.
- Large exports run asynchronously and are audited.
- Scheduled reports are delivered only to authorized recipients.

## 6. Non-Functional Requirements

| Area | Requirement |
|---|---|
| Usability | Common employee and manager tasks should require minimal steps |
| Accessibility | Follow an agreed accessibility baseline |
| Performance | Large attendance and payroll operations must be tested against agreed volume |
| Reliability | Imports, notifications, and payroll handoffs must be retry-safe |
| Privacy | Location, candidate, dependent, and compensation data are purpose-limited |
| Localization | Dates, time zones, currencies, language, and country rules are configurable |
| Auditability | Policy changes and sensitive corrections retain history |
| Compatibility | Supported browsers and mobile platforms are documented |

## 7. Dependencies

- Phase 1 tenant isolation, security, job processing, and payroll integrity.
- Approved attendance, leave, compensation, and payroll policy owners.
- Egypt payroll specialist and legal/accounting review.
- Mobile delivery decision and supported device/browser matrix.
- Biometric device vendors or integration specifications.
- Notification providers and email/SMS credentials.
- UX designs for employee and manager self-service.

## 8. Risks and Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Unclear leave policy | Incorrect balances | Policy workshops and scenario-based acceptance tests |
| Statutory payroll changes | Incorrect deductions | Effective-dated rules and periodic specialist review |
| Device data quality | Attendance disputes | Preserve raw evidence and provide reconciliation tools |
| Location privacy concerns | Employee trust and legal exposure | Explicit policy, data minimization, retention limits |
| Mobile scope expansion | Delivery delay | Prioritize employee and manager journeys |
| Duplicate payroll inputs | Financial overpayment | Source references, idempotency, reconciliation |

## 9. Success Metrics

- Leave balances reconcile with approved policy scenarios.
- Attendance exceptions have traceable resolution and preserve original evidence.
- Payroll proration and adjustment scenarios pass specialist-approved test cases.
- Recruitment candidates can move from vacancy to onboarding without duplicate data entry.
- Employee self-service adoption reaches a target agreed with pilot customers.
- Notification delivery failures and retries are visible to administrators.
- Standard HR reports reconcile with underlying authorized source data.

## 10. Release Gate

Phase 2 is complete only when:
1. Priority leave and attendance journeys pass end-to-end tests.
2. Payroll proration and adjustment scenarios pass approved regression tests.
3. The Egypt localization package has documented specialist sign-off for the enabled scope.
4. Recruitment and benefits workflows meet their agreed minimum release scope.
5. Employee and manager self-service passes authorization and usability testing.
6. Notification and export processing is observable and retry-safe.
7. Pilot customers approve the critical workflows.

## 11. Open Decisions

- Which leave types and accrual models are mandatory for the first release?
- Which attendance devices/vendors are in scope?
- Is mobile delivery native, cross-platform, or responsive web/PWA?
- Which overtime and payroll components are included in the first Egypt package?
- Which statutory forms and bank formats are required?
- Which benefits types are in the initial product scope?
- Which recruitment stages are configurable by tenant?
