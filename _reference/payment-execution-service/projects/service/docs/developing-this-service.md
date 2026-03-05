# Developing this service

## Running the service

The recommended way to develop this service is via Visual Studio Code.

In the Run and Debug tab there are the following launch configurations:
 - `Run and Debug Service`
 - `Run and Debug Service in Docker`
 - `Attach to .NET Process`
 - `Run Service with Hot Reload`

Dependencies are started prior to debugging and stopped once the debugger has been detached.

Alternatively you can:
<details><summary>Run the service via the CLI</summary>

```sh
# Start local dependencies
$ make start-local-dependencies

# Run the service
$ dotnet run --project src/PaymentExecutionService.Api

# Stop local dependencies
$ make stop-local-dependencies
```

</details>

<details><summary>Run the service in a Docker container</summary>

```sh
# Start local dependencies
$ make start-local-dependencies

# Run container
$ make start-api

# Stop container
$ make stop-api

# Stop local dependencies
$ make stop-local-dependencies
```

</details>


Navigate to http://localhost:5000/ping to verify the ping endpoint.

Navigate to http://localhost:5000/swagger/v1/swagger.json to view the OpenAPI documentation

#### Integrating with local stack
Localstack is used to mock AWS services and requires a dummy set of credentials to connect to it. We have defined a profile defined in `appsettings.Development.json` which you will need to have on your local machine
```json
"AWS": {
    "Profile": "localstack",
    "Region": "ap-southeast-2",
    "ServiceURL": "http://localhost:4566"
}
```
In your local credentials file located at ~.aws/credentials, place the following configuration:
```text
[localstack]
aws_access_key_id=test
aws_secret_access_key=test%
```

## Tasks

### Running tasks

Check out the list of tasks in your [Makefile](../Makefile)

## Logs

Seq aggregates logs from everything running in your APIs local environment.

`make start-local-dependencies` will start Seq, you can view and query your local environments aggregated logs via your browser at http://localhost:5341.

[Search and analyze logs with Seq](https://docs.datalust.co/docs/the-seq-query-language).

## Identity

Requests to secured endpoints of this API must include a token with an attached scope defined in `appsettings.json` `Identity::AllowedScopes`.

Requests from this API to other Xero Identity secured APIs will request a token through the services `XeroIdentityClient`. The scopes attached to this token are defined in `appsettings.json` `Identity::Scopes`.

### Local Identity

When running the solution using the `Development` hosting environment (default). The API secures its endpoints via the local identity mock started via `make start-local-dependencies`.

The local identity mock configuration is [located here](../docker/identity-mock/custom_config/). 
 - The `local_caller` client should be used to fetch a token to call this API.
 - The `xero_collecting-payments-execution_payment-execution-service` client is used by this API to fetch tokens to call other APIs.

To hit the APIs secured endpoints a token must be requested from the mock.

Via the Postman Authorisation tab:

| Key                   | Value                                         |
| --------------------- | --------------------------------------------- |
| Type                  | OAuth 2.0                                     |
| Header Prefix         | Bearer                                        |
| Grant Type            | Client Credentials                            |
| Access Token URL      | http://localhost:5003/connect/token           |
| Client ID             | local_caller                                  |
| Client Secret         | secret                                        |
| Scope                 | xero_collecting-payments-execution_payment-execution-service |
| Client Authentication | Send as Basic Auth Header                     |


## AuthZ

Requests to secured endpoints of this API must also include:
 - A header `Xero-User-Id`, its value being a User Id which has a role with permissions to pass the AuthZ policy securing the endpoint.
 - A URL parameter `{{tenantId}}` which contains a Tenant Id with permissions to pass the AuthZ policy securing the endpoint.

### Local AuthZ

When running the solution using the `Development` hosting environment (default). The API secures its endpoints via the local auth service started via `make start-local-dependencies`.

Local Authorisation policies are [located here](../docker/auth-service/).

### AuthZ in Pre Production Environments.

Authorisation in Uat and Production consumes policies [located in this Github repository](https://github.dev.xero.com/authorisation/authorisation.policy).

### Secret management

The implementation provided utilises AWS Secrets Manager using [security's recommendations](https://xero.atlassian.net/wiki/x/hQC_2j4).

Overall secret management guidance from security can be viewed [here](https://xero.atlassian.net/wiki/x/coDH2j4).

To create a secret in Secrets Manager we provide a `create-secret` task in the `Makefile` that accepts the name of the secret, the description of the secret, the infrastructure KMS key you requested for your service and the secret you want to store. 

Pre-requisities:
- [Set up AWS SSO](https://xerohelp.zendesk.com/hc/en-us/articles/4402981528601-AWS-SSO-via-the-AWS-CLI)
- An AWS Role of `Developer` (principle of least privilege) for each environment you want the secret deployed to
- Your KMS keys requested for infrastructure deployment

To encrypt a secret using this task (remember to create your secret in every environment your service operates in):

1. Assume a profile in the AWS account you want to store the secret in (`Developer` role)
2. Open a terminal session (Powershell, bash, etc) where your project's Makefile is located and run the following with your values (replace `env` with `uat` or `prod` depending on environment you're creating a secret in):
`make create-secret-for-env NAME=your-secret-name DESCRIPTION="your secret description" SECRET=your-secret`
3. You will be presented with output similar to this if the creation is successful:
```json
{
    "ARN": "arn:aws:secretsmanager:ap-southeast-2:639642979352:secret:test-sgn7WG",
    "Name": "test",
    "VersionId": "92bd7953-d063-4418-a099-d4a2babdfcb3"
}
```
4. We can then reference this ARN in a `aws_iam_policy_document` with your secret with the `secretsmanager:GetSecretValue` action allowed. You will need change the region and the account id on the ARN for each environment. The last few random digits after the secret name can be replaced with `??????`.
