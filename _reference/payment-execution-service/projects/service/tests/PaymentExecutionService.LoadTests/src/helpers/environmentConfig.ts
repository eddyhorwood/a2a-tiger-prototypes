export type ClientConfig = {
    clientId: string;
    clientSecret: string;
    scope: string;
}

export type EnvironmentConfigData = {
    environmentName: string;
    paymentRequestClientConfig: ClientConfig
    paymentExecutionClientConfig: ClientConfig
    tokenUrl: string;
    paymentRequestUrl: string;
    paymentExecutionUrl: string;
    providerAccountIds: string[];
}

export const development: EnvironmentConfigData = {
    environmentName: "development",
    paymentRequestClientConfig: {
        clientId: typeof __ENV.PR_IDENTITY_CLIENT_ID !== 'undefined'
            ? __ENV.PR_IDENTITY_CLIENT_ID
            : 'local_caller',
        clientSecret: typeof __ENV.PR_IDENTITY_CLIENT_SECRET !== 'undefined'
            ? __ENV.PR_IDENTITY_CLIENT_SECRET
            : 'secret',
        scope: 'xero_collecting-payments_payment-request-service.create xero_collecting-payments_payment-request-service.prepare',
    },
    paymentExecutionClientConfig: {
        clientId: typeof __ENV.PE_IDENTITY_CLIENT_ID !== 'undefined'
            ? __ENV.PE_IDENTITY_CLIENT_ID
            : 'local_caller',
        clientSecret: typeof __ENV.PE_IDENTITY_CLIENT_SECRET !== 'undefined'
            ? __ENV.PE_IDENTITY_CLIENT_SECRET
            : 'secret',
        scope: 'xero_collecting-payments-execution_payment-execution-service.submit'
    },
    tokenUrl: "http://host.docker.internal:5003/connect/token",
    paymentRequestUrl: "http://host.docker.internal:5000",
    paymentExecutionUrl: "http://host.docker.internal:5000",
    providerAccountIds: ["acct_12345678", "acct_87654321"]
};

export const test: EnvironmentConfigData = {
    environmentName: "test",
    paymentRequestClientConfig: {
        clientId: typeof __ENV.PR_IDENTITY_CLIENT_ID !== 'undefined'
            ? __ENV.PR_IDENTITY_CLIENT_ID
            : 'xero_collecting-payments_payment-request-service-consumer',
        clientSecret: typeof __ENV.PR_IDENTITY_CLIENT_SECRET !== 'undefined'
            ? __ENV.PR_IDENTITY_CLIENT_SECRET
            : '',
        scope: 'xero_collecting-payments_payment-request-service.create xero_collecting-payments_payment-request-service.prepare'
    },
    paymentExecutionClientConfig: {
        clientId: typeof __ENV.PE_IDENTITY_CLIENT_ID !== 'undefined'
            ? __ENV.PE_IDENTITY_CLIENT_ID
            : 'xero_collecting-payments-execution_payment-execution-service-consumer',
        clientSecret: typeof __ENV.PE_IDENTITY_CLIENT_SECRET !== 'undefined'
            ? __ENV.PE_IDENTITY_CLIENT_SECRET
            : '',
        scope: 'xero_collecting-payments-execution_payment-execution-service.submit'
    },
    tokenUrl: "https://identity-stage.xero-test.com/connect/token",
    paymentRequestUrl: "https://payment-execution.global.xero-test.com/requests",
    paymentExecutionUrl: "https://payment-execution.global.xero-test.com/execution",
    providerAccountIds: ["acct_1REdHv2S5le7Pbz1"]
};

export const uat: EnvironmentConfigData = {
    environmentName: "uat",
    paymentRequestClientConfig: {
        clientId: typeof __ENV.PR_IDENTITY_CLIENT_ID !== 'undefined'
            ? __ENV.PR_IDENTITY_CLIENT_ID
            : 'xero_collecting-payments_payment-request-service-consumer',
        clientSecret: typeof __ENV.PR_IDENTITY_CLIENT_SECRET !== 'undefined'
            ? __ENV.PR_IDENTITY_CLIENT_SECRET
            : '',
        scope: 'xero_collecting-payments_payment-request-service.create xero_collecting-payments_payment-request-service.prepare'
    },
    paymentExecutionClientConfig: {
        clientId: typeof __ENV.PE_IDENTITY_CLIENT_ID !== 'undefined'
            ? __ENV.PE_IDENTITY_CLIENT_ID
            : 'xero_collecting-payments-execution_payment-execution-service-consumer',
        clientSecret: typeof __ENV.PE_IDENTITY_CLIENT_SECRET !== 'undefined'
            ? __ENV.PE_IDENTITY_CLIENT_SECRET
            : '',
        scope: 'xero_collecting-payments-execution_payment-execution-service.submit'
    },
    tokenUrl: "https://integration-identity.xero-uat.com/connect/token",
    paymentRequestUrl: "https://payment-execution.global.xero-uat.com/requests",
    paymentExecutionUrl: "https://payment-execution.global.xero-uat.com/execution",
    providerAccountIds: ["acct_1Og0k9S4ztAMTLGB"]
};

export const environments: EnvironmentConfigData[] = [development, uat, test];
