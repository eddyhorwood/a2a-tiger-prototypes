## Provider Pact testings

Provider tests read incoming consumer contracts from a source (PactFlow configured consumers, PactFlow url, local file) and verify that the provider application satisfies those contracts. 

The current CI/CD workflow does the following:

1. Provider pact tests are invoked with a version setting `provider-sha-123` and loads the contracts of consumers based on the [CreateConsumerVerionSelectors](https://github.com/xero-internal/payment-execution-service/blob/main/projects/service/tests/PaymentExecutionService.ProviderPactTests/ProviderTests.cs#L101-L112), which learns from pactflow the deployed version, e.g `consumer-a-sha-321`
2. Provider pact tests verify the contracts and uploads the result to the pactflow broker:
```text
provider-sha-123 + consumer-a-sha-321 = pass
provider-sha-123 + consumer-b-sha-999 = fail
```
3. Because consumers also [record their deployments](https://github.com/xero-internal/payment-execution-service/blob/main/.github/workflows/service-ci-cd.yaml#L252-L264) they associate a version with an environment
4. The can-i-deploy check just does a lookup of the results `provider-sha-123 + consumer-a-version-in-test = ? ` and passes / fails the job based on the result of that lookup .

### I'm a consumer and want to write a test 

- Please ensure any identifiers are unique to your test run, as we run provider tests against a real database in docker and conflicting IDs may have unexpected results. 
- We only verify that a bearer authorisation header is present, and that the token doesn't need to be real. Currently advise to avoid writing contracts which assert auth workflows.
- We're mocking our downstream integrations with Payment Request Service and Stripe Execution Service. They will return a hardcoded response.

### I'm a consumer and I need to set up certain results in the provider data store during a test

Say for example you're writing a consumer contract against our `GET /v1/payments` endpoint and need to setup data in our datastore. 

We currently have no Provider State actions to set this up - please let us know in [#pod-cashtopia](https://xero.enterprise.slack.com/archives/C074HRXHHAA) and we can prioritise it. 


### I'm debugging a can-i-deploy error and want to verify against the service.

1. Download the failing pact file from pactflow. Store it in a json file in the ProviderPactTest directory.
2. Create an `.env` file which resembles the below

```shell
PACT_FILE_PATH=<replace-with-path-to-downloaded-pact.json>
PACT_BROKER_TOKEN=not-important
PACT_BROKER_BASE_URL=http://not-important
PACT_GIT_BRANCH=not-important
PACT_PROVIDER_VERSION=not-important
PACT_BUILD_URI=not-important
```

3. Update `PaymentExecutionService.ProviderPactTests.csproj` to include the new file
```xml
 <ItemGroup>
   <Content Include="./<replace-with-path-to-downloaded-pact>.json" >
     <CopyToOutputDirectory>Always</CopyToOutputDirectory>
   </Content>
 </ItemGroup>
```

4. Start local dependencies and run the test (invoke test in rider, or just `dotnet test`)

```shell
# assuming you're in the payment-execution-service/projects/service directory
make start-local-dependencies
cd tests/PaymentExecutionService.ProviderPactTests

# tell it to load development appsettings so it knows to talk to local resources.
DOTNET_ENVIRONMENT=Development dotnet test 
```
