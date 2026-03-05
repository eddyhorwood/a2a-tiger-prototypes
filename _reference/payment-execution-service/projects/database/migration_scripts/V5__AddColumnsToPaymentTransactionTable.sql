ALTER TABLE payment_execution.PaymentTransaction
    ADD COLUMN failureDetails varchar(125);

ALTER TABLE payment_execution.PaymentTransaction
    ADD COLUMN paymentCompletionDateTimeUtc timestamp;
