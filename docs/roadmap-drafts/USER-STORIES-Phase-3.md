> **PROPOSED DRAFT, NOT APPROVED. DOCS ONLY.** Stories derived mechanically from `BRD-Phase-3-Enterprise-and-Intelligent-HR.md` (itself an unapproved draft from the ChatGPT evaluation conversation, https://chatgpt.com/share/6b06e8ab-1b60-83ea-b893-80f4e267ce25). Not in scope for current BRD v3 work.
>
> **Assumptions to confirm:** (1) Ahmed's WhatsApp voice note of 2026-11-25 15:58 EET was partly garbled; it was read as "split the three phase BRDs into stories and keep them in the repo until current work is finished" and as meaning stories with their acceptance criteria. (2) No story actors were invented: each story keeps the draft's own "The system shall..." statement, its acceptance criteria and the phase-level priority (Strategic); the draft carries no per-requirement priority, estimate or owner. (3) Story and requirement numbering follow the draft: story `US-PH3-NNN` maps to `PH3-BR-NNN`. (4) Before any phase starts, gap-analyse each story against `docs/TRACEABILITY.md` and the code, since many describe features that already exist in part.


# User stories: Phase 3 (Strategic)

Source BRD: `BRD-Phase-3-Enterprise-and-Intelligent-HR.md`

## US-PH3-001: Integration catalog and administration

- Source requirement: PH3-BR-001
- Priority: Strategic (phase-level)
- Status: proposed, not started

**Story:** The system shall provide a governed way to configure and monitor supported external integrations.

**Acceptance criteria**
- Each integration has an owner, purpose, supported version, authentication method, and operational status.
- Credentials are stored in an approved secrets store and are never exposed in logs or UI.
- Administrators can enable, disable, and rotate integration credentials according to permissions.
- Integration configuration is tenant-scoped unless explicitly designated as platform-level.
- Changes are audited.

## US-PH3-002: Integration execution and reconciliation

- Source requirement: PH3-BR-002
- Priority: Strategic (phase-level)
- Status: proposed, not started

**Story:** The system shall provide reliable integration execution and traceability.

**Acceptance criteria**
- Each integration execution has a correlation identifier and external reference where available.
- Retries are bounded and idempotent.
- Permanent failures are visible and can be reconciled by authorized users.
- Payload mapping and schema versions are documented.
- Sensitive payload data is minimized and protected.
- Integration health, latency, failure rate, and last successful synchronization are observable.

## US-PH3-003: Enterprise identity integration

- Source requirement: PH3-BR-003
- Priority: Strategic (phase-level)
- Status: proposed, not started

**Story:** The system shall support enterprise identity federation for approved customers.

**Acceptance criteria**
- Supported protocols and providers are documented, such as OpenID Connect and SAML where required.
- Tenant-to-identity-provider mapping is explicit.
- Provisioning and deprovisioning follow approved rules.
- Role and group mapping is controlled and auditable.
- Break-glass access follows a documented secure process.

## US-PH3-004: Finance and accounting integration

- Source requirement: PH3-BR-004
- Priority: Strategic (phase-level)
- Status: proposed, not started

**Story:** The system shall transfer approved HR and payroll financial data to supported accounting systems.

**Acceptance criteria**
- Mapping supports accounts, cost centres, legal entities, departments, and periods as required.
- Exported records have unique references and reconciliation status.
- Failed transfers can be retried without duplicate postings.
- Payroll totals reconcile between PeopleHub and the receiving system.
- Integration does not alter finalized payroll records.

## US-PH3-005: Banking and payment integration

- Source requirement: PH3-BR-005
- Priority: Strategic (phase-level)
- Status: proposed, not started

**Story:** The system shall support controlled bank-file or API-based payment handoff where supported by the target market.

**Acceptance criteria**
- Supported banks, formats, and payment methods are explicitly listed.
- Payment files and responses are access-controlled and audited.
- Payment status can be reconciled against payroll disbursement records.
- Duplicate payment submission is prevented or detected.
- Bank credentials and signing keys are protected through approved key management.
- The system clearly distinguishes payroll approval, payment submission, and confirmed settlement.

## US-PH3-006: Communication and device integrations

- Source requirement: PH3-BR-006
- Priority: Strategic (phase-level)
- Status: proposed, not started

**Story:** The system shall support reusable adapters for approved email, SMS, collaboration, and attendance device providers.

**Acceptance criteria**
- Provider configuration is tenant-aware where required.
- Provider failures are visible and retry-safe.
- Provider-specific logic is isolated behind adapters.
- Provider replacement does not require changes to HR business rules.

### Advanced Workflow Governance

## US-PH3-007: Workflow service levels and escalation

- Source requirement: PH3-BR-007
- Priority: Strategic (phase-level)
- Status: proposed, not started

**Story:** The system shall support service-level targets and escalation policies for workflow tasks.

**Acceptance criteria**
- SLA calendars account for tenant working days, holidays, and time zones.
- Escalation thresholds and recipients are configurable.
- Escalation does not silently approve or reject a business request.
- SLA pauses and exclusions are explicit and auditable.
- Users can report overdue tasks and SLA compliance.

## US-PH3-008: Advanced delegation and approval governance

- Source requirement: PH3-BR-008
- Priority: Strategic (phase-level)
- Status: proposed, not started

**Story:** The system shall support governed delegation and dynamic approval routing.

**Acceptance criteria**
- Delegation has a defined scope, effective period, and optional approval.
- Delegated users cannot exceed the original permission boundary.
- Dynamic routing can resolve approved organizational attributes such as manager, department, or cost centre.
- Conflicts of interest and self-approval restrictions are enforced.
- Delegation and routing decisions are auditable.

## US-PH3-009: Workflow simulation and analytics

- Source requirement: PH3-BR-009
- Priority: Strategic (phase-level)
- Status: proposed, not started

**Story:** The system shall allow administrators to validate workflow definitions before publication and analyze execution outcomes.

**Acceptance criteria**
- Administrators can simulate representative paths using sample inputs.
- Validation identifies unreachable stages, missing approvers, and invalid configuration.
- Published workflow versions remain immutable for in-flight instances.
- Analytics report cycle time, pending age, rejection reasons, and SLA performance.
- Workflow analytics respect tenant and data access permissions.

### Advanced Analytics and Workforce Planning

## US-PH3-010: Executive HR analytics

- Source requirement: PH3-BR-010
- Priority: Strategic (phase-level)
- Status: proposed, not started

**Story:** The system shall provide role-appropriate dashboards for workforce and HR operational performance.

**Acceptance criteria**
- Dashboards include approved metrics such as headcount, movement, absence, overtime, turnover, payroll cost, and recruitment pipeline.
- Metric definitions, filters, and calculation periods are documented.
- Data refresh time and source coverage are visible.
- Drill-down follows row-level and tenant-level permissions.
- Users can export only data they are authorized to access.

## US-PH3-011: Workforce cost and headcount planning

- Source requirement: PH3-BR-011
- Priority: Strategic (phase-level)
- Status: proposed, not started

**Story:** The system shall support scenario-based workforce planning.

**Acceptance criteria**
- Authorized users can create draft planning scenarios without changing actual employee or payroll records.
- Scenarios support headcount, role, department, cost centre, compensation assumptions, and effective periods.
- Scenario assumptions and versions are retained.
- Planned and actual values can be compared.
- Approved plans can be exported or handed off to finance planning processes.

## US-PH3-012: Skills and competency intelligence

- Source requirement: PH3-BR-012
- Priority: Strategic (phase-level)
- Status: proposed, not started

**Story:** The system shall support organization-wide skills and competency visibility.

**Acceptance criteria**
- Skills and competency taxonomies are configurable.
- Employee skills can be sourced from approved profiles, assessments, and training records.
- Access to sensitive assessment data is restricted.
- Managers can view authorized team skill coverage.
- Skill-gap reports identify missing capabilities without making automated employment decisions.

## US-PH3-013: Succession and talent planning

- Source requirement: PH3-BR-013
- Priority: Strategic (phase-level)
- Status: proposed, not started

**Story:** The system shall support controlled succession and talent-pipeline planning.

**Acceptance criteria**
- Critical roles and succession plans can be defined by authorized users.
- Potential successors and readiness indicators are access-restricted.
- Data sources and assessment dates are visible.
- Changes are audited.
- The system presents planning information as decision support, not a guaranteed prediction of employee performance.

### AI-Assisted HR

## US-PH3-014: AI-assisted HR knowledge and policy search

- Source requirement: PH3-BR-014
- Priority: Strategic (phase-level)
- Status: proposed, not started

**Story:** The system shall allow authorized users to ask questions over approved HR policies and documentation.

**Acceptance criteria**
- Responses are grounded in tenant-authorized source documents.
- The system provides source references for generated answers.
- Users cannot retrieve documents outside their access scope.
- Unsupported answers are clearly identified rather than fabricated.
- Administrators can manage indexed sources and remove outdated material.
- Sensitive employee records are excluded unless a separately approved use case permits them.

## US-PH3-015: Payroll and attendance anomaly detection

- Source requirement: PH3-BR-015
- Priority: Strategic (phase-level)
- Status: proposed, not started

**Story:** The system shall identify unusual patterns for human investigation.

**Acceptance criteria**
- Initial use cases include duplicate-like payroll inputs, unusual pay variances, abnormal overtime, and attendance anomalies.
- Each alert explains the signals that caused it.
- Thresholds and model versions are recorded.
- Authorized users can review, dismiss, or confirm alerts with an audit trail.
- An alert does not automatically change payroll or employee records.
- False-positive rates are measured during pilot operation.

## US-PH3-016: Workforce forecasting

- Source requirement: PH3-BR-016
- Priority: Strategic (phase-level)
- Status: proposed, not started

**Story:** The system shall provide forecasts for selected workforce metrics where sufficient data quality exists.

**Acceptance criteria**
- Forecasts show the data period, assumptions, and uncertainty range where applicable.
- Forecasts are not presented as certainty.
- Data quality limitations are disclosed.
- Users can compare forecast values with actual outcomes over time.
- Forecasts are used for planning support, not automatic employment decisions.

## US-PH3-017: AI governance and human oversight

- Source requirement: PH3-BR-017
- Priority: Strategic (phase-level)
- Status: proposed, not started

**Story:** The system shall govern AI capabilities according to approved privacy, security, and employment-risk controls.

**Acceptance criteria**
- Each AI use case has an owner, purpose, risk classification, approved data sources, and retention policy.
- AI access respects tenant, role, and record-level authorization.
- Sensitive attributes are not used for employment recommendations unless explicitly approved and legally reviewed.
- Consequential decisions remain subject to qualified human review.
- Prompts, outputs, model/provider versions, and feedback are logged only to the extent approved by privacy policy.
- Administrators can disable an AI capability independently.
- AI providers and data-processing locations are reviewed before activation.

### Country Localization

## US-PH3-018: Country policy packages

- Source requirement: PH3-BR-018
- Priority: Strategic (phase-level)
- Status: proposed, not started

**Story:** The system shall support additional country-specific HR and payroll policy packages without embedding country rules throughout shared business logic.

**Acceptance criteria**
- Each country package defines currency, calendars, statutory rules, required employee data, payroll outputs, and effective dates.
- Shared payroll engine behavior is separated from jurisdiction-specific policy.
- Country packages are versioned and independently tested.
- Activation requires local specialist and business-owner sign-off.
- A tenant can use only an approved country package for its legal entity.
