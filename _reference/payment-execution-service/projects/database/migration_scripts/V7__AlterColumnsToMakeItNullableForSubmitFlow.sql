ALTER TABLE payment_execution.PaymentTransaction
    ALTER COLUMN providerServiceId DROP NOT NULL;

ALTER TABLE payment_execution.PaymentTransaction 
    ALTER COLUMN PaymentProviderPaymentTransactionId SET DEFAULT NULL;


