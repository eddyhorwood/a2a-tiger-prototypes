import http from "k6/http";
import encoding from "k6/encoding";
import {environments, EnvironmentConfigData, development, ClientConfig} from "./environmentConfig";
import {check} from "k6";

export type TokenData = { access_token: string | null, refreshed_at: number };

export function refreshToken(tokenData: TokenData, tokenUrl: string, clientConfig: any): TokenData {
    if (!tokenData.access_token || (Date.now() - tokenData.refreshed_at) > 60000) {
        const newToken = getAuthToken(tokenUrl, clientConfig);
        return {
            access_token: newToken,
            refreshed_at: Date.now()
        };
    }
    return tokenData;
}

export function uuidv4() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        const r = Math.random() * 16 | 0;
        const v = c === 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
}

export function randomItem(array: string[]): string {
    return array[Math.floor(Math.random() * array.length)];
}

export function getEnvironmentConfig(): EnvironmentConfigData {
    return environments.find(x => x.environmentName === __ENV.ENV) || development
}

export function getAuthToken(
    url: string,
    clientConfig: ClientConfig,
): string {
    const authStr = `${clientConfig.clientId}:${clientConfig.clientSecret}`;
    const b64AuthStr = encoding.b64encode(authStr);
    let headers = {
        'accept': 'application/json',
        'Content-Type': 'application/x-www-form-urlencoded',
        'Authorization': `Basic ${b64AuthStr}`
    };

    const payload = {
        'grant_type': 'client_credentials',
        'scope': clientConfig.scope
    }

    const res = http.post(url, payload, {headers: headers});
    check(res, {
        ["Token successfully received for " + clientConfig.clientId]: (r) => r.status === 200,
    });
    return res.json('access_token') as string;
}

