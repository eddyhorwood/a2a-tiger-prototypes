ALTER TABLE payment_execution.PaymentTransaction
    RENAME COLUMN providerTransactionReference TO PaymentProviderPaymentReferenceId;

ALTER TABLE payment_execution.PaymentTransaction 
    ALTER COLUMN PaymentProviderPaymentReferenceId TYPE varchar(75);

ALTER TABLE payment_execution.PaymentTransaction
    ADD COLUMN PaymentProviderPaymentTransactionId varchar(75);

ALTER TABLE payment_execution.PaymentTransaction
    ADD COLUMN EventCreatedDateTimeUtc timestamptz;
