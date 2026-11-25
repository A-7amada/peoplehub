> **PROPOSED DRAFT, NOT APPROVED. DOCS ONLY. Nothing is built.** Direction proposed by Ahmed by WhatsApp voice note on 2026-11-25 (16:21 EET) and agreed in the conversation. The wording of this addendum was written by the builder agent. Every Egypt pack value is an open item that needs a legal/payroll specialist, not a decision.

# Addendum: Country Packs (labor-law configuration) in every phase

## 1. Direction
Egyptian, Saudi and UAE labor laws each have their own articles. Each phase should state the country configuration its features run on, instead of hardcoding one country's rules.

## 2. Concept
- A **country pack** is a versioned, effective-dated configuration set for one country. A **tenant has exactly one active country pack** (alongside the tenant concept, see `PHASE-0-architecture-proposal.md`).
- A pack defines:
  - labor-law rules
  - leave entitlements
  - working hours per week
  - overtime rules
  - end-of-service benefit formula
  - social insurance and tax parameters
  - official holidays
  - payroll calendar
- Modules read the active pack. They do not embed Egyptian (or any) rules in shared logic.
- Pack changes are versioned, effective-dated, audited and independently tested. Past payroll runs keep the pack version they used.

## 3. Scope guard
- Build the pack layer and fill it with **Egyptian labor law only**.
- KSA and UAE packs are authored at expansion time, not now.
- This extends US-PH3-018 (country policy packages) and PH3-BR-018's rule that a tenant may use only an approved country package. The layer itself is Phase 0 architecture (PH0-004); Phase 1 and 2 stories consume it.

## 4. Requirements
| ID | Requirement |
|---|---|
| CP-BR-001 | The system shall define a country pack schema covering the eight parameter groups in section 2. |
| CP-BR-002 | Each tenant shall have one active country pack, with effective dates and version history. |
| CP-BR-003 | Leave, attendance, overtime, payroll, end-of-service and calendar features shall read pack values and not hardcode them. |
| CP-BR-004 | Pack values shall be changed only through an audited, versioned release with specialist sign-off. |
| CP-BR-005 | The Egypt pack shall be the only pack delivered initially; its values shall each cite their legal source and sign-off status. |

## 5. Current state (from the repo)
Egyptian rules sit in payroll code and settings (policy `EG-DEMO-2026-v1`, illustrative, not legally validated). There is no pack interface (see `GAP-ANALYSIS.md`, US-PH2-012 and US-PH3-018).

## 6. Egypt pack values each phase depends on (to specify per story)
Each phase BRD carries a note that its stories must list the Egypt pack values they use. Starting point:
- Phase 1: working hours/week, official holidays, payroll calendar, tax and social insurance parameters used by payroll integrity, data retention minimums.
- Phase 2: leave entitlements, overtime rules, end-of-service formula, deduction priority, payroll and statutory outputs, HR letter wording requirements.
- Phase 3: any additional country policy, effective-dated rule changes, new-country onboarding checklist.

## 7. Open owner decisions
- Who is the legal/payroll specialist who signs off the Egypt pack.
- Which values are configurable per tenant, and which are fixed by law.
- Whether a tenant may override a pack value (for example more generous leave) and how that is recorded.
- Which country follows Egypt.
