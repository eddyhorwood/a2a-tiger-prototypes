# A2A Onboarding UX - Prototype Requirements Brief
**Date**: 4 March 2026  
**Prototype Goal**: Build prod-ready-feeling onboarding flow using existing Xero patterns and XUI components

---

## Executive Summary

Based on analysis of existing Xero payment onboarding flows, I've identified the key architectural patterns, XUI components, and implementation decisions needed to build an authentic prototype for A2A Pay by Bank onboarding.

**Key Findings:**
1. **Microfrontend Architecture**: Xero payment onboarding uses dedicated MFEs, not inline wizards
2. **XUIStepper Component**: Multi-step flows use `@xero/xui/react/stepper` 
3. **Settings Page Structure**: Payment services are managed in dedicated settings panels with tile-based UI
4. **No Direct "Online Payments" Page Found**: Payment settings appear distributed across multiple repos

---

## 1. Existing Xero Payment Onboarding Patterns

### 1.1 Architecture

**Microfrontend Pattern** (from `cp-payment-plan-ui`):
```tsx
<Microfrontend
  hostApp="paymentplans"
  isModalOpen={true}
  microfrontendName="inpay:paymentServicesOnboarding"
  microfrontendVersion="1.x"
  provisioningContext={{
    step: "consider",
    subject: "payment-plans",
  }}
  onExit={onExit}
/>
```

**Key Insight**: Payment service onboarding is handled by a separate microfrontend, not inline React components. This keeps the provisioning logic decoupled.

**For Your Prototype**: Since you're building a standalone demo, you can simplify this by implementing the wizard directly in your app rather than using MFEs.

### 1.2 Multi-Step Wizard Pattern

**XUIStepper Component** (from `SubscriptionWizard.tsx`):
```tsx
<XUIStepper
  currentStep={currentStep}
  id="stepper-inline-standard"
  tabs={[
    { name: "Plan Selection", isDisabled: false, wizardPage: 1 },
    { name: "Add-ons", isDisabled: true, wizardPage: 2 },
    { name: "Billing Details", isDisabled: true, wizardPage: 3 }
  ]}
  updateCurrentStep={(index: number) => {
    setWizardPage(tabs[index].wizardPage)
  }}
/>
```

**Pattern**:
- Steps are defined as an array with `name`, `isDisabled`, and a page identifier
- `currentStep` is managed via index (0-based)
- Future steps are `isDisabled: true` until reached
- Clicking previous steps allows navigation backwards
- Step content is rendered outside the stepper component

### 1.3 Settings Page Structure

**Key Findings**:
- Settings use `xui-page-width-large` or `xui-page-width-standard` wrapper classes
- Sections are wrapped in `<article>` tags
- Feature flag pattern: Components conditionally render based on flags
- Modals dispatched via Redux state (not inline state)

**Example** (from `DefaultSettings.tsx`):
```tsx
<div className="xui-page-width-large">
  <BannerMessages />
  {featureFlagEnabled && (
    <article>
      <SurchargePanel orgId={orgId} userId={userId} />
    </article>
  )}
  {anotherFeatureEnabled && (
    <article>
      <ScheduledPayment orgId={orgId} userId={userId} />
    </article>
  )}
</div>
```

---

## 2. XUI Component Library

Based on code analysis, here are the confirmed XUI components you can use:

### 2.1 Confirmed Components

| Component | Import | Usage |
|-----------|--------|-------|
| **XUIStepper** | `@xero/xui/react/stepper` | Multi-step wizard navigation |
| **XUIButton** | `@xero/xui/react/button` | Primary, secondary, borderless buttons |
| **XUIBanner** | `@xero/xui/react/banner` | Info/warning/error messages |
| **XUILoader** | `@xero/xui/react/loader` | Loading spinners |
| **XUIModal** | `@xero/xui/react/modal` | Dialogs and overlays (inferred) |
| **XUICompositionDetail** | `@xero/xui/react/compositions` | Layout wrapper for detail views |

### 2.2 Likely Components (Need Verification)

Your PRD requires these; confirm they exist in XUI docs:
- **Checkbox**: For legal acknowledgement
- **Radio/Select**: For bank account selection
- **Status Pill/Badge**: For "Enabled/Disabled" states
- **Tile/Card**: For payment service tiles

**Next Step**: Use the `xui-components-mcp` server to look up exact component APIs.

---

## 3. PRD Implementation Mapping

### 3.1 Onboarding Flow Architecture

**Option A: Full-Page Wizard** (Recommended)
- Dedicated route: `/settings/payment-by-bank/onboarding`
- Full-page stepper (not modal)
- Cleaner for 4 steps with significant content
- Matches Xero's subscription wizard pattern

**Option B: Modal Wizard**
- Stepper inside `XUIModal`
- Entry from settings tile via button
- More constrained space; harder for long legal copy

**Recommendation**: Use **Option A** (full-page) for initial prototype. Easier to build and test step transitions.

### 3.2 Step-by-Step Implementation

#### Step 1: Intro
```tsx
<div className="xui-page-width-standard">
  <h1 className="xui-heading-xlarge">Enable Pay by bank</h1>
  <ul>
    <li>Customers pay you via direct bank transfer from their own bank.</li>
    <li>Payments are initiated via Akahu and executed by the bank.</li>
    <li>Xero and Akahu do not hold or pool funds at any time.</li>
  </ul>
  <XUIButton variant="primary" onClick={handleContinue}>
    Continue
  </XUIButton>
</div>
```

#### Step 2: Settlement Account Selection

**Data Source**: 
```tsx
// API Call (stubbed in prototype)
const accounts = await fetch('/api/accounts?EnablePayments=true')
  .then(r => r.json())

// Expected structure:
[
  {
    accountId: "abc123",
    name: "Business Cheque Account",
    accountNumber: "12-3456-7890123-00", // Masked
    type: "BANK"
  }
]
```

**UI Pattern**:
```tsx
<div className="xui-page-width-standard">
  <h2 className="xui-heading-large">Choose settlement account</h2>
  <p>Select the bank account where Pay by bank deposits should go.</p>
  
  {accounts.map(account => (
    <div key={account.accountId} className="account-selector-item">
      <input 
        type="radio" 
        name="settlement-account" 
        value={account.accountId}
        onChange={handleAccountSelect}
      />
      <label>
        <div className="account-name">{account.name}</div>
        <div className="account-number">{account.accountNumber}</div>
      </label>
    </div>
  ))}
  
  <XUIButton 
    variant="primary" 
    disabled={!selectedAccountId}
    onClick={handleContinue}
  >
    Continue
  </XUIButton>
</div>
```

**Note**: XUI may have a dedicated `AccountSelector` component. Check XUI docs.

#### Step 3: Guardrails + Acknowledgement

```tsx
<div className="xui-page-width-standard">
  <h2 className="xui-heading-large">Understand how Pay by bank works</h2>
  
  <div className="legal-copy xui-margin-bottom">
    <p>
      Pay by bank uses Akahu to initiate direct bank transfers between your 
      customer's bank account and your bank account. Funds move directly 
      between banks. Neither Xero nor Akahu hold or pool funds at any time.
    </p>
    <ul>
      <li>Akahu initiates the payment; the bank executes the transfer.</li>
      <li>Xero collects payment details and passes them to Akahu; Xero does 
          not transfer or hold customer funds.</li>
    </ul>
  </div>
  
  <div className="acknowledgement-checkbox">
    <input 
      type="checkbox" 
      id="ack-checkbox" 
      checked={acknowledged}
      onChange={(e) => setAcknowledged(e.target.checked)}
    />
    <label htmlFor="ack-checkbox">
      I understand that Pay by bank is a direct bank-to-bank transfer 
      initiated via Akahu, and that neither Xero nor Akahu hold or 
      transfer funds on my behalf.
    </label>
  </div>
  
  <XUIButton 
    variant="primary" 
    disabled={!acknowledged}
    onClick={handleEnablePayByBank}
  >
    Enable Pay by bank
  </XUIButton>
</div>
```

**Backend Call on Submit**:
```tsx
const handleEnablePayByBank = async () => {
  await fetch('/api/a2a/config', {
    method: 'PUT',
    body: JSON.stringify({
      enabled: true,
      settlement_account_id: selectedAccountId
    })
  })
  // Navigate to Step 4
  setCurrentStep(3)
}
```

#### Step 4: Confirmation

```tsx
<div className="xui-page-width-standard xui-u-text-align-center">
  <div className="success-icon xui-margin-bottom">✓</div>
  <h2 className="xui-heading-large">Pay by bank is now enabled</h2>
  
  <div className="confirmation-summary xui-margin-vertical-large">
    <h3 className="xui-heading-medium">Summary</h3>
    <dl>
      <dt>Settlement account:</dt>
      <dd>{selectedAccount.name}</dd>
      <dd className="xui-text-deemphasis">{selectedAccount.accountNumber}</dd>
    </dl>
  </div>
  
  <p>
    Customers will now see a 'Pay by bank' option on eligible online invoices.
  </p>
  
  <div className="xui-margin-top-large">
    <XUIButton variant="primary" onClick={handleReturnToSettings}>
      Done
    </XUIButton>
  </div>
</div>
```

### 3.3 Settings Tile (Disabled State)

```tsx
<article className="payment-service-tile">
  <div className="tile-header">
    <h3 className="xui-heading-medium">Pay by bank (Safer A2A)</h3>
  </div>
  
  <p className="tile-description">
    Let customers pay invoices by secure direct bank transfer, initiated 
    via Akahu. Funds move directly between bank accounts; Xero and Akahu 
    don't hold your money.
  </p>
  
  <XUIButton variant="primary" onClick={handleEnableClick}>
    Enable Pay by bank
  </XUIButton>
</article>
```

### 3.4 Settings Tile (Enabled State)

```tsx
<article className="payment-service-tile">
  <div className="tile-header">
    <h3 className="xui-heading-medium">Pay by bank (Safer A2A)</h3>
    <span className="status-pill status-pill--active">Enabled</span>
  </div>
  
  <dl className="tile-details">
    <dt>Deposits to:</dt>
    <dd>{settlementAccount.name}</dd>
  </dl>
  
  <div className="tile-actions">
    <XUIButton variant="borderless-main" onClick={handleChangeAccount}>
      Change settlement account
    </XUIButton>
    <XUIButton variant="borderless-destructive" onClick={handleDisable}>
      Disable Pay by bank
    </XUIButton>
  </div>
</article>
```

---

## 4. Data Model & API Contracts

Your PRD assumes these APIs. For the prototype, stub them:

### 4.1 GET /api/accounts/eligible-for-a2a

**Response**:
```json
[
  {
    "accountId": "550e8400-e29b-41d4-a716-446655440000",
    "name": "Business Cheque Account",
    "accountNumber": "12-3456-7890123-00",
    "type": "BANK",
    "currencyCode": "NZD"
  },
  {
    "accountId": "550e8400-e29b-41d4-a716-446655440001",
    "name": "Business Savings",
    "accountNumber": "12-3456-7890456-00",
    "type": "BANK",
    "currencyCode": "NZD"
  }
]
```

### 4.2 GET /api/a2a/config

**Response**:
```json
{
  "enabled": false,
  "settlement_account_id": null
}
```

### 4.3 PUT /api/a2a/config

**Request**:
```json
{
  "enabled": true,
  "settlement_account_id": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Response**:
```json
{
  "success": true,
  "config": {
    "enabled": true,
    "settlement_account_id": "550e8400-e29b-41d4-a716-446655440000"
  }
}
```

---

## 5. Payer Flow (Happy Path)

### 5.1 Invoice View with Pay by Bank Option

```tsx
<div className="online-payment-options">
  <h3>Payment options</h3>
  
  {/* Other payment methods */}
  
  <button 
    className="payment-option-button"
    onClick={handlePayByBankClick}
  >
    <div className="payment-option-icon">🏦</div>
    <div className="payment-option-label">Pay by bank</div>
    <div className="payment-option-sublabel">Direct bank transfer</div>
  </button>
</div>
```

### 5.2 Pre-Redirect Explanation Page

```tsx
<div className="xui-page-width-standard">
  <h2 className="xui-heading-large">Pay by bank</h2>
  
  <p>
    You'll be redirected to Akahu to initiate a direct bank transfer from 
    your bank account to <strong>{merchantName}</strong>.
  </p>
  
  <p>
    Funds move directly between bank accounts. Xero and Akahu do not hold 
    your money; your bank processes the payment.
  </p>
  
  <div className="xui-margin-top-large">
    <XUIButton variant="primary" onClick={handleRedirectToAkahu}>
      Continue to bank
    </XUIButton>
    <XUIButton variant="borderless-main" onClick={handleCancel}>
      Cancel
    </XUIButton>
  </div>
</div>
```

### 5.3 Return from Akahu (Success State)

```tsx
<div className="xui-page-width-standard xui-u-text-align-center">
  <div className="success-icon xui-margin-bottom">⏳</div>
  <h2 className="xui-heading-large">Payment in progress</h2>
  
  <p>
    Your bank has received the payment instruction.
  </p>
  
  <p>
    Once your bank confirms the transfer, Xero will update this invoice to Paid.
  </p>
  
  <div className="xui-margin-top-large">
    <XUIButton variant="primary" onClick={handleBackToInvoice}>
      Back to invoice
    </XUIButton>
  </div>
</div>
```

---

## 6. Open Questions & Decisions

### 6.1 XUI Component Verification

**Action Required**: Verify these XUI components exist and match your needs:
- [ ] Checkbox component (for acknowledgement)
- [ ] Radio/Select for account picker (or custom `AccountSelector`)
- [ ] Status pill/badge for "Enabled" state
- [ ] Tile/Card component for settings tiles

**How**: Use the `xui-components-mcp` MCP server to fetch component docs.

### 6.2 Stepper Placement

**Decision**: Full-page wizard vs modal wizard?

**Recommendation**: Full-page for initial prototype. Easier to build and matches Xero subscription pattern.

### 6.3 Settings Page Location

**Unknown**: Where exactly does the "Online payments / Bank payments settings" page live in production Xero?

**For Prototype**: Create a mock settings page at `/settings/online-payments` with:
- Heading: "Online payments"
- Section: "Bank payments"
- Tiles for different payment methods (Pay by bank, Direct debit, etc.)

### 6.4 Repeat Acknowledgement

**PRD Open Question**: Does changing settlement account require re-acknowledging the guardrails?

**Recommendation for Prototype**: 
- **First enable**: Full acknowledgement required
- **Changing account**: Show a lighter reminder banner (no checkbox), but require clicking "Confirm"

### 6.5 OPMM Integration

**PRD mentions**: Whether Pay by bank auto-shows in Online Payment Method Management (OPMM).

**For Prototype**: Out of scope. Assume Pay by bank is hardcoded to appear on invoices once enabled.

---

## 7. Next Steps

### 7.1 Phase 1: Verify XUI Components (30 mins)
- Use `xui-components-mcp` to look up:
  - Checkbox
  - Radio / Account selector
  - Status pill/badge
  - Tile/Card
  - Modal (if going modal route)
- Document exact import paths and props

### 7.2 Phase 2: Scaffold Prototype Structure (1 hour)
- Create routes:
  - `/settings/online-payments` (mock settings page)
  - `/settings/payment-by-bank/onboarding` (wizard)
  - `/payer/invoice/:id` (mock invoice view)
  - `/payer/pay-by-bank/processing` (return from Akahu)
- Set up React Router or Next.js routing
- Create placeholder pages

### 7.3 Phase 3: Implement Onboarding Wizard (2-3 hours)
- Build 4-step wizard with `XUIStepper`
- Implement step navigation logic
- Wire up stubbed APIs
- Add form validation (checkbox, account selection)

### 7.4 Phase 4: Build Settings Tile (1 hour)
- Enabled and disabled states
- "Change account" and "Disable" actions
- Confirmation dialogs (if needed)

### 7.5 Phase 5: Build Payer Flow (1 hour)
- Invoice view with payment option
- Pre-redirect explanation page
- Success/processing page (mock Akahu redirect)

### 7.6 Phase 6: Polish & Testing (1 hour)
- Add XUI styling classes
- Test all navigation flows
- Review legal copy against guardrail requirements
- Screenshot key states for PRD validation

---

## 8. Code Reusability from Existing Xero

### 8.1 Copy These Patterns

1. **XUIStepper usage** from `SubscriptionWizard.tsx` (ecosystem-partner-ui)
2. **Settings page structure** from `DefaultSettings.tsx` (collectpayments-provisioning-payment-service)
3. **Payment service list tile pattern** from `PaymentServicesList.tsx`

### 8.2 Don't Copy

- Microfrontend architecture (too complex for prototype)
- Redux state management (use React Context or useState for prototype)
- Feature flags (hardcode enabled/disabled for demo)

---

## 9. Prototype File Structure

**Recommended structure** for your A2A Tiger project:

```
A2APaymentsApp/
├── Views/
│   ├── Settings/
│   │   ├── OnlinePayments.cshtml           # Settings page with tiles
│   │   └── PayByBankOnboarding.cshtml      # 4-step wizard (or React SPA)
│   ├── Payer/
│   │   ├── InvoiceView.cshtml              # Invoice with payment options
│   │   ├── PayByBankExplanation.cshtml     # Pre-redirect page
│   │   └── PaymentProcessing.cshtml        # Success state
├── wwwroot/
│   ├── js/
│   │   └── onboarding-wizard.tsx           # React wizard (if client-side)
│   └── css/
│       └── pay-by-bank.css                 # Custom styles
```

**OR** if going full React SPA:
```
A2APaymentsApp/
├── ClientApp/                               # React app
│   ├── src/
│   │   ├── pages/
│   │   │   ├── SettingsPage.tsx
│   │   │   ├── OnboardingWizard.tsx
│   │   │   ├── InvoiceView.tsx
│   │   │   └── PaymentProcessing.tsx
│   │   ├── components/
│   │   │   ├── PayByBankTile.tsx
│   │   │   ├── StepperWizard.tsx
│   │   │   └── AccountSelector.tsx
│   │   └── App.tsx
```

---

## 10. Success Criteria

Your prototype will be "prod-ready-feeling" if:

- ✅ Uses actual XUI components (not custom CSS imitations)
- ✅ Follows XUIStepper wizard pattern from Xero codebase
- ✅ Settings page layout matches existing payment service tiles
- ✅ Legal guardrail copy is prominent and requires explicit acknowledgement
- ✅ Bank account selection UI feels native to Xero
- ✅ All navigation flows work (onboarding, manage, disable, payer flow)
- ✅ Loading and error states use XUI patterns (XUILoader, XUIBanner)
- ✅ Terminology matches PRD ("Pay by bank", "settlement account", "direct bank transfer")

---

## Questions for You

Before I start building, clarify:

1. **Tech stack preference**: 
   - Option A: Server-rendered .NET MVC views (cshtml) with minimal JS?
   - Option B: React SPA embedded in your existing .NET app?
   - Option C: Full standalone Next.js/React app?

2. **XUI access**: Do you have access to `@xero/xui` npm package, or should I mock the components?

3. **Scope priority**: Which flow is most critical for demo?
   - Merchant onboarding (4 steps)?
   - Settings management (enable/disable/change account)?
   - Payer flow (invoice → Akahu redirect)?

4. **API stubbing**: Should I stub the APIs with hardcoded data, or wire them to your existing A2APaymentsApp backend?

Let me know your answers and I'll start building! 🚀
