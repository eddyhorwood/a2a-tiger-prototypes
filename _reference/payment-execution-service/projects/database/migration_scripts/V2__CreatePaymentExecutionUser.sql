CREATE USER payment_execution_user WITH PASSWORD 'temp_p@ssw0rd';

GRANT USAGE ON SCHEMA payment_execution TO payment_execution_user;

GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA payment_execution TO payment_execution_user;

