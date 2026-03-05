import {uuidv4} from './requestHelpers';
import http from "k6/http";
import {EnvironmentConfigData} from "./environmentConfig";
import {check} from "k6";
import {TokenData, refreshToken} from "./requestHelpers";

let paymentRequestTokenData: TokenData = {
    access_token: null,
    refreshed_at: 0
};

export function createPaymentRequest(config: EnvironmentConfigData): string {
    const payload = {
        PaymentDate: "2025-01-01T00:00:00",
        ContactId: uuidv4(),
        BillingContactDetails: {
            Email: "k6@email.com",
        },
        Amount: 100,
        Currency: "AUD",
        PaymentDescription: "description goes here!",
        SelectedPaymentMethod: {
            PaymentGatewayId: uuidv4(),
            PaymentMethodName: "card",
            SurchargeAmount: 10,
        },
        LineItems: [
            {
                Description: "Line item 1",
                Reference: "Some-reference",
            },
        ],
        SourceContext: {
            Identifier: uuidv4(),
            Type: "statementpayments",
            RepeatingTemplateId: uuidv4(),
        },
        Executor: "paymentexecution",
        Receivables: [
            {
                Identifier: uuidv4(),
                Type: "invoice",
            },
            {
                Identifier: uuidv4(),
                Type: "invoice",
            },
        ],
        MerchantReference: "available upon request",
    };
    paymentRequestTokenData = refreshToken(paymentRequestTokenData, config.tokenUrl, config.paymentRequestClientConfig);
    const response = http.post(`${config.paymentRequestUrl}/v1/payment-requests/create`, JSON.stringify(payload), {
        headers: {
            'Authorization': `Bearer ${paymentRequestTokenData.access_token}`,
            "Xero-Client-Name": "Payment-execution-load-test-script",
            "Xero-Correlation-Id": uuidv4(),
            "Xero-Tenant-Id": uuidv4(),
            'Content-Type': 'application/json'
        }
    });

    check(response, {
        "Payment Request created successfully": (r) => r.status === 201,
    });
    return response.json('paymentRequestId') as string;
}

export function preparePaymentRequest(requestId: string, config: EnvironmentConfigData) {
    paymentRequestTokenData = refreshToken(paymentRequestTokenData, config.tokenUrl, config.paymentRequestClientConfig);
    const response = http.post(`${config.paymentRequestUrl}/v1/payment-requests/${requestId}/prepare`, null, {
        headers: {
            'Authorization': `Bearer ${paymentRequestTokenData.access_token}`,
            "Xero-Client-Name": "Payment-execution-load-test-script",
            "Xero-Correlation-Id": uuidv4(),
            "Xero-Tenant-Id": uuidv4(),
            'Content-Type': 'application/json'
        }
    });

    check(response, {
        "Payment Request prepared successfully": (r) => r.status === 204,
    });
}

