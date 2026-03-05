CREATE TABLE payment_execution.PaymentTransaction (
    paymentTransactionId uuid NOT NULL,
    paymentRequestId uuid NOT NULL,
    paymentServiceId uuid NOT NULL,
    status varchar(20) NOT NULL,
    
    fee numeric(12,2),
    feeCurrency varchar(3),
    providerTransactionReference varchar(20),
    providerType varchar(20),
    
    createdUTC timestamp NOT NULL,
    updatedUTC timestamp NOT NULL,
    PRIMARY KEY (paymentTransactionId)
);
