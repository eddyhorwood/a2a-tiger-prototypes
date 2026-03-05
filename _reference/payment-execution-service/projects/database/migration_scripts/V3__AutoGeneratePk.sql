ALTER TABLE payment_execution.PaymentTransaction
    ALTER COLUMN paymentTransactionId SET DEFAULT gen_random_uuid();