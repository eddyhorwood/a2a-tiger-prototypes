do $$
DECLARE
    numOfPaymentExecutionUser integer;
BEGIN
    SELECT COUNT(*) INTO numOfPaymentExecutionUser FROM pg_roles WHERE rolname = 'payment_execution_user';
    IF numOfPaymentExecutionUser > 0 THEN
        REVOKE SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA payment_execution FROM payment_execution_user;
        REVOKE USAGE ON SCHEMA payment_execution FROM payment_execution_user;
        DROP USER payment_execution_user;
    END IF;
END;
$$;
