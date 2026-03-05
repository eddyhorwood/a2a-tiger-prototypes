-- This script creates a readonly user for Retool to query the database
-- The user has SELECT permissions on all tables in the payment_execution schema

-- Create user only if it doesn't already exist
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_catalog.pg_user WHERE usename = 'retool_readonly') THEN
        CREATE USER retool_readonly WITH
            LOGIN
            PASSWORD 'temp_p@ssw0rd'
            NOSUPERUSER
            INHERIT
            NOCREATEDB
            NOCREATEROLE
            NOREPLICATION;
    END IF;
END
$$;

-- Grant usage on the schema
GRANT USAGE ON SCHEMA payment_execution TO retool_readonly;

-- Grant SELECT on all existing tables
GRANT SELECT ON ALL TABLES IN SCHEMA payment_execution TO retool_readonly;

-- Grant SELECT on all future tables (important for new tables added later)
-- When FOR ROLE is omitted, it applies to objects created by the current user (schema_manager)
ALTER DEFAULT PRIVILEGES IN SCHEMA payment_execution GRANT SELECT ON TABLES TO retool_readonly;

-- Grant usage on sequences (needed for pagination queries)
GRANT USAGE ON ALL SEQUENCES IN SCHEMA payment_execution TO retool_readonly;
ALTER DEFAULT PRIVILEGES IN SCHEMA payment_execution GRANT USAGE ON SEQUENCES TO retool_readonly;



