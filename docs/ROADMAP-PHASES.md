# Roadmap phases (proposed drafts, not approved)

Status: PROPOSED DRAFTS ONLY. Nothing here is approved, scheduled or implemented, and none of it changes BRD v3 or the current regression scope.

Source: a ChatGPT evaluation conversation shared by the project owner on 2026-11-25: https://chatgpt.com/share/6b06e8ab-1b60-83ea-b893-80f4e267ce25 . The reviewer read only the repository documentation, not `src`, and said so. Its claims need checking against the code before use. One claim checked on 2026-11-25: money is stored as `double` through a global EF Core conversion in `AppDbContext.ConfigureConventions` while calculations use `decimal`.

Owner instruction (WhatsApp voice note, 2026-11-25 15:57 EET): take no action on the evaluation itself, store these drafts in the repo, finish the current BRD v3 work, then apply the architecture step first.

Files in `docs/roadmap-drafts/`:

| File | Content |
|---|---|
| `PHASE-0-architecture-proposal.md` | Architecture-first proposal: modular monolith, shared database with tenant id, outbox, jobs, localization packages. Ends with an unanswered operating-model question (SaaS, dedicated or hybrid). |
| `README.md` | Package overview and phase dependencies as generated. |
| `BRD-Phase-1-Foundation-and-Production-Readiness.md` | Draft Phase 1 BRD. |
| `BRD-Phase-2-HR-Functional-Excellence.md` | Draft Phase 2 BRD. |
| `BRD-Phase-3-Enterprise-and-Intelligent-HR.md` | Draft Phase 3 BRD. |

| `GAP-ANALYSIS.md` | Each of the 50 stories classed Covered, Partial or Gap against the code and TRACEABILITY, with evidence. Read before planning any phase. |
| `USER-STORIES-Phase-1.md` / `-2.md` / `-3.md` | 13 / 19 / 18 stories split from the phase BRDs, each with its source requirement id, the draft's own statement, acceptance criteria and phase-level priority. No actors, estimates or per-story priorities were added. See each file header for the assumptions (the owner's voice note about this was partly garbled). |

Before any phase starts: gap-analyse each requirement against `docs/TRACEABILITY.md` and the code so existing features are not rebuilt, and settle the owner decisions it lists.
