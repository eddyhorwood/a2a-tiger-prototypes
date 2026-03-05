# A2A Onboarding Prototype - PRD v2

## Purpose
**Stakeholder workshop tool** to align product, legal, and engineering on the compliance/friction tradeoff spectrum for A2A merchant onboarding.

**Not** for user testing or pilot. Goal is 75% production fidelity that lets the team interact with, critique, and give feedback on different implementation approaches.

**Core tension**: Legal requires specific disclosures (Bell Gully AML/CFT constraints), but users abandon heavy onboarding flows. Team needs to find the minimum friction Legal will approve.

## Context

### ERD Insights
- **Problem**: Current payment onboarding is leaky, non-native, causes drop-off
- **Hypothesis**: Native, shorter, clearer flow → more merchants enable payment methods
- **A2A advantage**: Can leverage Xero data (bank accounts, org context), use Xero Identity instead of separate OAuth

### One Onboarding Alignment (FY27 Initiative)
**What it is**: Single, provider-agnostic onboarding capability for all Xero payments (cards, A2A, bill pay). Abstracts provider specifics behind common API + consistent UX patterns.

**Core patterns we must follow**:
- **1-3 setup screens max** (not 6+ steps)
- **Left-side stepper** showing progress (Business details → Personal details → Review & submit)
- **Heavy pre-filling** from Xero org data (name, address, contact)
- **"Save and exit"** capability to resume later
- **Single-purpose screens** with clear headings
- **Minimal friction**: One config choice per screen, not forms with 5+ fields

**Entry point philosophy**: "As many entry points as make sense, one underlying onboarding service"
- Dashboard widgets, settings tiles, invoice modals,banners, guided setup tasks
- All funnel to the same flow (not separate implementations)

**Why this matters**: Safer A2A must be designed as **one provider** plugged into One Onboarding, not a standalone custom flow. Prototype must validate patterns that work within this architecture.

## Success Criteria (Workshop Context)
**Team can answer**:
- Which flow best balances legal requirements vs conversion optimization?
- Does legal messaging feel sufficient to Bell Gully without killing momentum?
- Does pre-filling feel helpful or intrusive?
- Which approach should we pilot first?

**Specific debates to resolve**:
- Aggressive flow (2 screens): Too risky legally?
- Safe flow (4+ screens with full explainers): Too slow for invoice capture?
- Balanced flow (3 screens): Good enough for both?

## Legal/AML Constraints (Bell Gully)
- Must NOT represent Xero/Akahu as holding/transferring/managing funds
- Must show clear enablement explanation + confirmation for merchants
- Must show clear pre-redirect explanation for payers
- Copy must emphasize: direct bank-to-bank, Akahu as initiator, bank as executor

## Open Questions for Workshop
1. **PII/pre-fill level**: How much can we pre-fill from Xero org data without feeling intrusive?
2. **Legal placement**: Upfront explainer vs post-OAuth vs just checkbox?
3. **CTA language**: "Enable Pay by bank" vs "Accept bank transfers" vs "Connect direct bank payments"
4. **Entry point differences**: Should invoice→fast, settings→detailed be different flows?
5. **Settlement account UX**: How do we surface the right account when orgs have 5+ bank accounts?

## Tweakable/High-Fidelity Areas

1. **Settlement account selection** (CRITICAL)
   - Mock chart of accounts with realistic bank account structure
   - Account display: Name + code + masked number
   - Edge cases: No accounts, single account (pre-select), multiple accounts
   - "Add new account" stub option
   
2. **Legal consent language** (config-driven)
   - Extracted to `src/config/onboardingContent.ts` for easy editing
   - Bell Gully requirements baked into defaults
   - Team can edit without touching component code

3. **Pre-fill vs manual entry** (org data)
   - Mock Xero org data: Business name, address, contact
   - Test full pre-fill vs asking users to type
   
4. **CTA language** (config-driven)
   - Buttons, headers, help text all in config file
   - Easy A/B variant testing

## Flow Spectrum: Aggressive → Balanced → Safe

**These are NOT A/B test variants**. They represent different implementation options with a range of aggressiveness, showing the team the full compliance/friction tradeoff space.

| Dimension | Aggressive | Balanced | Safe |
|-----------|-----------|----------|------|
| **Screens** | 2 screens | 3 screens | 4+ screens |
| **Pre-fill** | Auto-fill everything, "Looks right?" | Pre-fill some, ask to confirm | Ask user to type everything |
| **Legal copy** | Minimal footer text | Short inline explanations | Full explainer screens |
| **Consent** | Implied / checkbox at end | Single explicit consent step | Multiple consent checkpoints |
| **Tone** | "We've got this, click to continue" | "Here's what we need" | "Here's why + what it means" |

**One Onboarding constraint**: All flows must be 1-3 screens to match existing payment onboarding patterns (screenshots show 3-step flows).

**Workshop value**: 
- Aggressive shows dream UX (might be legally insufficient)
- Safe shows legally bulletproof (might kill conversion)
- Balanced is the negotiation starting point

---

## Tonight's Scope: Balanced Flow + Switcher

**Deliverable**: Single complete Balanced flow from invoice entry, aligned with One Onboarding patterns.

**Includes**:
- One Onboarding-style stepper (left sidebar, 3 steps)
- Mock CoA with realistic bank accounts
- Config-driven legal copy + CTA language
- Full settlement account logic (edge cases, pre-selection)
- Mock Akahu OAuth stub
- "Save and exit" capability

**Out of scope tonight**:
- Building Aggressive + Safe variants (clone Balanced later)
- Settings entry point variations
- Switcher page (can add if time permits)

---

## Flow B: "Balanced" (BUILD THIS - One Onboarding Aligned)

**Hypothesis**: Balance legal clarity with conversion optimization, following One Onboarding patterns

**One Onboarding compliance**:
- 3 main screens (matches pattern: checklist → config → review)
- Left stepper showing progress
- Pre-filled from mock Xero orgdata
- Single-purpose screens
- "Save and exit" on every screen

### Screen 1: "A checklist before you start"

**Pattern**: Matches One Onboarding "What to expect" entry screens (see screenshots)

**Layout**:
- Modal/full-page with title "Set up online payments"
- Main heading: "A checklist before you start"
- Subheading: "Select your business structure to see what documents you'll need"
- Dropdown: "Business type" (Individual or sole trader, etc.) - for A2A, we might not need this or pre-fill it
- Section: "What to expect"
- Time estimate: "It will take about 2-3 minutes to complete if you have this information ready"

**Checklist with icons** (blue illustrations like screenshots):
- 📊 **Settlement account** - "Choose where customer payments will be deposited (from your Xero chart of accounts)"
- 🔐 **Connect to Akahu** - "Secure provider for direct bank-to-bank payments" 
- ✅ **Review and enable** - "Understand how Pay by bank works and complete setup"

**Footer**:
- 🔒 "We protect your data" with info icon
- Small text: "Xero takes a defence-in-depth approach to protecting our systems and your data. [Learn more about security at Xero]"

**CTAs**:
- Primary: "Start now" (bright blue button)
- No secondary (can't save yet, nothing filled)

---

### Screen 2: "Choose settlement account"

**Pattern**: Single-purpose config screen with left stepper

**Layout**:
- Left sidebar stepper:
  - Step 1: "Business details" ← YOU ARE HERE (blue circle, filled)
  - Step 2: "Review & submit" (grey circle, unfilled)
- Main heading: "Choose settlement account"
- Top right: "Save and exit" link

**Pre-context**:
- Org badge at top: "Acme Consulting Ltd" (pill-style, from mock org data)

**Main content**:
- **Dropdown/Select** (CRITICAL UX AREA):
  - Label: "Settlement account"
  - Help text below: "Where should customer payments be deposited?"
  - Options format: `"ANZ Business Account (090) •••• 1234"`
  - If 0 accounts: Show disabled state with message: "You need at least one bank account in your chart of accounts to continue"
  - If 1 account: Pre-select automatically, show selection
  - If 2+ accounts: Show dropdown with all options
  - Bottom of list: "+ Add new bank account..." (stub, shows non-functional message on click)

**Legal footer** (small grey text, bottom of screen):
- "Xero and Akahu don't hold funds. Payments are direct bank-to-bank transfers. [Read more]"

**CTAs**:
- Primary: "Continue" (disabled if no account selected)
- Secondary: "Back" (ghost button, goes to Screen 1)
- Top right link: "Save and exit"

---

### Screen 3: "Review and connect"

**Pattern**: One Onboarding review/consent screen

**Layout**:
- Left sidebar stepper:
  - Step 1: "Business details" (completed, checkmark)
  - Step 2: "Review & submit" ← YOU ARE HERE (blue circle, filled)
- Main heading: "Review and connect to Akahu"
- Top right: "Save and exit" link

**Summary card** (light blue background, rounded):
- "Settlement account"
- "ANZ Business Account (090)"
- [Edit] link (goes back to Screen 2)

**Explainer section**:
- Subheading: "How Pay by bank works"
- Icon/illustration (optional for v1)
- 3-step text:
  1. "Customers choose their bank and approve the payment"
  2. "Akahu securely initiates the direct bank transfer"
  3. "Funds go directly to your bank account"
- Emphasized line: "**Xero and Akahu never hold your money.**"

**Consent**:
- Checkbox: "I understand that payments are direct bank-to-bank transfers. Xero and Akahu do not hold funds."
- Link below: "View full terms and conditions" (modal stub with legal text)

**CTAs**:
- Primary: "Connect to Akahu" (disabled until checkbox checked, bright blue)
- Secondary: "Back" (goes to Screen 2)
- Top right link: "Save and exit"

---

### Screen 4: "Akahu OAuth" (Mock external flow)

**Pattern**: Briefly leaves One Onboarding UI (new window/redirect simulation)

**Layout**:
- Akahu branding header
- Title: "Authorize Xero to initiate bank payments"
- Text: "You will be redirected to securely connect your bank account"
- **Bank selection dropdown**:
  - Label: "Choose your bank"
  - Options: ANZ, Westpac, BNZ, ASB, Kiwibank
- Permission list (optional for v1):
  - "Initiate payments on your behalf"
- Primary button: "Approve"

**Behavior**:
- On click "Approve": Show loading state ("Authorizing...") for 2 seconds
- Auto-return to Screen 5 (Success)

---

### Screen 5: "Pay by bank is now enabled" (Success)

**Pattern**: One Onboarding confirmation screen

**Layout**:
- Left sidebar stepper:
  - Step 1: "Business details" (completed, checkmark)
  - Step 2: "Review & submit" (completed, checkmark)
- Success icon (green checkmark circle, large)
- Main heading: "Pay by bank is now enabled"
- Confirmation text: "You're all set to accept direct bank payments from your customers."

**What happens next** (bullet list):
- "Customers will see 'Pay by bank' as a payment option on invoices you send"
- "Payments typically clear in 1-2 business days"
- "You can manage your settlement account in Online payments settings"

**CTAs**:
- Primary: "Go to Online payments settings" (navigates to /settings/online-payments)
- Secondary: "Back to invoice" (if entered from invoice, uses returnTo param)

---

## Config Extraction

All copy, legal text, and CTA labels extracted to `src/config/onboardingContent.ts`:

```typescript
export const balancedFlowContent = {
  screen1: {
    modalTitle: "Set up online payments",
    heading: "A checklist before you start",
    timeEstimate: "It will take about 2-3 minutes to complete if you have this information ready",
    checklist: [
      {
        icon: "📊",
        title: "Settlement account",
        description: "Choose where customer payments will be deposited (from your Xero chart of accounts)"
      },
      {
        icon: "🔐",
        title: "Connect to Akahu",
        description: "Secure provider for direct bank-to-bank payments"
      },
      {
        icon: "✅",
        title: "Review and enable",
        description: "Understand how Pay by bank works and complete setup"
      }
    ],
    dataProtectionText: "Xero takes a defence-in-depth approach to protecting our systems and your data.",
    cta: "Start now"
  },
  screen2: {
    heading: "Choose settlement account",
    label: "Settlement account",
    helpText: "Where should customer payments be deposited?",
    noAccountsMessage: "You need at least one bank account in your chart of accounts to continue",
    addNewAccountText: "+ Add new bank account...",
    legalFooter: "Xero and Akahu don't hold funds. Payments are direct bank-to-bank transfers.",
    cta: "Continue"
  },
  screen3: {
    heading: "Review and connect to Akahu",
    summaryLabel: "Settlement account",
    explainerHeading: "How Pay by bank works",
    explainerSteps: [
      "Customers choose their bank and approve the payment",
      "Akahu securely initiates the direct bank transfer",
      "Funds go directly to your bank account"
    ],
    explainerEmphasis: "Xero and Akahu never hold your money.",
    consentCheckbox: "I understand that payments are direct bank-to-bank transfers. Xero and Akahu do not hold funds.",
    termsLink: "View full terms and conditions",
    cta: "Connect to Akahu"
  },
  screen5: {
    heading: "Pay by bank is now enabled",
    confirmation: "You're all set to accept direct bank payments from your customers.",
    whatNext: [
      "Customers will see 'Pay by bank' as a payment option on invoices you send",
      "Payments typically clear in 1-2 business days",
      "You can manage your settlement account in Online payments settings"
    ],
    cta: "Go to Online payments settings",
    ctaSecondary: "Back to invoice"
  }
}
```

---

## Mock Data

### Settlement Accounts (Chart of Accounts)

```typescript
// src/mocks/xeroOrgData.ts

export interface BankAccount {
  id: string
  name: string
  code: string
  type: 'BANK' | 'CREDITCARD' | 'CURRENT'
  isEnablePayments: boolean
  bankAccountNumberMasked: string
}

export const mockBankAccounts: BankAccount[] = [
  {
    id: "acc-001",
    name: "ANZ Business Account",
    code: "090",
    type: "BANK",
    isEnablePayments: true,
    bankAccountNumberMasked: "•••• 1234"
  },
  {
    id: "acc-002",
    name: "Westpac Operating Account",
    code: "545",
    type: "BANK",
    isEnablePayments: true,
    bankAccountNumberMasked: "•••• 5678"
  },
  {
    id: "acc-003",
    name: "Savings Account",
    code: "212",
    type: "BANK",
    isEnablePayments: false, // Edge case: not enabled for payments
    bankAccountNumberMasked: "•••• 9012"
  }
]

// Filter function for eligible accounts
export const getEligibleSettlementAccounts = () => {
  return mockBankAccounts.filter(acc => 
    acc.type === 'BANK' && acc.isEnablePayments
  )
}
```

### Xero Org Data (Pre-fill)

```typescript
export interface XeroOrg {
  organisationID: string
  organisationName: string
  contactName: string
  emailAddress: string
  address: string
}

export const mockXeroOrg: XeroOrg = {
  organisationID: "org-12345",
  organisationName: "Acme Consulting Ltd",
  contactName: "Jordan Smith",
  emailAddress: "jordan@acmeconsulting.co.nz",
  address: "123 Queen Street, Auckland 1010"
}
```

---

## Implementation Plan

### Tonight (Phase 1):
1. ✅ Create config file: `src/config/onboardingContent.ts`
2. ✅ Create mock data: `src/mocks/xeroOrgData.ts`
3. ✅ Update OnboardingWizard.tsx to use One Onboarding 3-screen pattern
   - Screen 1: Checklist
   - Screen 2: Settlement account selection
   - Screen 3: Review + consent
   - Screen 4: Mock Akahu OAuth
   - Screen 5: Success
4. ✅ Add left-side stepper component
5. ✅ Implement "Save and exit" capability (optional if time short)

### Later (Phase 2):
- Clone Balanced → Aggressive (combine screens 2+3, minimal legal)
- Clone Balanced → Safe (add explainer screen between 1 and 2)
- Add prototype switcher page if valuable for workshop
- Add Settings entry point variant flows

---

## Next Steps After Workshop

1. Get team feedback: Which flow feels right?
2. Get Legal sign-off: Which flow meets Bell Gully requirements?
3. Decision: Which variant(s) to pilot with real merchants?
4. If piloting: Consider building real Xero OAuth app for actual org data

---

## References

- ERD: `[DRAFT] High-level ERD_ NZ A2A Safe Payments`
- Bell Gully legal constraints (AML/CFT, custody language)
- One Onboarding Figma screenshots (checklist, stepper, multi-step patterns)
- Current entry points: `/settings/online-payments`, `/invoice/INV-001`
- One Onboarding initiative (FY27 alignment)
