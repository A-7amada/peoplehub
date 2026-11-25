> **PROPOSED DRAFT, NOT APPROVED. DOCS ONLY. Nothing is built.** Written by the builder agent on 2026-11-25 after Ahmed confirmed the SaaS multi-tenant model. Not part of the ChatGPT evaluation drafts. Every price, limit, tax and provider below is an open question for the owner, not a decision.

# Addendum: Subscriptions, Billing and Pricing

## 1. Purpose
Selling PeopleHub as SaaS needs a commercial layer the current demo lacks: plans, subscriptions, invoicing, trials and usage limits, per tenant. Starting market: Egyptian SMEs, then KSA and UAE.

## 2. Scope (proposed)
- Plan catalogue and entitlements (modules, employee cap, features).
- Tenant subscription lifecycle: trial, active, past due, suspended, cancelled, reactivated.
- Metering: billable unit is likely active employees per month (to confirm).
- Invoicing and payment collection, with tax-compliant invoices per country.
- Platform-administrator tools, and tenant-admin self-service for plan and invoices.
- Dunning and suspension that never deletes tenant data without a retention rule.

## 3. Market considerations (to verify, not assumed)
- Egypt: local payment methods and card gateways, EGP pricing, e-invoicing requirements of the Egyptian Tax Authority, VAT. Many SMEs prefer bank transfer, cash or wallets over recurring card billing.
- KSA: SAR, ZATCA e-invoicing, VAT. UAE: AED, VAT, e-invoicing rollout.
- Multi-currency pricing and FX policy, and whether prices include tax.
- Data residency may affect hosting per country.

## 4. Requirements
| ID | Requirement |
|---|---|
| SUB-BR-001 | The system shall define plans with price, billing period, currency and entitlements. |
| SUB-BR-002 | The system shall enforce entitlements per tenant (module access, employee cap) without code changes per customer. |
| SUB-BR-003 | The system shall manage a subscription lifecycle with trial, grace and suspension states and an audit trail. |
| SUB-BR-004 | The system shall meter billable usage per tenant per period with a reviewable, immutable usage record. |
| SUB-BR-005 | The system shall issue invoices per tenant with country tax rules, sequential numbering and credit notes for corrections. |
| SUB-BR-006 | The system shall support payment collection through pluggable providers, including offline methods (bank transfer) with manual reconciliation. |
| SUB-BR-007 | The system shall support plan change, with a defined proration rule. |
| SUB-BR-008 | The platform administrator shall manage tenants, plans, discounts and manual overrides, all audited. |
| SUB-BR-009 | Suspended or cancelled tenants shall keep a read-only export window and follow the retention policy. |
| SUB-BR-010 | Billing data shall be tenant-isolated and never exposed to other tenants. |

## 5. Dependencies
Tenancy design (PH1-001 to 004, Phase 0), retention policy, payment provider and tax decisions, legal review of terms and invoicing.

## 6. Open owner decisions
- Pricing model: per employee, per module, tiered, flat. Price points and currencies.
- Trial length and limits; free tier or not.
- Payment providers per country; whether offline payment is allowed at launch.
- Invoice tax and e-invoicing approach per country.
- Annual vs monthly, discounts, refund policy.
- Who operates billing: in-house or an external billing service.
