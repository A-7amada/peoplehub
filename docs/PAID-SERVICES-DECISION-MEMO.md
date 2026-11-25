> **PROPOSED DRAFT, NOT APPROVED. DOCS ONLY.** Written 2026-11-25 for Ahmed, who asked which services the deferred rows would need. Nothing here is bought or built. Prices are list prices read on 2026-11-25 from the vendor pages named in section 5, in USD per month; EGP uses 1 USD = 54.7 EGP (CBE market average 54.63 buy / 54.73 sell on 2026-11-25). Prices change and usage-based items are examples, not quotes.

# Paid services decision memo

Current rule (Ahmed, 2026-11-25): no paid infrastructure for now. This memo lists, per deferred or blocked row, the cheapest viable option, what it unlocks, and whether it is really paid.

## 1. Two different databases, do not mix them up
- **Verification PostgreSQL** (to test races, migrations, restore): can be free. A throw-away Postgres container in GitHub Actions, or a Neon free branch. Cost $0. It unlocks LEV-009 verification and the restore drill. No decision needed beyond "yes, build it".
- **Production PostgreSQL** (live customer data): the current free Neon already serves the demo. It has a 6-hour history, 1 GB cap, no SLA and no point-in-time recovery beyond that. Paid Neon Launch has no minimum fee; it bills compute at $0.106 per compute-unit-hour and storage at $0.35 per GB-month, with up to 7 days of history. Always-on at 0.25 CU is about 182 CU-hours, so about $19 plus storage, roughly **$20/month (EGP 1,094)**. With scale-to-zero it costs less. This is only needed for real customers, not for the demo.

## 2. Rows, options and costs
| Row | What it needs | Cheapest viable option | USD/month | EGP/month | What it unlocks |
|---|---|---|---|---|---|
| ARC-004 durable scheduler | An always-awake process. Free Render sleeps, so reminders, effective-dated changes and email workers stop. | Render Starter web service ($7). Free workaround: a free external pinger or a GitHub Actions cron that wakes the app, which is not durable and not guaranteed. | 7 | 383 | Reminders, document-expiry alerts, scheduled status changes, SLA escalation (WFL-001), EMP-004 remainder |
| ARC-005 multi-instance and cache | At least 2 app instances plus a shared cache | 2 x Starter ($14) plus Render Key Value $6 | 20 | 1,094 | Availability and scale. Not useful before real load. |
| ARC-006 private object storage | Durable file storage that survives restarts | Cloudflare R2: 10 GB-month free, then $0.015/GB. It may need a card on file even for the free tier (to verify). Alternative: Render disk $0.25/GB, but only on a paid instance. | 0 to 1 | 0 to 55 | Documents, certificates and evidence stored durably (today they live with the app database) |
| SEC-004 field encryption at rest | A key held outside the database | Cheapest: an application key kept in a Render environment variable. It costs $0 but custody is on Ahmed, and losing it loses the data. Managed: AWS KMS $1 per key plus $0.03 per 10,000 requests. | 0 (env var) or about 1 | 0 or 55 | Encrypted salary, bank, TOTP seeds. Needs his decision on key custody and rotation first. |
| SEC-008 malware scanning | A scanner with about 1 GB RAM | ClamAV in the container needs a bigger instance than Starter: Render 1 CPU / 2 GB at $25. Public scanning APIs would upload confidential HR files, so they are not suitable. | 25 | 1,368 | Scanning of every uploaded document |
| ENT-004 SSO | An identity provider | OIDC with Microsoft Entra or Google is free for basic sign-in. Costs appear only with enterprise features (SCIM, conditional access). | 0 | 0 | Company sign-in. Mostly engineering, not a paid service. |
| ENT-005 mobile / GPS attendance | A phone client | Installable web app (PWA) with browser geolocation: free. A native store app costs Apple $99 per year (about $8.25/month) and Google $25 once. | 0, or 8.25 | 0, or 451 | Phone check-in. See ATT-006. |
| ATT-006 remote attendance / geofencing | Browser location and a site list | Free with a PWA. Not strictly paid; I deferred it because it depends on the mobile client and a privacy decision. | 0 | 0 | Remote punches with a geofence |
| ENT-001 multiple legal entities | Tenancy design only | No service. Deferred because it depends on the tenancy architecture (Phase 0). | 0 | 0 | Several companies, shared employees |
| ENT-003 bank, accounting, government adapters | Vendor contracts and credentials | Cannot price. It depends on which bank, accounting package and tax authority integration, and on the vendor's fees. | quote needed | quote needed | Payroll bank files and journals posting, e-invoicing |
| ENT-006 analytics / AI insights | A model API and a governance policy | Usage-based; cannot price before the model and volume are chosen. Needs an AI-governance decision. | quote needed | quote needed | Workforce planning and insights |
| LEV-009 race verification | A verification PostgreSQL | Free (section 1) | 0 | 0 | Verified concurrency behaviour |
| PAY-010 bank reconciliation | Bank result files | No service for manual evidence (built). Bank file import depends on the bank's format. | quote needed | quote needed | Automatic bank-result matching |
| PAY-011 accounting journals | The accounting package's API and chart of accounts | Depends on the package and plan. Cannot price. | quote needed | quote needed | Posted journals |

## 3. Cheapest combination that changes the most
- **$0:** verification Postgres in CI, R2 free tier, env-var key for field encryption, a PWA, Entra/Google OIDC. Each of these still needs engineering plus some of Ahmed's decisions, and R2 may ask for a card on file.
- **$7/month (EGP 383):** Render Starter, which makes the scheduler, reminders and SLA escalation real. This is the single most useful paid item.
- **$32/month (EGP 1,750):** Starter plus ClamAV-sized instance. Note a single instance of the larger size would cover both jobs, so $25 alone may be enough (to verify).
- **About $20/month more (EGP 1,094):** paid Neon only when the first real customer arrives.

## 4. What stays free regardless
The off-host backup (GitHub private repo and Actions) runs at $0 (SEC-009 slice, approved 2026-11-25).

## 5. Sources (read 2026-11-25)
- Render pricing: https://render.com/pricing (Hobby $0 plus compute; Starter 0.5 CPU / 512 MB $7; 1 CPU / 2 GB $25; disks $0.25/GB)
- Neon plans: https://neon.com/docs/introduction/plans (Launch no minimum; $0.106/CU-hour; $0.35/GB-month; 7-day history)
- Cloudflare R2: https://developers.cloudflare.com/r2/pricing/ (10 GB-month free, $0.015/GB-month)
- AWS KMS: https://aws.amazon.com/kms/pricing/ ($1 per key per month)
- CBE exchange rates: https://www.cbe.org.eg/ar/economic-research/statistics/exchange-rates
- Not verified: Apple and Google store fees, Entra/Google OIDC pricing, whether R2 needs a card (from general knowledge, to check before any decision).
