import {uuidv4} from './requestHelpers';
import http from "k6/http";
import {EnvironmentConfigData} from "./environmentConfig";
import {check} from "k6";
import {randomItem} from "./requestHelpers";

import {TokenData, refreshToken} from "./requestHelpers";

let paymentExecutionTokenData: TokenData = {
    access_token: null,
    refreshed_at: 0
};

export function submitPaymentRequest(requestId: string, config: EnvironmentConfigData): [string, number] {
    const payload = {
        paymentRequestId: requestId,
        paymentMethodsMadeAvailable: ["card"],
    }
    paymentExecutionTokenData = refreshToken(paymentExecutionTokenData, config.tokenUrl, config.paymentExecutionClientConfig);
    const response = http.post(`${config.paymentExecutionUrl}/v1/payments/stripe/submit`, JSON.stringify(payload), {
        headers: {
            'Authorization': `Bearer ${paymentExecutionTokenData.access_token}`,
            "Xero-Client-Name": "Payment-execution-load-test-script",
            "Xero-Correlation-Id": uuidv4(),
            "Xero-Tenant-Id": uuidv4(),
            "provider-account-id": randomItem(config.providerAccountIds),
            'Content-Type': 'application/json'
        }
    });
    check(response, {
        "Payment Request submitted successfully": (r) => r.status === 200,
    });
    const paymentIntentId = response.json('paymentIntentId') as string;
    return [paymentIntentId, response.timings.duration];
}
