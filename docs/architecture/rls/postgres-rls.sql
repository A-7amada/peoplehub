-- ADR-0023 stage 5: PostgreSQL row-level security as a second line of defence.
-- STATUS: WRITTEN, NOT APPLIED, NOT VERIFIED. Do not run on production.
-- It needs (1) a run on a Neon branch, and (2) app-side wiring that sets app.tenant_id on each connection
-- (SELECT set_config('app.tenant_id', '<id>', false)), which is not built yet. Applying it without both would hide every row.
-- The application role must not own the tables and must not be a superuser (owners bypass RLS unless FORCE is set; FORCE is used below).
DO $$
DECLARE r record;
BEGIN
  FOR r IN SELECT table_schema, table_name FROM information_schema.columns
           WHERE column_name = 'TenantId' AND table_schema = 'public' AND table_name NOT IN ('Tenant','PlatformUser','PlatformAuditLog','ScheduledJobState')
  LOOP
    EXECUTE format('ALTER TABLE %I.%I ENABLE ROW LEVEL SECURITY', r.table_schema, r.table_name);
    EXECUTE format('ALTER TABLE %I.%I FORCE ROW LEVEL SECURITY', r.table_schema, r.table_name);
    EXECUTE format('DROP POLICY IF EXISTS tenant_isolation ON %I.%I', r.table_schema, r.table_name);
    EXECUTE format('CREATE POLICY tenant_isolation ON %I.%I USING ("TenantId" = NULLIF(current_setting(''app.tenant_id'', true), '''')::int) WITH CHECK ("TenantId" = NULLIF(current_setting(''app.tenant_id'', true), '''')::int)', r.table_schema, r.table_name);
  END LOOP;
END $$;
-- Rollback: for each table, ALTER TABLE ... NO FORCE ROW LEVEL SECURITY; ALTER TABLE ... DISABLE ROW LEVEL SECURITY; DROP POLICY tenant_isolation.
