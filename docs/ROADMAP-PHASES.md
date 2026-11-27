# Roadmap phases (proposed drafts, not approved)

Status: PROPOSED DRAFTS ONLY. Nothing here is approved, scheduled or implemented, and none of it changes BRD v3 or the current regression scope.

Source: a ChatGPT evaluation conversation shared by the project owner on 2026-11-25: https://chatgpt.com/share/6b06e8ab-1b60-83ea-b893-80f4e267ce25 . The reviewer read only the repository documentation, not `src`, and said so. Its claims need checking against the code before use. One claim checked on 2026-11-25: money is stored as `double` through a global EF Core conversion in `AppDbContext.ConfigureConventions` while calculations use `decimal`.

Owner instruction (WhatsApp voice note, 2026-11-25 15:57 EET): take no action on the evaluation itself, store these drafts in the repo, finish the current BRD v3 work, then apply the architecture step first.

Files in `docs/roadmap-drafts/`:

| File | Content |
|---|---|
| `PHASE-0-architecture-proposal.md` | Architecture-first proposal: modular monolith, shared database with tenant id, outbox, jobs, localization packages. Its operating-model question is resolved: Ahmed confirmed SaaS multi-tenant with subscriptions on 2026-11-25 (shared database + TenantId first, database-per-tenant later for large customers). Subscriptions/billing is a proposed addendum, see `roadmap-drafts/`. |
| `README.md` | Package overview and phase dependencies as generated. |
| `BRD-Phase-1-Foundation-and-Production-Readiness.md` | Draft Phase 1 BRD. |
| `BRD-Phase-2-HR-Functional-Excellence.md` | Draft Phase 2 BRD. |
| `BRD-Phase-3-Enterprise-and-Intelligent-HR.md` | Draft Phase 3 BRD. |

| `GAP-ANALYSIS.md` | Each of the 50 stories classed Covered, Partial or Gap against the code and TRACEABILITY, with evidence. Read before planning any phase. |
| `USER-STORIES-Phase-1.md` / `-2.md` / `-3.md` | 13 / 19 / 18 stories split from the phase BRDs, each with its source requirement id, the draft's own statement, acceptance criteria and phase-level priority. No actors, estimates or per-story priorities were added. See each file header for the assumptions (the owner's voice note about this was partly garbled). |

Before any phase starts: gap-analyse each requirement against `docs/TRACEABILITY.md` and the code so existing features are not rebuilt, and settle the owner decisions it lists.

## Progress by phase (updated 2026-11-26)

| Phase | State | Built so far | Still open |
|---|---|---|---|
| 0 Architecture | In progress, decisions recorded as ADR-0001 to ADR-0021 | Module and transaction guard tests, tenant context, internal scheduler, files in DB, field encryption (ADR-0021), API conventions (ADR-0020, convention only) | Full tenant isolation build, REST/mobile API endpoints, OIDC provider |
| 1 Foundation | Partial (7 of 13 stories partial, 6 gap) | SoD at HR review and Finance approve, lockout and rate limit config, optional TOTP MFA, field encryption, metrics endpoint, correlation ids | Tenancy and subscriptions, durable jobs hosting, off-host backups and restore drill, release governance |
| 2 HR functionality | Partial (decision-free items being built) | Employee lifecycle items, mobile-responsive pass, employment letters, headcount and turnover report, outbox delivery states (US-PH2-018) | Leave accrual and expiry, hourly leave, GPS capture, Egypt payroll validation, category grace |
| 3 Enterprise | Mostly open | Executive analytics started (headcount, turnover, payroll cost trend) | Integrations, SSO, banking, AI features, country packs |

The per-story evidence is in [GAP-ANALYSIS.md](roadmap-drafts/GAP-ANALYSIS.md) and the per-requirement register in [TRACEABILITY.md](TRACEABILITY.md). Items that need an owner decision or spend are listed there and are not started.

Update 2026-11-27: two owner decisions recorded as ADR-0022 (leave policy defaults, built configurable) and ADR-0023 (staged full tenant isolation build). Both are decided and not yet built. Everything else on the held list stays deferred.
Leave policy unit 1 (editable policy, tiers, preview) is built. Unit 2 (built): an enabled policy with annual accrual sets the opening entitlement of a new year balance in whole days (tier, waiting period, first-year proration); policies are off by default so existing balances are unchanged. Still to build: monthly accrual, carry-over expiry, half-day and hourly requests (they need decimal balances).
