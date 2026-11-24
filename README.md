# PeopleHub

A bilingual (English / Arabic, with RTL) HR management system **demo project** built with ASP.NET Core 8 MVC.
It is a sample app with **fictional data**, made to demonstrate architecture and product thinking for a portfolio.
It is **not client work** and not a production HR system. The repository owner presents it as a demo, not as a delivered commercial product.

The feature set follows a business requirements document (BRD) for an Arabic HRMS, applied as far as is practical for a demo, in stage order: Core HR, Attendance, Leave, Payroll, Employee Portal, Reporting.

## BRD coverage

| Stage | Covered in this demo | Not covered |
|---|---|---|
| 1. Core HR | Employee records (personal, work, bank, insurance), department tree with managers and cost centers, branches, job titles with grades and salary ranges, status changes with history, contracts (fixed term / permanent, renewal, expiry alerts), career events, audit log of sensitive changes | File attachments (documents are referenced by name only), bulk import |
| 2. Attendance | Check in / out, manual entry, shifts with grace period, lateness, early leave, overtime threshold, absence rules, excused absence, daily and monthly views, attendance correction requests | Fingerprint device and mobile GPS integration (the source is only recorded), shift rosters and rotations |
| 3. Leave | Leave types, balances with carry-over, request and approval route (manager, plus HR for more than 2 days or unpaid), backdating rules, public holidays, overlap checks, cancelling approved leave, HR balance adjustments, who-is-away view | Approval delegation, accrual by tenure rules, half-day leave |
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

- Business rules live in the domain layer and are unit tested (46 tests: attendance calculation, leave rules, approval routing, configurable cycles and role-driven permissions, advances, contracts, status changes, payroll calculation and workflow, password hashing).
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

A `Dockerfile` is included (used for the Render free tier). Set `HR_SEED_PASSWORD` for the HR account. The SQLite database is recreated and re-seeded on every start, so changes made in the demo do not persist.

## Limits

- Demo project with fictional data. Tax and insurance rates are illustrative, not legal advice or Egyptian law.
- No file uploads, no email or SMS, no delegation of approvals (out-of-office), no external integrations.

## Documentation

[BRD.md](BRD.md) is a business requirements document (Arabic) describing the whole system, its rules and its flows.
