# Staging (ADR-0019)

Free, manual deploy only. Same repo, same code path as production.

## State (2026-11-26)
- Neon branch `staging` exists in project `peoplehub-demo` (branch id `br-tiny-mud-bci7x05e`, parent `production`, data and schema copied, auto-delete: Never). It uses the shared 100 CU-hours; the compute scales to zero when idle.
- Render service `peoplehub-staging`: **not created yet.** It needs the staging branch connection string as a secret (`POSTGRES_CONNECTION`), which must not pass through chat or logs.

## Steps to finish
1. Neon console, branch `staging`, Connect: copy the connection string for the app role.
2. Render, New, Web Service, public repo `A-7amada/peoplehub`, branch `main`, runtime Docker (same as production), instance type Free, name `peoplehub-staging`, auto-deploy Off.
3. Environment variables, same as production except: `POSTGRES_CONNECTION` = the staging branch string; `HR_SEED_PASSWORD` = a different value than production; leave `METRICS_TOKEN` and email settings unset.
4. Manual deploy. Check `/health/ready`, sign in as the HR admin.
5. Release flow: push to main, wait for CI green, deploy staging first (migrations run there), then production.

## Cost
All free tier. If Render or Neon asks for a card or an upgrade at any step, stop: that is a spend decision.
