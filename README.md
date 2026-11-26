# PeopleHub

A bilingual (English / Arabic, with RTL) HR management system **demo project** built with ASP.NET Core 8 MVC.
It is a sample app with **fictional data**, made to demonstrate architecture and product thinking for a portfolio.
It is **not client work** and not a production HR system. The repository owner presents it as a demo, not as a delivered commercial product.

The feature set follows a business requirements document (BRD) for an Arabic HRMS, applied as far as is practical for a demo, in stage order: Core HR, Attendance, Leave, Payroll, Employee Portal, Reporting.

## Current project status (updated 2026-11-26)

Demo project, fictional data. Live demo: https://peoplehub-ozfw.onrender.com/ (Render free tier, Neon PostgreSQL free tier, manual deploys). The site banner stays.

Built since the BRD v3 baseline (details and evidence in [docs/TRACEABILITY.md](docs/TRACEABILITY.md) and [docs/roadmap-drafts/GAP-ANALYSIS.md](docs/roadmap-drafts/GAP-ANALYSIS.md)):

- **Phase 0, architecture:** modular monolith with module-boundary and transaction-boundary guard tests, tenant context, ADRs 0001 to 0021 ([docs/architecture/adr](docs/architecture/adr/README.md), [Blueprint](docs/architecture/BLUEPRINT.md)).
- **Security:** separation of duties (the preparer is blocked at HR review and at Finance approve), configurable lockout and rate limit, optional TOTP MFA with recovery codes (off by default), field-level grants, and free app-level AES-256-GCM encryption of national id, IBAN, payroll IBAN snapshot and TOTP seeds (key from the `FIELD_ENCRYPTION_KEY` environment variable; custody limits in OPERATIONS.md).
- **HR operations:** employment confirmation letters (employee request, a different HR user issues or declines, text frozen at issue, printable EN/AR), headcount and turnover report, six-month payroll cost trend (finance grant), email outbox delivery states with requeue (live email still off), 244 EN/AR role-navigation renders as a regression check.
- **Free-tier choices that stay:** internal scheduler, files stored in the database, no paid services, live email off.

Not done, and held for owner decisions or spend: tenant isolation and subscriptions, OIDC/SSO, leave accrual and carry-forward policies, mobile GPS capture, Egypt payroll validation by a labour lawyer and payroll accountant (deferred), durable job hosting, staging (held), and paid services. Not verified: Postgres races and load, a real ZKTeco device, live email, real mobile devices, RLS on Neon.

Test status: the full suite runs by class partitions, each test counted once (673 tests in 96 classes on the last full run; 2 are skipped because they need a Postgres server). CI runs on every push.

## BRD coverage

| Stage | Covered in this demo | Not covered |
|---|---|---|
| 1. Core HR | Employee records (personal, work, bank, insurance), department tree with managers and cost centers, branches, job titles with grades and salary ranges, status changes with history, contracts (fixed term / permanent, renewal, expiry alerts), career events, audit log of sensitive changes | File attachments (documents are referenced by name only), bulk import |
| 2. Attendance | Check in / out, manual entry, shifts with grace period, lateness, early leave, overtime threshold, absence rules, excused absence, daily and monthly views, attendance correction requests | Fingerprint device and mobile GPS integration (the source is only recorded), shift rosters and rotations |
| 3. Leave | Leave types, balances with carry-over, request and approval route (manager, plus HR for more than 2 days or unpaid), backdating rules, public holidays, overlap checks, cancelling approved leave, HR balance adjustments, who-is-away view | Accrual by tenure, accrual by tenure rules, half-day leave |
| 4. Payroll | Salary components, monthly runs built from attendance, approved overtime and advances, advances with installments, bonus and deduction adjustments with a mandatory reason, staged approval (draft, HR review, finance, final close, paid), locked closed runs, reopen as logged admin exception, payslips with print view, bank transfer CSV | Real Egyptian social insurance and tax tables (rates here are **illustrative**), end-of-service settlement, accounting export |
| 5. Employee portal | Own profile and contact update, check in / out, leave requests and balances, overtime / correction / advance requests, payslips, notifications | Mobile app |
| 6. Reporting | Dashboard KPIs and charts, employee, attendance, leave balance, payroll cost and leavers reports with CSV export | Custom report builder, scheduled reports |
| 7. Integrations (BRD phase 7) | **Not implemented** (government, bank, biometric, accounting systems) | all |
| 8. Advanced HR (BRD phase 8) | **Not implemented** (recruitment, performance, training, AI features) | all |

In-app notifications exist. Email and SMS are not sent.

## Roles and demo accounts

Nine built-in roles are seeded (admins can add more): system admin, HR manager, HR specialist, attendance officer, payroll officer, finance manager, department manager, top management, employee. Data visibility is scoped: employees see themselves, department managers their team, company roles see everyone. Salary data is limited to HR manager, payroll, finance and the employee.

| Account | Role |
|---|---|
| `employee@peoplehub.local` | Employee |
| `manager@peoplehub.local` | Department manager |
| `hrspecialist@peoplehub.local` | HR specialist |
| `attendance@peoplehub.local` | Attendance officer |
| `payroll@peoplehub.local` | Payroll officer |
| `finance@peoplehub.local` | Finance manager |
| `ceo@peoplehub.local` | Top management |
| `hr@peoplehub.local` | System admin + HR manager |

All accounts except `hr@` use the password `Demo123!` (fictional data only). The `hr@` password comes from the `HR_SEED_PASSWORD` environment variable; in Development it is `Admin123!`. Without the variable the account stays locked.

Try this flow: sign in as the employee and request leave, as the manager approve it, as HR give the second approval. Then as the payroll officer create a run, as HR review it, as finance approve it, as top management finalize it, and as payroll mark it paid and download the bank file.

## Administration: roles, permissions and approval cycles

Nothing about access or approvals is hardcoded to a person.

- **Roles and permissions** (`Admin > Roles & permissions`): a matrix of 24 permissions (pages and actions) by role. Tick a box and it applies immediately, no restart. Admins can create custom roles. The system admin role always keeps every permission. Built-in roles start with sensible defaults.
- **Users** (`Admin > Users`): create accounts, assign one or more roles, deactivate. You cannot remove your own admin access.
- **Approval cycles** (`Admin > Approval cycles`): for each request kind (leave, overtime, attendance correction, salary advance) define the ordered steps. Each step is either the employee's direct manager or one or more roles, with an optional threshold (for example, HR approves leave only above 2 days). Steps can be reordered, added and removed. New requests follow the new cycle at once; requests already in flight keep their route.
- Rules kept by the engine: the direct-manager step is skipped when the employee has none, nobody approves their own request, the system admin can act on any step, and an empty route falls back to the first role step.
- Every change is written to the audit log.

## Architecture

```
src/
  PeopleHub.Domain          entities, business rules (attendance, leave, approvals, payroll), repository port, password hasher
  PeopleHub.Application     use-case services with role and data-scope checks
  PeopleHub.Infrastructure  EF Core + SQLite, seed data
  PeopleHub.Web             MVC controllers, Razor views, localization, CSS (no frontend build step, no external fonts or scripts)
tests/
  PeopleHub.Tests           xUnit tests for the domain rules
```

- Business rules live in the domain layer and are unit tested (unit and integration tests, 673 in total at the last full run: attendance calculation, leave rules, approval routing, configurable cycles and role-driven permissions, advances, contracts, status changes, payroll calculation and workflow, password hashing).
- Cookie authentication, PBKDF2 password hashing, antiforgery on every POST, permission checks in the application layer (role-to-permission map loaded from the database).
- English and Arabic UI with a language switch and a right-to-left layout built with CSS logical properties.
- The Egyptian weekend (Friday and Saturday) and public holidays are excluded from working days.

## Design

The UI uses patterns common in modern SaaS products such as Linear, Stripe Dashboard and Gusto: a dark sidebar with grouped navigation, KPI cards, status chips, avatar initials, inline SVG charts, tabbed profiles and a step tracker for approval paths. Icons are inline SVG. This is inspiration for layout and interaction only; no assets were copied.

## Run locally

```bash
dotnet run --project src/PeopleHub.Web      # Development: hr@peoplehub.local / Admin123!
dotnet test
```

## Deploy

A `Dockerfile` is included (used for the Render free tier). Set `HR_SEED_PASSWORD` for the HR account. The host now runs EF migrations without deleting data. Set POSTGRES_CONNECTION for external PostgreSQL persistence; local SQLite still needs a persistent disk. See OPERATIONS.md.

## Limits

- Demo project with fictional data. Payroll uses the 2026 Egyptian insurance (11%, floor 2,700, cap 16,700), income tax brackets, 20,000 personal exemption and 0.05% martyrs fund from public sources. It has not been reviewed by a specialist and is not legal advice.
- Attendance can come from the system or a ZKTeco device (Admin > Settings). A cloud server cannot reach a private LAN IP, so a real machine needs a bridge posting to /api/device/punches; the demo uses a simulator. Not tested against a physical device.
- Email goes to an outbox and is sent only if SMTP is configured. 2FA is TOTP. No SSO, SMS, mobile app or GPS attendance.
- Uploaded attachments (2 MB, PDF/JPG/PNG) are stored in the demo database, which persists only when durable storage is configured. PDF export uses the browser print dialog. No external integrations. See BRD.md sections 4B and 4C for an honest benchmark against ZenHR and Odoo.

## Documentation

Index: [BRD v3](docs/BRD-v3.md), [roadmap phases](docs/ROADMAP-PHASES.md), [gap analysis](docs/roadmap-drafts/GAP-ANALYSIS.md), [traceability](docs/TRACEABILITY.md), [architecture blueprint](docs/architecture/BLUEPRINT.md), [ADR index](docs/architecture/adr/README.md), [operations](OPERATIONS.md), [regression](REGRESSION.md).

[BRD.md](BRD.md) is a business requirements document (Arabic) describing the whole system, its rules and its flows.

## Performance management (round 5)

`/Perf` has cycles (Open -> Calibration -> Closed), weighted goals, progress, self and manager ratings, HR calibration with an audited reason, and development actions. Scores use 70% weighted goals + 30% manager overall (or overall only without goals). Promotion/bonus advice is advisory, never an automatic payroll change. Only the employee, direct manager and Talent managers can view appraisals; employees see manager scores after close. Dates are descriptive, phase changes are manual. No 360, competency templates, automatic KPI feeds or automatic training/payroll integration. See BRD section 4D.

## Performance follow-up (round 6)

Round 6 supersedes the earlier fixed/manual-only limits: per-cycle goals/manager/360 weights, optional Cairo date-driven phases, daily deduplicated reminders, named 360 invitations, competency templates, linked training enrollments/completion, explicit performance award handoff to draft payroll and idempotent import of old reviews. A 15-minute hosted worker runs only while the free service is awake, with catch-up on access. 360 is confidential but not anonymous: managers/HR see names, employees see only the published average. Payroll awards are never automatic payments and follow the normal payroll approval chain. See BRD section 4E for remaining limits.

## Hardening (round 7)

PostgreSQL/SQLite migrations, optimistic concurrency, transactional performance awards, hashed reset tokens without UI disclosure, cookie invalidation, account rate limiting, automatic allowlisted mail delivery, and consistent archive export with tested empty-database restore. See [OPERATIONS.md](OPERATIONS.md). Free hosting still sleeps; provider setup and scheduled off-host backups are separate operational requirements.

## Business baseline and review roadmap

[BRD v3](docs/BRD-v3.md) separates the business baseline from historical implementation notes. [Feature traceability](docs/TRACEABILITY.md) records shipped, partial and open recommendations. Enterprise targets and legal payroll validation are not claimed.
