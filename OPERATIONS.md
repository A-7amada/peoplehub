# PeopleHub operations

Fictional portfolio demo, not client work. Do not enter real employee, bank or identity data.

## Storage

- `POSTGRES_CONNECTION`: Npgsql connection string in hosting secrets. Use a pooler endpoint, `SSL Mode=VerifyFull`, `Maximum Pool Size=5`, `Timeout=30`, `Command Timeout=30`. Never commit this value.
- Without it, SQLite uses `DB_PATH` (default peoplehub.db). For durable SQLite use a mounted persistent disk and `PERSISTENT_STORAGE=true`.
- Separate versioned SQLite and PostgreSQL EF migration sets run at startup. Seeding runs only on a new empty database and in a transaction. No automatic database deletion.
- Old pre-migration SQLite files need a deliberate export/baseline migration; do not attach them and expect automatic adoption. The old free-host demo data was ephemeral; this rollout starts from fictional seed data in Neon, not a claimed migration of earlier browser edits.
- PostgreSQL preserves existing wall-clock/UTC field conventions as timestamp without time zone; converters normalize DateTime.Kind. This avoids guessing timezone conversions for Cairo dates. Revisit the mixed semantics before real use.
- Free Render still sleeps. Hosted reminders/email workers do not run while sleeping. Free Neon has quotas and short history, not an SLA.

## Backup and restore

Admin > Settings > Operations provides an authenticated, permission-checked, anti-CSRF POST download. SQLite uses its online backup API; PostgreSQL uses a repeatable-read COPY data archive. Archives include password hashes, TOTP seeds and all records. Store encrypted and privately off-host. Do not attach them to chats or commit them.

The COPY ZIP is a data archive for the exact schema, not a full cluster backup. It does not include database roles, permissions, migration history, data-protection keys, SMTP/API credentials or app configuration. For a full logical database backup prefer current PostgreSQL `pg_dump`/`pg_restore` against the DIRECT endpoint, with credentials in secure environment variables, not command history.

For the app archive, set `POSTGRES_DIRECT` privately. Export:

```sh
dotnet run --project tools/PeopleHub.Ops -- export /secure/path/peoplehub.zip
```

Restore into a NEW EMPTY database with the SAME release:

```sh
dotnet run --project tools/PeopleHub.Ops -- restore-empty /secure/path/peoplehub.zip
```

The tool migrates the target, refuses a nonempty target, restores in FK order, resets identity sequences, and rolls back on failure. The Neon integration test exported and restored to a separate temporary database, checked employee/user counts and refusal to overwrite. Repeat a restore drill before each real cutover; verify more than counts for real data.

SQLite can use `BACKUP_PATH` for a daily rotating seven-file backup while the app is awake. That path must be durable. No scheduled off-host PostgreSQL backup is configured in this demo yet. Manual export and a successful restore drill are not an ongoing disaster recovery service. Never rely only on Neon's short free history.

## Authentication

Reset links are not rendered to requesters. Tokens are hashed, expire in an hour, and a newer request invalidates an older one. Password changes reset the security stamp and invalidate existing cookies. Configured public HTTPS `PUBLIC_BASE_URL` is the only reset URL origin, never the incoming Host header. Account endpoints have a small per-process IP request limit. TOTP re-enrollment cannot silently disable already-enabled 2FA. Development/demo credentials remain intentionally available for fictional portfolio roles.

Data-protection keys can be persisted using `DATA_PROTECTION_PATH` on a durable PRIVATE volume. On free Render no such volume is attached: restarts can still invalidate cookies and pending 2FA tickets. TOTP seeds and mail bodies remain plaintext in the access-controlled DB. No SSO, penetration audit, anti-bot proof, recovery codes or production credential lifecycle is claimed. CSP still permits inline scripts/styles used by this UI.

## Email

Outbox delivery runs every minute while awake. For Brevo HTTPS, set secret `BREVO_API_KEY`, verified `MAIL_FROM`, and comma-separated `MAIL_ALLOWED_RECIPIENTS`. Empty allowlist means nobody receives email. Every fictional `.local` recipient is blocked. Do not add real people without approved recipients and message scope. Reset emails for demo `.local` accounts therefore cannot be delivered.

Alternatively configure SMTP. Free Render blocks SMTP ports 25/465/587, so usual SMTP cannot deliver there. SMTP was not tested with a real provider. API code must be verified with a live authorized test recipient before calling mail operational. Five attempts with exponential delay; a network timeout can still cause duplicates and provider acceptance is not proof of inbox delivery. No exact-once promise. The in-process send gate assumes one web instance.

## Concurrency and security testing

Every tracked entity has an optimistic revision token, so stale saves fail instead of silently overwriting. Award application wraps payroll adjustment, award marking and audit in one DB transaction. Unique constraints enforce one award/appraisal, one feedback invitation/reviewer/appraisal, one line/employee/run, and unique legacy imports. These improve integrity but do not cover every possible race in the entire HR workflow. Payroll arithmetic still uses the previous double conversion and is not accountant-certified.

Run `dotnet test`. With a dedicated disposable PostgreSQL database, set `TEST_POSTGRES` privately for the database smoke/archive/restore test. It creates then drops a separate temporary restore database, never the source. Without this variable that one integration check is bypassed. Unit tests are not load, penetration or compliance tests.
