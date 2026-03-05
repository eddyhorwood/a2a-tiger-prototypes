# Prototype Configuration System

## Overview

The A2A Tiger prototype now includes a configuration landing page that lets you set up different merchant scenarios before testing onboarding flows. This replaces hardcoded mock data with a flexible state management system.

## How It Works

### Configuration Flow

1. **Landing Page** (`/`) - Configure prototype state
2. **Demo Page** (`/demo`) - Browse entry points with current config
3. **Onboarding Flows** - Experience flows based on configured state

### Configuration Dimensions

The prototype configuration system lets you customize six core dimensions:

| Dimension | Options | Impact |
|-----------|---------|--------|
| **Stripe Status** | None, Connected & Active, Connected (Low Use) | Affects whether Stripe competes with A2A, OPMM behavior |
| **Bank Accounts** | None (ineligible), Single, Multiple | Eligibility checks, settlement account selection UX |
| **A2A Onboarding Status** | Not Started, Partially Complete, Complete & Enabled, Complete & Disabled | Starting point in onboarding, available actions |
| **Flow Variant** | Balanced (multi-step), Aggressive (single-step) | Which onboarding wizard is used |
| **Business Type** | B2C High Volume, B2C Low Win-rate, B2B Focused | Merchant persona, target cohort identification |
| **Region Eligibility** | NZ Eligible, Ineligible | Feature availability, error state testing |

## Quick Start Presets

Click "Show Presets" on the configuration page to load common scenarios:

### Fresh Start
- No Stripe, single bank account, first-time setup
- **Use for:** Clean onboarding flow testing

### Stripe Competitor
- Stripe active, multiple accounts, low online win-rate (target cohort)
- **Use for:** Testing A2A alongside existing Stripe setup, OPMM behavior

### Needs Completion
- Started setup but didn't finish - resume flow
- **Use for:** Testing partially-complete state recovery

### Power User
- Fully configured, testing management & edge cases
- **Use for:** Settings management, disable/re-enable flows

### Ineligible Org
- No bank accounts or non-NZ - blocked state
- **Use for:** Error messaging, eligibility gating

### Fast Onboarding (Aggressive)
- Single-step flow variant
- **Use for:** Comparing balanced vs aggressive onboarding UX

## Technical Architecture

### Files

```
src/
├── types/
│   └── PrototypeConfig.ts          # Configuration types, presets, labels
├── config/
│   └── PrototypeConfigContext.tsx  # React context provider, hooks
├── pages/
│   ├── PrototypeSetup.tsx          # Configuration landing page UI
│   └── PrototypeSetup.css          # Styling
```

### Context Provider

The `PrototypeConfigProvider` wraps the entire app in `main.tsx`:

```tsx
<PrototypeConfigProvider>
  <App />
</PrototypeConfigProvider>
```

Configuration is automatically persisted to `localStorage` so it survives page refreshes during testing sessions.

### Using Configuration in Components

#### Basic Access

```tsx
import { usePrototypeConfig } from '../config/PrototypeConfigContext'

function MyComponent() {
  const { config, updateConfig } = usePrototypeConfig()
  
  // Access config values
  const hasStripe = config.stripe !== 'none'
  const isEligible = config.regionEligibility === 'eligible'
  
  // Update config (if needed)
  updateConfig({ a2aStatus: 'complete_enabled' })
}
```

#### Derived State Hook

For common checks, use `useConfigState()`:

```tsx
import { useConfigState } from '../config/PrototypeConfigContext'

function OnboardingWizard() {
  const {
    isEligible,          // Region eligible + has bank accounts
    hasStripe,           // Any Stripe connection
    isStripeActive,      // Stripe actively used
    hasBankAccounts,     // At least one bank account
    hasMultipleBankAccounts,
    isA2AStarted,        // Has begun onboarding
    isA2AComplete,       // Finished onboarding
    isA2AEnabled,        // Complete AND enabled
    needsCompletion,     // Partially complete state
    isAggressiveFlow,    // Using fast flow variant
    isTargetCohort,      // B2C low win-rate (A2A target)
    config               // Full config object
  } = useConfigState()
  
  if (!isEligible) {
    return <IneligibleMessage />
  }
  
  if (needsCompletion) {
    return <ResumeOnboarding />
  }
  
  // ... rest of logic
}
```

## Mock Data Integration

### Before (Hardcoded)

```tsx
// Old approach - fixed mock data
const mockBankAccounts = [
  { id: "acc-001", name: "ANZ Business Account" },
  { id: "acc-002", name: "Westpac Operating Account" }
]
```

### After (Configuration-Driven)

```tsx
// New approach - mock data adapts to config
import { useConfigState } from '../config/PrototypeConfigContext'
import { mockBankAccounts } from '../mocks/xeroOrgData'

function SettlementAccountSelector() {
  const { hasBankAccounts, hasMultipleBankAccounts } = useConfigState()
  
  if (!hasBankAccounts) {
    return <NoBankAccountsError />
  }
  
  const accounts = hasMultipleBankAccounts
    ? mockBankAccounts           // Show all accounts
    : [mockBankAccounts[0]]      // Show single account
  
  return <AccountDropdown accounts={accounts} />
}
```

## Extending the Configuration

To add new configuration dimensions:

1. **Add type to `PrototypeConfig.ts`:**
   ```typescript
   export type MyNewDimension = 'option1' | 'option2'
   
   export interface PrototypeConfig {
     // ... existing
     myNewDimension: MyNewDimension
   }
   ```

2. **Add labels:**
   ```typescript
   export const CONFIG_LABELS = {
     // ... existing
     myNewDimension: {
       option1: 'Option 1',
       option2: 'Option 2'
     }
   }
   ```

3. **Update presets** (if needed)

4. **Add UI in `PrototypeSetup.tsx`:**
   ```tsx
   <div className="x-config-group">
     <label className="x-config-group__label">My New Dimension</label>
     <p className="x-text-sm x-text-muted x-config-group__hint">
       What this dimension affects
     </p>
     <div className="x-config-group__options">
       {(Object.keys(CONFIG_LABELS.myNewDimension) as MyNewDimension[]).map(option => (
         <label key={option} className="x-config-option">
           <input
             type="radio"
             name="myNewDimension"
             value={option}
             checked={config.myNewDimension === option}
             onChange={() => updateConfig({ myNewDimension: option })}
           />
           <span className="x-config-option__label">
             {CONFIG_LABELS.myNewDimension[option]}
           </span>
         </label>
       ))}
     </div>
   </div>
   ```

5. **Add derived state helper** (optional) in `PrototypeConfigContext.tsx`

## Current Config Display

The demo landing page (`/demo`) shows a banner with the current configuration. Users can click "Change Configuration" to return to the setup page.

## Storage & Persistence

- Configuration is stored in `localStorage` under key `a2a-prototype-config`
- Persists across page refreshes and browser sessions
- Clear browser storage or use "Reset" to return to defaults

## Compliance Note

Configuration affects which flows are shown, but **does not bypass AML/CFT compliance requirements**. All flows must still use correct language around fund custody, regardless of configuration. See `compliance/AML_CFT_LLM_CONTEXT.md` for details.

## Related Documentation

- [Prototype PRD](../specifications/prototype-prd.md) - Feature scope
- [Flow Variants](../specifications/flow-variants.md) - Balanced vs Aggressive flows
- [Multi-Entry Implementation](../architecture/multi-entry-implementation.md) - Entry point system
- [Xero Payment Setup Flow](../reference/xero-payment-setup-flow.md) - Current Xero patterns

---

*Last updated: 2026-03-05*
