# A2A Payment Onboarding - React SPA

This directory contains the React single-page application for merchant onboarding.

## Getting Started

### 1. Install Dependencies

```bash
cd A2APaymentsApp/ClientApp
npm install
```

**Note**: If you don't have `@xero/xui` access, you may need to configure npm to access Xero's private registry or update the package.json to use a compatible version.

### 2. Development Mode

Run the Vite dev server:

```bash
npm run dev
```

This will start the development server at `http://localhost:5173`.

### 3. Build for Production

Build the React app to `wwwroot/dist`:

```bash
npm run build
```

The compiled assets will be available for the .NET app to serve.

### 4. Run the .NET App

In the project root:

```bash
cd ..
dotnet run
```

Navigate to:
- Onboarding: `https://localhost:5001/MerchantOnboarding/React`
- Settings: `https://localhost:5001/settings/payment-by-bank` (once React is running)

## Project Structure

```
ClientApp/
├── src/
│   ├── components/
│   │   └── onboarding/          # 4 step components
│   │       ├── IntroStep.tsx
│   │       ├── SettlementAccountStep.tsx
│   │       ├── GuardrailsStep.tsx
│   │       ├── ConfirmationStep.tsx
│   │       └── Steps.css
│   ├── pages/
│   │   ├── OnboardingWizard.tsx  # Main wizard orchestrator
│   │   └── SettingsPage.tsx      # Settings management
│   ├── services/
│   │   └── api.ts                # Stubbed API service
│   ├── App.tsx
│   ├── main.tsx
│   └── index.css
├── vite.config.ts
├── tsconfig.json
└── package.json
```

## Features Implemented

### ✅ 4-Step Onboarding Wizard

1. **Introduction**: Overview of Pay by bank
2. **Settlement Account**: Select bank account from list
3. **Guardrails & Acknowledgement**: Legal terms with checkbox
4. **Confirmation**: Success state with summary

### ✅ XUI Components

- `XUIStepper` - Multi-step navigation
- `XUIButton` - Primary, borderless, destructive variants
- `XUILoader` - Loading state during enable
- `XUICompositionDetail` - Layout wrapper

### ✅ Settings Management

- Enable/Disable Pay by bank
- View current settlement account
- Change settlement account (re-routes to wizard)

### ✅ State Management

- React hooks (useState, useEffect)
- LocalStorage for stubbed persistence
- Navigation with React Router

## Stubbed APIs

All APIs use `localStorage` for demo purposes:

- `GET /api/accounts/eligible-for-a2a` → Returns 3 mock NZ bank accounts
- `GET /api/a2a/config` → Returns current config (enabled, settlement_account_id)
- `PUT /api/a2a/config` → Saves config to localStorage

## Next Steps

### Integration with Backend

Replace the stubbed API in `src/services/api.ts` with actual calls to your .NET controllers:

```typescript
export const api = {
  async getEligibleAccounts(): Promise<BankAccount[]> {
    const response = await fetch('/api/accounts/eligible-for-a2a')
    return response.json()
  },
  
  async getConfig(): Promise<A2AConfig> {
    const response = await fetch('/api/a2a/config')
    return response.json()
  },
  
  async updateConfig(config: A2AConfig) {
    const response = await fetch('/api/a2a/config', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(config)
    })
    return response.json()
  }
}
```

### Add Controller Route

Update `MerchantOnboardingController.cs` to serve the React view:

```csharp
public IActionResult React()
{
    return View();
}
```

### XUI Verification

If `@xero/xui` import fails, verify:
1. You have access to Xero's npm registry
2. The package version matches your environment
3. Check XUI documentation for component API changes

## Troubleshooting

### "Cannot find module '@xero/xui'"

You may need to configure npm to access Xero's private registry:

```bash
npm config set @xero:registry https://registry.npmjs.xero.com/
```

Or use a different XUI version:

```json
"@xero/xui": "^9.0.0"
```

### Build Output Not Found

Ensure Vite build completes successfully and outputs to `wwwroot/dist`. Check `vite.config.ts` paths.

### React Router 404s

The .NET app needs to forward client-side routes to the React app. Add middleware in `Startup.cs`:

```csharp
app.UseSpa(spa =>
{
    spa.Options.SourcePath = "ClientApp";
});
```

## Demo Mode

The current implementation uses stubbed data. To see the full flow:

1. Visit `/MerchantOnboarding/React`
2. Click through the 4-step wizard
3. Complete onboarding
4. Visit `/settings/payment-by-bank` to see the enabled state
5. Click "Disable" to reset
