> **PROPOSED DRAFT, NOT APPROVED.** Generated in a ChatGPT evaluation conversation (https://chatgpt.com/share/6b06e8ab-1b60-83ea-b893-80f4e267ce25, 2026-11-25). It is not part of BRD v3, has not been reviewed against the repository, and is not in scope for current work. Per Ahmed Hamada's WhatsApp voice note of 2026-11-25 15:57 EET: take no action on the evaluation itself, keep these drafts in the repo, and apply the architecture step first once the current BRD v3 work is finished.

# PeopleHub Product Evolution — BRD Package

## Purpose
This package divides the proposed PeopleHub improvements into three independently manageable product phases. Each phase has its own Business Requirements Document (BRD), scope, requirements, acceptance criteria, dependencies, risks, and release gates.

## Phase structure

| Phase | BRD | Business outcome |
|---|---|---|
| 1 | `BRD-Phase-1-Foundation-and-Production-Readiness.md` | Make PeopleHub secure, tenant-aware, financially reliable, and operationally supportable |
| 2 | `BRD-Phase-2-HR-Functional-Excellence.md` | Close core HR functional gaps and improve employee and HR operations |
| 3 | `BRD-Phase-3-Enterprise-and-Intelligent-HR.md` | Add enterprise integrations, advanced analytics, and carefully governed AI capabilities |

## Planning principles
1. Phase 1 is a prerequisite for a commercially reliable multi-company product.
2. Phase 2 prioritizes frequently used HR workflows before adding differentiating features.
3. Phase 3 builds on stable, governed data and operational capabilities from the earlier phases.
4. Requirements marked **Baseline validation** must be checked against the current implementation before being treated as new development.
5. Payroll rules must be validated by qualified local payroll and legal specialists before production use.
6. The proposed scope does not mandate microservices. A modular monolith remains the default architectural direction unless measured operational needs justify a change.

## Requirement status
The BRDs describe target business requirements, not claims that every capability is currently missing. During discovery, classify each requirement as:
- Existing and accepted
- Existing but needs correction
- Partially implemented
- Not implemented
- Not applicable to the target release

## Suggested delivery governance
Each phase should have:
- A confirmed scope baseline
- Prioritized user stories linked to requirement IDs
- UX and API/data design where applicable
- Security and privacy review
- Test evidence mapped to acceptance criteria
- Release notes, operational runbook, and rollback plan
- Business sign-off

## Important assumptions to confirm
- Initial target market: Egypt, with later localization for other countries.
- Initial customer segment: small and mid-sized organizations, with a possible growth path to larger enterprises.
- **Confirmed decision (Ahmed, 2026-11-25, WhatsApp 16:19):** PeopleHub's target operating model is SaaS multi-tenant with subscriptions. The current single-tenant build stays as the demo and can be deployed dedicated for an early customer who insists on their own environment. Multi-tenancy is therefore a product requirement, not an assumption.
- Hosting strategy (follows the review chat recommendation): shared database with `TenantId` first, with the option of database-per-tenant later for large customers.
- Subscriptions, billing and pricing are added as a proposed workstream (`BRD-Phase-1-Subscriptions-Billing-Addendum.md`, `USER-STORIES-Subscriptions-Billing.md`). Draft only, nothing is built.
- Existing PeopleHub modules and documentation will be assessed before implementation to avoid duplicating completed work.
- The exact legal, tax, social insurance, banking, and data-retention requirements must be confirmed for each target country.

- Country packs (proposed direction by Ahmed, 2026-11-25): per-tenant country configuration for labor-law rules, consumed by every phase. Egypt only at first. See `COUNTRY-PACKS-Addendum.md`. Draft, not approved.
