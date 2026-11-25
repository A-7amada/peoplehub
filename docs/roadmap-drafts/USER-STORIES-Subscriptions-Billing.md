> **PROPOSED DRAFT, NOT APPROVED. DOCS ONLY. Nothing is built.** Stories for `BRD-Phase-1-Subscriptions-Billing-Addendum.md`. Written by the builder agent; not from Ahmed's evaluation drafts. Priority, estimates and owners are not set. Gap analysis against the code is pending (expected result: all Gap, since no tenant, plan or billing concept exists).

# User stories: Subscriptions and billing

Each story keeps the requirement's "The system shall" wording. Story `US-SUB-NNN` maps to `SUB-BR-NNN`.

| Story | Requirement | Acceptance criteria (proposed) |
|---|---|---|
| US-SUB-001 Plan catalogue | SUB-BR-001 | Plans have price, period, currency, entitlements. Versioned; existing subscribers keep their plan version. |
| US-SUB-002 Entitlement enforcement | SUB-BR-002 | Module and employee-cap limits enforced per tenant. Over-limit gives a clear message, not data loss. |
| US-SUB-003 Subscription lifecycle | SUB-BR-003 | Trial, active, past due, suspended, cancelled, reactivated. Each transition audited with actor and reason. |
| US-SUB-004 Usage metering | SUB-BR-004 | Monthly active-employee count per tenant stored as an immutable record that can be reviewed before invoicing. |
| US-SUB-005 Invoicing | SUB-BR-005 | Sequential numbers, country tax lines, PDF output, credit notes for corrections. Issued invoices are never edited. |
| US-SUB-006 Payment collection | SUB-BR-006 | At least one pluggable provider interface and a manual bank-transfer reconciliation flow. Idempotent webhooks. |
| US-SUB-007 Plan change | SUB-BR-007 | Upgrade and downgrade with a documented proration rule and a preview before confirming. |
| US-SUB-008 Platform admin console | SUB-BR-008 | Manage tenants, plans, discounts, overrides. All actions audited; platform role separate from tenant roles. |
| US-SUB-009 Suspension and exit | SUB-BR-009 | Read-only export window after suspension or cancellation; deletion follows retention policy only. |
| US-SUB-010 Billing isolation | SUB-BR-010 | Tests prove one tenant cannot read another's invoices or usage. |
