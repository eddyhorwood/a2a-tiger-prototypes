# A2A Payments NZ - Project Summary

## Overview
Safer Account-to-Account (A2A) Payments for New Zealand enables Xero customers to accept direct bank payments via **Akahu** (NZ's Open Banking provider). This closes "the loop" by automatically marking invoices as paid and enabling 1:1 auto-reconciliation in the bank feed.

## Current Status
- **Phase**: Pre-PVT (Production Verification Testing)
- **PVT Start**: Target 13 April 2026
- **Repository**: Hackathon prototype, scaffolded from Xero .NET sample app
- **Technology**: .NET 9.0 MVC, Entity Framework Core, SQLite

## Key Components

### 1. Merchant Onboarding Flow
**Location**: [`A2APaymentsApp/Controllers/MerchantOnboardingController.cs`](A2APaymentsApp/Controllers/MerchantOnboardingController.cs)

- Xero OAuth 2.0 1st Party App authentication
- Select settlement bank account from Xero GL
- Configure chart of accounts mapping
- Store merchant config in local SQLite database

### 2. Payment Flow (Payer Experience)
**Location**: [`A2APaymentsApp/Controllers/Payer/PayerController.cs`](A2APaymentsApp/Controllers/Payer/PayerController.cs)

1. Payer clicks "Pay by Bank" link on invoice
2. App creates payment via Akahu API
3. Redirect to Akahu OAuth flow
4. Payer authenticates with their NZ bank (ANZ, ASB, BNZ, Westpac, Kiwibank)
5. Payment initiated via Open Banking rails
6. Callback to app with payment status
7. Poll Akahu API until payment reaches terminal state (SENT/FAILED/CANCELLED)

### 3. Akahu Integration
**Location**: [`A2APaymentsApp/Clients/AkahuClient.cs`](A2APaymentsApp/Clients/AkahuClient.cs)

- **Create Payment**: `POST /v1/one-off-payments`
- **Poll Status**: `GET /v1/one-off-payments/{id}`
- **Auth**: HTTP Basic (AppToken:AppSecret)

#### Payment Request Structure
```csharp
{
    Amount: decimal,
    RedirectUri: string,
    Payee: {
        Name: string,
        AccountNumber: string, // Formatted NZ bank account
        Particulars: "Invoice",
        Code: invoiceNo,
        Reference: invoiceNo // Statement reference for auto-match
    }
}
```

### 4. Webhook Handler
**Location**: [`A2APaymentsApp/Controllers/Webhook/WebhookController.cs`](A2APaymentsApp/Controllers/Webhook/WebhookController.cs)

- Receives payment completion webhooks (Akahu & Xero)
- Signature verification using HMAC-SHA256
- Background processing of webhook queue
- **TODO**: Mark invoice as paid via Xero API

### 5. Data Model
**Location**: [`A2APaymentsApp/Migrations/`](A2APaymentsApp/Migrations/)

#### Organisation Table
```sql
CREATE TABLE Organisations (
    TenantId TEXT PRIMARY KEY,
    TenantShortCode TEXT NOT NULL,
    BankAccountNumber TEXT NOT NULL,
    AccountIdForPayment TEXT NOT NULL,
    AccessToken TEXT,
    RefreshToken TEXT
)
```

## Configuration
**Location**: [`A2APaymentsApp/appsettings.json`](A2APaymentsApp/appsettings.json)

Required settings:
- `XeroConfiguration.ClientId` - Xero OAuth app client ID
- `XeroConfiguration.ClientSecret` - Xero OAuth app secret
- `XeroConfiguration.Scope` - Must include `paymentservices`
- `AkahuSettings.AppToken` - Akahu API token
- `AkahuSettings.AppSecret` - Akahu API secret
- `WebhookSettings.WebhookKey` - Webhook signature verification key

## PVT Plan Objectives

### Success Criteria
1. ✅ **Onboarding Integrity**: 1P App correctly fetches & maps Xero bank accounts
2. ✅ **Payment Success**: Akahu redirect & bank auth flows work across major NZ banks
3. 🔄 **Loop Closure**: Payment webhook triggers invoice "Mark as Paid"
4. 🔄 **Auto-Match**: Statement reference injection enables 1:1 bank rec matching
5. ✅ **Security**: Only authorized Xero users can modify settlement config
6. 🔄 **Telemetry**: Transaction events logged for pricing discovery

### Exit Criteria
- 100% pass rate on PVT test scenarios
- Zero P0/P1 bugs in redirect or reconciliation flow
- Visual confirmation of injected reference on bank statements
- 100% auto-match success rate in Xero Bank Reconciliation

### Test Participants
- 10-15 NZ-based Xero employees
- Banks: ANZ, ASB, BNZ, Westpac, Kiwibank
- Test payments: $0.01 - $1.00 (real money, no fees during PVT)

## Architecture Flow

```
┌─────────────┐     ┌──────────────┐     ┌─────────────┐     ┌──────────┐
│   Merchant  │────>│  Xero 1P App │────>│   Akahu     │────>│ NZ Bank  │
│  (Invoice)  │     │   (OAuth)    │     │  (OAuth)    │     │  (Auth)  │
└─────────────┘     └──────────────┘     └─────────────┘     └──────────┘
                           │                     │
                           │                     │
                           ▼                     ▼
                    ┌──────────────┐     ┌─────────────┐
                    │ Xero Invoice │<────│   Webhook   │
                    │ Mark as Paid │     │  (payment_  │
                    │              │     │  completed) │
                    └──────────────┘     └─────────────┘
                           │
                           ▼
                    ┌──────────────┐
                    │  Bank Feed   │
                    │  Statement   │────> Auto-match via reference
                    └──────────────┘
```

## Key Integration Points

### Xero APIs Used
- **OAuth 2.0**: User authentication & tenant selection
- **Accounting API**: 
  - Get Organisations
  - Get Bank Accounts
  - Get Chart of Accounts
  - **TODO**: Get Invoices
  - **TODO**: Create Payments (mark as paid)
- **Payment Services API**: **TODO** - Register/update payment service

### External Dependencies
- **Akahu API**: Payment initiation & status polling
- **Xero Payment Services**: Custom URL flow for invoice payment links

## Known Gaps / TODOs

1. **Invoice Validation**: Currently not validating invoice exists before payment
2. **Mark as Paid**: Webhook handler doesn't yet mark invoice as paid in Xero
3. **Org Name Storage**: Should store org name in DB for better payee display
4. **Payment Services Registration**: Not yet calling Xero Payment Services API to register
5. **Branding Themes**: Need to integrate with branding themes for custom payment links
6. **Error Handling**: Need better error states for failed/cancelled payments
7. **Telemetry**: Need to add structured logging for transaction events

## Environment Setup

### Prerequisites
- .NET 9.0 SDK
- Git
- ngrok (for webhook testing)
- Entity Framework Core CLI tools

### Running Locally
```bash
cd "A2A Tiger/A2APaymentsApp"

# Configure appsettings.json with your Xero & Akahu credentials

# Run migrations
dotnet ef database update

# Start the app
dotnet run

# In another terminal, expose via ngrok
ngrok http https://localhost:5001
```

## Reference Documentation

### Internal
- [PRD: Safer A2A Payments in NZ](https://docs.google.com/document/d/1gYqxVkBCGEjo9AlzYdQ-pKf87Bm8Jeao8huuvo2rWw8/edit?tab=t.0)
- [PVT Plan & Test Scenarios](https://docs.google.com/document/d/1zwRFCMW8Cb_0mlWCshCQwQW8PfqprE4NFf8Yg22EzTs/edit?tab=t.0)
- [High-Level ERD](https://xero.atlassian.net/wiki/x/IgEFSj8)
- [Hackathon ERD & Architecture](https://xero.atlassian.net/wiki/spaces/XFS/pages/271213167124/ERD+-+Hackathon+Safer+Payments+NZ+-+Account+2+Account+payments+NZ)

### External
- [Xero Payment Services Integration Guide](https://developer.xero.com/documentation/guides/how-to-guides/payment-services-integration-with-xero/)
- [Akahu API Documentation](https://developers.akahu.nz/)

## Related Work
- **Melio Integration** (Global → US cross-border): Similar pattern, different provider
  - [PRD: Pay by Melio](https://docs.google.com/document/d/1pj4xfAy2aHQb97fxZiXR-TvD8Nn-6C5UD35HOy4WLx0)
  - [ERD: Melio Ecosystem](https://docs.google.com/document/d/1XE0rmSlujzktzpzWZSQUlT5Y9fUqXWrNYH9GnEIlzNQ)

---

**Last Updated**: 3 March 2026
