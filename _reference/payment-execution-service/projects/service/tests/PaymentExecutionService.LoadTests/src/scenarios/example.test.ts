import {check} from "k6";
import http from "k6/http";

import {getEnvironmentConfig} from "../helpers/requestHelpers";
import {createPaymentRequest, preparePaymentRequest} from "../helpers/PaymentRequestHelpers";
import {submitPaymentRequest} from "../helpers/PaymentExecutionHelpers";
import {Trend} from "k6/metrics";

const config = getEnvironmentConfig();
const submitPaymentRequestTrend = new Trend('submit_payment_request_duration');

export const performPing = () => {
    const response = http.get(`${config.paymentExecutionUrl}/ping`);
    check(response, {
        "HTTP is status 200": (r) => r.status === 200,
    });
};

export const performSubmit = () => {
    const requestId = createPaymentRequest(config);
    preparePaymentRequest(requestId, config);
    const [paymentIntentId, duration] = submitPaymentRequest(requestId, config);
    submitPaymentRequestTrend.add(duration);
    console.log(`${requestId}, ${paymentIntentId}`);
};
