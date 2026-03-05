# Enhanced Entry Points Implementation

## Overview

This update adds realistic Xero-style entry points for the A2A bank payments prototype, focusing on two main flows:

1. **Settings-initiated flow** - Enhanced Payment Services landing page
2. **Invoice-initiated flow** - Contextual banner on invoice view

## What Was Added

### 1. SetupBanner Component (`/src/components/SetupBanner.tsx`)

Reusable banner component for contextual payment setup prompts.

**Features:**
- Three variants: `info` (blue), `promotional` (purple), `high-contrast` (yellow)
- Primary and secondary action buttons
- Dismissible functionality
- Icon support
- Responsive design

**Usage:**
```tsx
<SetupBanner
  variant="info"
  title="Get paid 2× faster with online payments"
  description="Add a 'Pay now' button to your invoices..."
  primaryAction={{ label: "Set up online payments", onClick: handleSetup }}
  secondaryAction={{ label: "Learn more", onClick: handleLearnMore }}
  onDismiss={handleDismiss}
/>
```

### 2. InvoiceView Page (`/src/pages/InvoiceView.tsx`)

New realistic invoice view/edit screen with contextual setup banner.

**Features:**
- Xero-style breadcrumbs navigation
- Contextual setup banner (appears when no payment service configured)
- Realistic invoice layout matching production Xero
- Contact display with avatar
- Line items table
- Totals calculation section
- Notes textarea
- Entry context tracking for banner click

**Route:** `/invoice-view/:invoiceId`

**Entry Point ID:** `banner` (tracks that user came from invoice banner)

### 3. Enhanced OnlinePaymentsSettings (`/src/pages/OnlinePaymentsSettings.tsx`)

Upgraded Payment Services landing page with realistic state management.

**New Features:**
- **Provider Status States:**
  - `NOT_CONFIGURED` - No relationship yet
  - `SETUP_STARTED` - Onboarding started but not completed
  - `SETUP_COMPLETE` - Fully configured and usable
  - `ERROR` - Action required
  
- **Dynamic CTAs based on state:**
  - `NOT_CONFIGURED` → "Get set up"
  - `SETUP_STARTED` → "Resume setup" + "Cancel"
  - `SETUP_COMPLETE` → "Manage" + "Disconnect"
  - `ERROR` → "Fix issues" + "Disconnect"

- **Status badges:**
  - Connected (green)
  - Setup started (yellow)
  - Action required (red)

- **Contextual banners:**
  - Success banner after completing setup
  - "Almost there!" banner for incomplete setups

- **Provider cards with:**
  - Service icon (bank/card/debit)
  - Payment method badges
  - Expandable features list (for NOT_CONFIGURED state)
  - Pricing and setup time information
  - Settlement and fee account display (when SETUP_COMPLETE)

**Three providers shown:**
1. **Pay by bank** (Powered by Akahu) - Fully functional
2. **Cards and digital wallets** (Powered by Stripe) - Demo placeholder
3. **Direct Debit** (Powered by GoCardless) - Demo placeholder

### 4. DemoLanding Page (`/src/pages/DemoLanding.tsx`)

New landing page that showcases all entry points.

**Purpose:**
- Overview of all available demo flows
- Links to each entry point with descriptions
- Feature lists for each flow
- Educational content about the prototype

**Route:** `/` (home)

## Routing Structure

```
/ → DemoLanding (landing page with links to all flows)
├── /settings/online-payments → OnlinePaymentsSettings (Entry Point 1)
├── /invoice-view/:id → InvoiceView (Entry Point 2 - NEW)
├── /invoice/:id → InvoiceDetail (Entry Point 3 - existing modal flow)
└── /merchant-onboarding → OnboardingWizardBalanced
```

## Entry Context Tracking

All entry points properly track their source using the `EntryContext` system:

| Entry Point | Source ID | Mode | Metadata |
|-------------|-----------|------|----------|
| Settings → Get set up | `settings` | `first_time` | `{ serviceId }` |
| Settings → Resume setup | `settings` | `resume` | `{ serviceId }` |
| Settings → Manage | `manage` | `manage` | `{ serviceId }` |
| Invoice banner | `banner` | `first_time` | `{ invoiceId, invoiceAmount, contactName }` |
| Invoice modal | `invoice.modal` | `first_time` | `{ invoiceId }` |

## Design System Compliance

All components follow Xero Design Guidelines:

- ✅ CSS variables for colors (`--xui-color-*`, `var(--text-*)`, etc.)
- ✅ 4px spacing grid (8px, 12px, 16px, 20px, 24px, 32px, 40px, 48px)
- ✅ XUI components (`XUIButton`, etc.)
- ✅ Xero typography scale
- ✅ Proper border radius (6px, 8px, 12px)
- ✅ Consistent box shadows
- ✅ Responsive breakpoints

## AML/CFT Compliance

All payment-related copy follows compliance guidelines:

- ✅ No "Xero transfers funds" language
- ✅ Clear provider attribution ("Powered by Akahu")
- ✅ No fund custody claims
- ✅ Proper role descriptions

## Testing the Flows

### Flow 1: Settings-Initiated Setup

1. Navigate to `/` or `/settings/online-payments`
2. Click "Get set up" on Pay by bank card
3. Complete onboarding wizard
4. Return to settings page (status should be `SETUP_COMPLETE`)
5. See "Connected" badge and settlement account details
6. Click "Manage" to modify settings

### Flow 2: Invoice Banner Setup

1. Navigate to `/invoice-view/INV-002`
2. See blue informational banner at top
3. Click "Set up online payments" (primary CTA)
4. Complete onboarding wizard
5. Return to invoice view
6. Banner should be hidden (payment service configured)

### Flow 3: Settings Resume Flow

To demo the "Setup started" state:

1. In `OnlinePaymentsSettings.tsx`, line 49, change:
   ```tsx
   status: 'SETUP_STARTED', // Change from NOT_CONFIGURED
   ```
2. Navigate to `/settings/online-payments`
3. See "Almost there!" banner at top
4. See "Resume setup" button on Pay by bank card
5. Click to continue onboarding

## File Structure

```
src/
├── components/
│   ├── SetupBanner.tsx (147 lines)
│   └── SetupBanner.css (155 lines)
├── pages/
│   ├── DemoLanding.tsx (149 lines)
│   ├── DemoLanding.css (172 lines)
│   ├── InvoiceView.tsx (280 lines)
│   ├── InvoiceView.css (362 lines)
│   ├── OnlinePaymentsSettings.tsx (289 lines)
│   └── OnlinePaymentsSettings.css (291 lines)
└── App.tsx (updated routing)
```

## Next Steps

To further enhance realism:

1. **Add more entry points:**
   - Dashboard invoice widget with banner
   - Invoice list view with inline banners
   - Repeating invoices page with setup prompt
   - Quote view with promotional banner

2. **Enhance provider onboarding:**
   - Simulate Stripe/GoCardless flows
   - Add verification states
   - Implement resume points within onboarding

3. **Add more states:**
   - KYC/KYB verification pending
   - Payout account pending
   - Suspended account
   - Fee dispute states

4. **Improve animations:**
   - Banner slide-in transitions
   - Status badge updates
   - Card hover effects
   - Loading states

## References

- [PAYMENT_ONBOARDING_ENTRY_POINTS.md](../docs/reference/payment-onboarding-entry-points.md) - Real Xero entry points catalog
- [XERO_DESIGN_GUIDELINES.md](../XERO_DESIGN_GUIDELINES.md) - Design system rules
- [AML_CFT_LLM_CONTEXT.md](../compliance/AML_CFT_LLM_CONTEXT.md) - Compliance guardrails (§4.1 for canonical language)
- [MULTI_ENTRY_IMPLEMENTATION.md](../docs/architecture/multi-entry-implementation.md) - Entry context system architecture
