# Pay by Bank - Merchant Onboarding Prototype

## Quick Start Guide

### 1. Install Node Dependencies

```bash
cd A2APaymentsApp/ClientApp
npm install
```

**Important**: If `@xero/xui` installation fails, you may not have access to Xero's private npm registry. See the "XUI Setup" section below.

### 2. Start Development Server

In one terminal, run the React dev server:

```bash
cd A2APaymentsApp/ClientApp
npm run dev
```

This starts Vite at `http://localhost:5173`.

### 3. Run the .NET Application

In another terminal, run the .NET app:

```bash
cd A2APaymentsApp
dotnet run
```

### 4. Access the Onboarding Flow

Open your browser and navigate to:

**Onboarding Wizard**: `https://localhost:5001/MerchantOnboarding/React`

The flow will guide you through:
1. ✨ **Introduction** - Overview of Pay by bank
2. 🏦 **Settlement Account** - Select bank account
3. ✅ **Guardrails** - Legal acknowledgement
4. 🎉 **Confirmation** - Success state

After completion, you'll be navigated to the settings page where you can manage the configuration.

---

## XUI Setup

### Option A: Access Xero's Private Registry (Recommended)

If you have access to Xero's npm registry:

```bash
npm config set @xero:registry https://registry.xero.com/npm/
npm login --registry=https://registry.xero.com/npm/ --scope=@xero
```

Then install:

```bash
npm install
```

### Option B: Use Mock XUI Components (Fallback)

If you don't have registry access, you can temporarily mock the XUI components:

1. Comment out the `@xero/xui` import in `package.json`
2. Create mock XUI components in `src/components/xui/`
3. Update imports to use the mocks

---

## Build for Production

To compile the React app for deployment:

```bash
cd A2APaymentsApp/ClientApp
npm run build
```

This outputs compiled assets to `wwwroot/dist/`. The .NET app will serve these files in production.

---

## Project Structure

```
A2APaymentsApp/
├── ClientApp/                         # React application
│   ├── src/
│   │   ├── components/
│   │   │   └── onboarding/           # 4-step wizard components
│   │   ├── pages/
│   │   │   ├── OnboardingWizard.tsx  # Main wizard orchestrator
│   │   │   └── SettingsPage.tsx      # Settings management UI
│   │   ├── services/
│   │   │   └── api.ts                # API client (stubbed)
│   │   └── main.tsx
│   ├── package.json
│   ├── vite.config.ts
│   └── tsconfig.json
├── Controllers/
│   └── MerchantOnboardingController.cs  # Added React() action
├── Views/
│   └── MerchantOnboarding/
│       └── React.cshtml                 # React app host
└── wwwroot/
    └── dist/                            # Compiled React assets (after build)
```

---

## Features Implemented

### ✅ Merchant Onboarding (Happy Path)

- 4-step wizard with `XUIStepper` navigation
- Bank account selection with radio buttons
- Legal guardrails with checkbox acknowledgement
- Success confirmation screen
- Stubbed API with localStorage persistence

### ✅ Settings Management

- View enabled/disabled state
- Display settlement account details
- Enable/disable Pay by bank
- Change settlement account (routes back to wizard)

### ✅ XUI Components Used

- `XUIStepper` - Multi-step wizard navigation
- `XUIButton` - Primary, borderless, destructive variants
- `XUILoader` - Loading state during API calls
- `XUICompositionDetail` - Layout composition
- XUI CSS classes for typography and spacing

---

## API Stubbing

The prototype uses **localStorage** for persistence. No real backend integration yet.

**Stubbed Endpoints** (in `src/services/api.ts`):
- `getEligibleAccounts()` → Returns 3 mock NZ bank accounts
- `getConfig()` → Returns current config (enabled status, settlement account)
- `updateConfig()` → Saves config to localStorage

**To replace with real APIs**: Update `src/services/api.ts` to call your .NET backend endpoints.

---

## Next Steps

### 1. Create Backend API Endpoints

Add these routes to your .NET controllers:

```csharp
// GET /api/accounts/eligible-for-a2a
[HttpGet("api/accounts/eligible-for-a2a")]
public async Task<IActionResult> GetEligibleAccounts()
{
    // Return bank accounts with EnablePayments=true
}

// GET /api/a2a/config
[HttpGet("api/a2a/config")]
public async Task<IActionResult> GetA2AConfig()
{
    // Return current config from database
}

// PUT /api/a2a/config
[HttpPut("api/a2a/config")]
public async Task<IActionResult> UpdateA2AConfig([FromBody] A2AConfigDto config)
{
    // Save config to database
    // Register payment service with API.Accounting
}
```

### 2. Wire Up Real APIs

Update `ClientApp/src/services/api.ts` to use `fetch()` with your backend URLs.

### 3. Add Payer Flow

Build the payer experience:
- Invoice view with "Pay by bank" button
- Pre-redirect explanation page
- Akahu OAuth redirect
- Return/success page

### 4. Deploy

- Run `npm run build` to compile React assets
- Deploy .NET app with compiled `wwwroot/dist/` folder
- Configure production URLs in `vite.config.ts`

---

## Testing the Flow

### Happy Path Test

1. Navigate to `/MerchantOnboarding/React`
2. **Step 1**: Click "Continue"
3. **Step 2**: Select "Business Cheque Account"
4. **Step 3**: Check the acknowledgement box, click "Enable Pay by bank"
5. **Step 4**: Verify confirmation shows correct account
6. Click "Done" → Redirects to settings page
7. Verify "Enabled" status pill appears
8. Verify settlement account is displayed

### Disable Test

1. From settings page, click "Disable Pay by bank"
2. Confirm the dialog
3. Verify UI returns to disabled state with "Enable" button

### Change Account Test

1. From enabled state, click "Change settlement account"
2. Should route back to onboarding wizard
3. Complete wizard with different account selection
4. Verify settings page updates with new account

---

## Troubleshooting

### React App Not Loading

- Ensure Vite dev server is running (`npm run dev`)
- Check console for errors
- Verify `wwwroot/dist/assets/main.js` exists (after build)

### XUI Components Not Found

- Check `@xero/xui` is installed: `npm list @xero/xui`
- Verify registry access (see XUI Setup above)
- Try clearing node_modules and reinstalling

### Stepper Not Rendering

- Check XUI CSS is imported: `import '@xero/xui/css/index.css'`
- Inspect browser console for CSS loading errors
- Verify XUI version compatibility

### LocalStorage Not Persisting

- Check browser developer tools → Application → Local Storage
- Key should be `a2a_config`
- Clear storage to reset: `localStorage.clear()`

---

## Demo Video Script

**Intro**: "This is the merchant onboarding flow for Pay by Bank, built with XUI components and React."

**Step 1**: "First, we introduce the merchant to Pay by bank and explain how it works."

**Step 2**: "Next, the merchant selects which bank account they want payments to settle into. We fetch their existing Xero accounts."

**Step 3**: "We show guardrail language to comply with AML/CFT requirements. The merchant must acknowledge they understand how the service works."

**Step 4**: "Finally, we confirm the setup is complete and show a summary of their configuration."

**Settings**: "The merchant can now manage their Pay by bank settings, including changing the settlement account or disabling the service."

---

## Questions?

See the [PROTOTYPE_REQUIREMENTS_BRIEF.md](../feature-traces/a2a-onboarding-ux/PROTOTYPE_REQUIREMENTS_BRIEF.md) for detailed architecture analysis and implementation decisions.
