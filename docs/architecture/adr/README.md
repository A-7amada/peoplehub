# Architecture Decision Records

Status of every record below: **Accepted** by Ahmed on 2026-11-25 22:50 (WhatsApp quote-reply on the Phase 0 proposal, all 21 decisions). Numbers match the decision numbers in the proposal. Decision 20 is this folder; decision 21 is the order in BLUEPRINT.md.

| ADR | Title |
|---|---|
| [0001](ADR-0001.md) | Tenant data isolation: shared database with TenantId |
| [0002](ADR-0002.md) | Tenant resolution |
| [0003](ADR-0003.md) | Tenant context in jobs, cache, files, settings |
| [0004](ADR-0004.md) | Platform tenant versus customer tenant |
| [0005](ADR-0005.md) | Module boundaries and contracts |
| [0006](ADR-0006.md) | Data ownership per module |
| [0007](ADR-0007.md) | Country and policy separation |
| [0008](ADR-0008.md) | REST and mobile API |
| [0009](ADR-0009.md) | OIDC versus current authentication |
| [0010](ADR-0010.md) | CQRS, selectively |
| [0011](ADR-0011.md) | Domain events and outbox |
| [0012](ADR-0012.md) | Background jobs: internal scheduler, not Hangfire |
| [0013](ADR-0013.md) | Cache |
| [0014](ADR-0014.md) | File storage |
| [0015](ADR-0015.md) | decimal(18,2) migration timing |
| [0016](ADR-0016.md) | Security architecture |
| [0017](ADR-0017.md) | Testing architecture |
| [0018](ADR-0018.md) | Observability |
| [0019](ADR-0019.md) | Staging |
| [0020](ADR-0020.md) | API conventions and OIDC preparation (convention only, no endpoints yet) |
| [0021](ADR-0021.md) | Sensitive field encryption (free, app level) |
| [0022](ADR-0022.md) | Leave policy defaults, built configurable (decided 2026-11-27, not built) |
| [0023](ADR-0023.md) | Staged full tenant isolation build (decided 2026-11-27, stage 0 exists) |
