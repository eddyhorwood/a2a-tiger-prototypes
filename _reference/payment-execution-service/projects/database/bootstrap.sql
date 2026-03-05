
CREATE DATABASE payment_execution_db;
\connect payment_execution_db;

CREATE SCHEMA payment_execution;

CREATE USER payment_execution_schema_manager WITH PASSWORD 'local_p@ssword';

GRANT USAGE ON SCHEMA payment_execution TO payment_execution_schema_manager;
GRANT CREATE ON SCHEMA payment_execution TO payment_execution_schema_manager;

ALTER USER payment_execution_schema_manager WITH CREATEROLE;
ALTER SCHEMA payment_execution OWNER TO payment_execution_schema_manager;