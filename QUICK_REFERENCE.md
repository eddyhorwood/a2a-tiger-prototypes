# Quick Reference - Key Files

## Controllers

### Merchant/Org Setup
- [`MerchantOnboardingController.cs`](A2APaymentsApp/Controllers/MerchantOnboardingController.cs) - Merchant onboarding, bank account selection, OAuth flow

### Payer Journey
- [`PayerController.cs`](A2APaymentsApp/Controllers/Payer/PayerController.cs) - Payment initiation, Akahu redirect, callback handling

### Webhooks
- [`WebhookController.cs`](A2APaymentsApp/Controllers/Webhook/WebhookController.cs) - Payment completion webhooks, signature verification

### Core Framework
- [`BaseXeroOAuth2Controller.cs`](A2APaymentsApp/Controllers/BaseXeroOAuth2Controller.cs) - Base controller with Xero OAuth token management
- [`ApiAccessorController.cs`](A2APaymentsApp/Controllers/ApiAccessorController.cs) - Generic API accessor with tenant context
- [`AuthorizationController.cs`](A2APaymentsApp/Controllers/AuthorizationController.cs) - OAuth callbacks and tenant selection

## Services & Clients

### External Integration
- [`AkahuClient.cs`](A2APaymentsApp/Clients/AkahuClient.cs) - Akahu API client (create payment, poll status)
- [`IAkahuClient.cs`](A2APaymentsApp/Clients/IAkahuClient.cs) - Interface definition

### Data Access
- [`DatabaseService.cs`](A2APaymentsApp/Services/DatabaseService.cs) - Organisation CRUD operations

## Configuration

- [`appsettings.json`](A2APaymentsApp/appsettings.json) - App configuration (Xero OAuth, Akahu API keys)
- [`AkahuSettings.cs`](A2APaymentsApp/Config/AkahuSettings.cs) - Akahu config model
- [`XeroConfiguration.cs`](Xero.NetStandard.OAuth2/Config/XeroConfiguration.cs) - Xero OAuth config

## Models

- [`Organisation.cs`](A2APaymentsApp/Models/Organisation.cs) - Merchant org data model
- [`MerchantOnboardingModel.cs`](A2APaymentsApp/Models/MerchantOnboardingModel.cs) - Onboarding form model
- [`PaymentServiceSelectionModel.cs`](A2APaymentsApp/Models/PaymentServiceSelectionModel.cs) - Payment service selection

## Database

- [`Migrations/`](A2APaymentsApp/Migrations/) - EF Core migrations
  - `20251022021742_OrganisationTable.cs` - Organisations table
  - `20251022032104_AddTokensColToOrganisationTable.cs` - OAuth tokens

## Views

- [`Views/MerchantOnboarding/`](A2APaymentsApp/Views/MerchantOnboarding/) - Onboarding UI
- [`Views/Payer/`](A2APaymentsApp/Views/Payer/) - Payer payment flow UI

## Infrastructure

- [`Dockerfile`](Dockerfile) - Container definition
- [`infra/`](infra/) - Infrastructure as code (if any)
- [`.vscode/`](.vscode/) - VS Code launch configs

## Entry Points

- [`Program.cs`](A2APaymentsApp/Program.cs) - Application bootstrap
- [`Startup.cs`](A2APaymentsApp/Startup.cs) - Service configuration & middleware pipeline

---

## Common Development Tasks

### Add a new Akahu API endpoint
1. Add method signature to [`IAkahuClient.cs`](A2APaymentsApp/Clients/IAkahuClient.cs)
2. Implement in [`AkahuClient.cs`](A2APaymentsApp/Clients/AkahuClient.cs)
3. Call from controller

### Add a new database column
1. Update model in [`Models/Organisation.cs`](A2APaymentsApp/Models/Organisation.cs)
2. Run `dotnet ef migrations add <MigrationName>`
3. Run `dotnet ef database update`

### Add a new webhook handler
1. Add handler method in [`WebhookController.cs`](A2APaymentsApp/Controllers/Webhook/WebhookController.cs)
2. Update `ProcessPayloadQueue()` method
3. Test with ngrok + webhook simulator

### Debug OAuth issues
1. Check [`AuthorizationController.cs`](A2APaymentsApp/Controllers/AuthorizationController.cs) callback handling
2. Verify [`appsettings.json`](A2APaymentsApp/appsettings.json) ClientId/Secret/Scope
3. Check token storage in `BaseXeroOAuth2Controller`
