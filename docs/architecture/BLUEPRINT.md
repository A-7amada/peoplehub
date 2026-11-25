# PeopleHub Architecture Blueprint (Phase 0)

Source of the target: the architecture discussion Ahmed shared (https://chatgpt.com/share/6b06e8ab-1b60-83ea-b893-80f4e267ce25, last architecture answer, "Phase 0: Architecture Evolution & Technical Foundation"). Decisions are recorded as ADRs in `adr/`, accepted by Ahmed on 2026-11-25 22:50.
Status labels: **Built**, **Partial**, **Planned**. Nothing here is claimed as built unless the register (docs/TRACEABILITY.md) or the code shows it.

## 1. Style
Modular monolith, Clean Architecture, .NET 8, EF Core, PostgreSQL (Neon Free) with SQLite for local and tests. No microservices. Free-tier deviations that stay unless Ahmed lifts them: internal scheduler instead of Hangfire (ADR-0012), files in the database (ADR-0014), no Redis (ADR-0013), no paid services.

## 2. Layers (dependency direction: Web -> Infrastructure -> Application -> Domain)
| Layer | Holds | Rule |
|---|---|---|
| Domain | Entities, value objects, domain events, domain rules, interfaces | Depends on nothing |
| Application | Use cases, command and query handlers, module contracts, validation, authorization checks | Depends on Domain only |
| Infrastructure | EF Core, migrations, tenancy filter, outbox, file store, cache, jobs, integration adapters | Implements Application interfaces |
| Web | MVC controllers, Razor views, device API, workers host | Thin: no business rules |

## 3. Target folder layout inside each project (decision: organize inside projects, not only by module name)
```
src/PeopleHub.Domain/
  Common/            Entity base, ValueObjects, DomainEvent, DomainException
  Modules/<Module>/  Entities, Events, Rules      (Employees, Leave, Attendance, Payroll, Talent, Benefits, Grievances, Relations, Organization, Requests)
  Platform/          Tenant, Subscription, Audit, Notification, Settings
src/PeopleHub.Application/
  Common/            ICurrentUser, ITenantContext, ICache, IFileStore, Clock, Result types
  Modules/<Module>/  Contracts (public interfaces + DTOs), Commands, Queries, Services, EventHandlers
  Platform/          Tenancy, Scheduling, Outbox contracts, Localization packs
  Localization/      Global rules, CountryPacks/Egypt, policy resolution
src/PeopleHub.Infrastructure/
  Persistence/       AppDbContext, Configurations/<Module>, Migrations/Sqlite|Postgres, Seed
  Tenancy/           Query filter, tenant stamping, resolver
  Outbox/ Files/ Caching/ Jobs/ Integrations/ Operations/
src/PeopleHub.Web/
  Modules/<Module>/  Controller + Views for that module
  Api/               versioned /api/v1 and device API
  Hosting/           Program setup, workers, middleware
tests/PeopleHub.Tests/
  Architecture/ Tenancy/ Modules/<Module>/ Postgres/
```
Namespaces follow folders. Files move module by module in small commits; every move keeps the full gate green. Current state: **Planned**. Today the code is flat per layer (Domain/Entities, Application/Services, Web/Controllers).

## 4. Design patterns, named where they are used
- Repository and Unit of Work: AppDbContext behind IRepository (exists, Built). `AtomicAsync` is the unit of work boundary.
- Transactional Outbox: IntegrationEvent and email outbox (Built, Partial for domain events).
- Strategy: country calculation contracts, ICache, IFileStore, identity providers (Planned).
- Decorator: tenant-keyed cache (Planned).
- Domain Event and Observer: in-aggregate events turned into outbox rows (Planned).
- Specification / global query filter: tenant filter (Planned).
- Facade: one public contract per module (Planned).
- CQRS (selective): Payroll and Reports only (Planned).
- Template Method: scheduled job wrapper (Built as JobTracker, Partial).
- Ambient context: ITenantContext (Planned).
Each class that implements a pattern carries a one-line comment naming it.

## 5. Multi-tenancy (ADR-0001 to 0004)
Shared database, TenantId on customer data, EF global query filter, tenant stamped on save, ITenantContext per request and per job, platform tenant separate from customer tenants, support access by audited time-limited grant. Resolution from the session claim now, subdomain later. Existing demo data is assigned to a default tenant by the migration. Status: **Built** 2026-11-26 (TenantFoundation migration): shadow TenantId column on 98 customer tables, global query filter, stamping and cross-tenant write guard, Tenant table (Id 1 = default demo tenant), natural keys unique per tenant, logins still unique by email. Not yet: tenant claim issued at login (resolver defaults to tenant 1), second-tenant onboarding, PostgreSQL row-level security.

## 6. Data
PostgreSQL via EF Core. Money moves from double storage to decimal(18,2) (ADR-0015), after a backup and a verified restore, before the tenancy migration. Migrations are generated for both providers on every change; SQLite upgrade is exercised and Postgres SQL is inspected. Status: decimal(18,2) **Built** on PostgreSQL (migration DecimalMoney, 2026-11-25; quantities such as hours and multiplier are numeric(18,4); SQLite keeps REAL for local and tests through the provider Strategy). Rehearsed on a restored copy of production: 36 columns, sums identical. Migrations process **Built**.

## 7. Financial rules (target section E)
Decimal precision (Planned), effective-dated policies (Partial), immutable payroll snapshots (Partial: finalized runs are fixed), calculation traceability (Partial), adjustment and reversal ledger (Partial), approval separation (Built), reconciliation (Partial).

## 8. Security (ADR-0016)
RBAC with permissions, field grants, separation of duties, append-only audit, step-up, session management, lockout (Built). Tenant isolation in audit and permissions, support access log (Planned). Field encryption deferred (needs key management). Key custody for backups: printed copy with a second trusted person, restore drill before any real client, then quarterly.

## 9. Background processing (ADR-0012)
Internal scheduler with persisted run state and a 10-minute database lease (Built). Per-tenant execution and backoff retry (Planned). Runs only while the host is awake; a business-hours keep-awake ping exists (Sun-Thu 08:00-18:00 Cairo).

## 10. Integration architecture (target section D)
Adapter per external system (devices, banks, accounting, email/SMS, identity, government) with outbox, idempotency and retry. Outbox with idempotent enqueue, backoff and dead-letter after 5 attempts is Built. Adapters per system are Planned. Live email is off for the demo (provider mail does not reach the agent mailbox).

## 11. Observability and testing (ADR-0017, 0018)
Health live and ready, in-process metrics, correlation ids, Diagnostics page (Built). Structured logs with tenant id and an OpenTelemetry-format metrics endpoint (Planned). 590 tests on SQLite. Architecture tests, tenant-isolation tests and Postgres tests in GitHub Actions (Planned).

## 12. Environments and deployment (ADR-0019)
Production: Render Free, manual deploy, Neon Free. Staging (Planned): second free Render service and a Neon branch. Backups: daily encrypted pg_dump from a private repository.

## 13. Implementation order (decision 21)
1. ADRs and this Blueprint (**Done**).
2. Restore drill (**Done**, weekly workflow in the private backups repo) then decimal(18,2) (**Done**, deploy pending verification).
3. Postgres tests in GitHub Actions (**Done**, CI workflow).
4. Interfaces: ITenantContext, ICache, IFileStore (**Done**; DB-backed IFileStore with the file migration is **Planned**).
5. TenantId, backfill, query filter, isolation tests (**Done**).
6. Module boundaries and domain events, with the folder reorganization above.
7. Egypt Pack and policy separation.
8. API conventions and OIDC preparation.
9. Observability and staging.
Each step: focused tests, full gate with every test accounted for once, EN/AR role-navigation renders and pixels for UI changes, SQLite upgrade and Postgres SQL inspection for schema changes, manual deploy, checkpoint to Main.

## 14. Needs approval before any spend
ARC-005 multi-instance with shared cache (about 20 USD per month), custom domain for subdomains, paid identity provider, labour lawyer and payroll accountant review, managed key service, Render Starter at the first paying client.
