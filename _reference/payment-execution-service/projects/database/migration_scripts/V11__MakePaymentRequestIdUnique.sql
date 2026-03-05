ALTER TABLE payment_execution.paymenttransaction
    ADD CONSTRAINT uk_paymenttransaction_paymentrequestid UNIQUE (paymentRequestId);