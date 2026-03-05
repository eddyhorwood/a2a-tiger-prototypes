-- Revoke all privileges before dropping the user
DO $$
BEGIN
    IF EXISTS (SELECT FROM pg_catalog.pg_user WHERE usename = 'retool_readonly') THEN
        -- Revoke default privileges first (these create dependencies)
        ALTER DEFAULT PRIVILEGES FOR ROLE payment_execution_schema_manager IN SCHEMA payment_execution REVOKE SELECT ON TABLES FROM retool_readonly;
        ALTER DEFAULT PRIVILEGES FOR ROLE payment_execution_schema_manager IN SCHEMA payment_execution REVOKE USAGE ON SEQUENCES FROM retool_readonly;

        -- Revoke privileges on information schema
        REVOKE SELECT ON information_schema.tables FROM retool_readonly;
        REVOKE SELECT ON information_schema.columns FROM retool_readonly;
        

        -- Revoke usage on sequences
        REVOKE USAGE ON ALL SEQUENCES IN SCHEMA payment_execution FROM retool_readonly;

        -- Revoke SELECT on all tables
        REVOKE SELECT ON ALL TABLES IN SCHEMA payment_execution FROM retool_readonly;

        -- Revoke usage on the schema
        REVOKE USAGE ON SCHEMA payment_execution FROM retool_readonly;
    END IF;
END
$$;

-- Drop the user
DROP USER IF EXISTS retool_readonly;


