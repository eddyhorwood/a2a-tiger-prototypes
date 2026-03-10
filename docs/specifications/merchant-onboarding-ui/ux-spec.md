# Pay by Bank: Merchant Onboarding UI/UX Spec

**Version:** 0.2 (Draft)
**Date:** 10 March 2026
**Status:** Draft, updated with PM alignment on key UX/product questions; remaining unresolved items flagged with [TBC]
**Audience:** Engineering teams implementing A2A merchant onboarding, AI coding agents supervised by engineers
**Prototype reference:** `A2APaymentsApp/ClientApp/src/pages/OnboardingWizardAggressiveFast.tsx`

---

## 1. Problem and Opportunity

Xero merchants in New Zealand need a way to accept direct bank-to-bank payments on invoices. Current payment onboarding flows (Stripe, GoCardless) have high drop-off rates because they require merchants to leave Xero, enter redundant information, and navigate multi-step provider-specific flows.

This onboarding flow eliminates that friction. It uses data Xero already holds (org details, chart of accounts, bank accounts) to compress onboarding into a single modal interaction, targeting sub-10-second completion.

The business case: higher conversion from "sees the option" to "starts accepting bank payments," particularly for the invoice entry point where merchants are already thinking about getting paid.

---

## 2. Scope

### In scope (this spec)

- Single-modal merchant onboarding flow for Pay by bank
- Settlement account selection from Xero chart of accounts
- AML/CFT compliant disclosure copy
- Entry from invoice view (banner and OPMM modal) and Online Payments settings
- Success state and return-to-origin behaviour
- Error and edge case handling

### Out of scope

- Backend API design, data model, and Akahu integration (covered in separate **Akahu A2A Merchant Onboarding API Spec**)
- Balanced (multi-step) and conservative (full KYC) flow variants
- Payer-side payment experience (covered in `payment-execution-pattern.md`)
- Webhook processing and invoice status updates
- Pricing, billing, and App Store subscription
- Mobile (XAA) entry points
- Markets outside New Zealand

---

## 3. Actors

| Actor | Role | AML/CFT boundaries |
|---|---|---|
| Merchant | Xero customer enabling Pay by bank on their invoices | Receives funds directly into their nominated NZ bank account |
| Xero | Software provider. Collects payment instructions, updates the ledger | Never holds, pools, or manages customer funds |
| Akahu | Accredited requestor under CDPA 2025. Initiates payments via NZ bank APIs | Initiates payments only. Does not hold funds |
| Banks | Executing financial institutions. Authenticate payers, execute transfers | Hold funds, execute transfers, maintain records |

---

## 4. Prerequisites

Before a merchant can enter the onboarding flow:

| Prerequisite | Source | Validation |
|---|---|---|
| Authenticated Xero session | Xero identity / native auth | Active session; backend has token with `paymentservices` capability |
| NZ-based organisation | Xero Organisation API (`CountryCode`) | `CountryCode === "NZ"` |
| At least one eligible bank account | Xero Accounts API | At least one account where `Type === BANK` and `EnablePaymentsToAccount === true` |
| User has bank-feed authorisation permissions | Xero identity / permissions + bank feed authorisation model | Direction agreed: users who can view bank-feed authorised account context should be eligible; preferred gate is users who can edit bank feed/payment settings. Final Xero role-name mapping remains an implementation follow-up |
| Pay by bank not already enabled | Internal state (see backend API spec) | `A2AOnboardingStatus !== 'complete_enabled'` |

---

## 5. User Flow

### 5.1 Flow overview

```
Entry point → Onboarding modal (single screen) → Enable → [Backend setup] → Success → Return to origin
```

The entire merchant-facing interaction is a single XUI modal. No page navigation, no multi-step wizard, no redirect to a third-party site.

### 5.2 Entry points

Two entry points are in scope for v1. Both open the same modal.

#### 5.2.1 Invoice view entry

**Trigger:** Merchant is viewing or editing an invoice. Pay by bank is not enabled for the org.

**Surfaces:**
- **SetupBanner** at top of invoice view. Copy: "Get paid faster with direct bank payments." CTA: "Set up online payments."
- **OPMM button** in invoice form body. CTA: "Set up online payments." Opens an intermediate info modal (payment service details, bank logos, feature list) with secondary CTA: "Get set up with Akahu."

**Required context for the onboarding flow:**

| Context | Value | Purpose |
|---|---|---|
| Origin surface | Invoice (banner or OPMM modal) | Analytics attribution; determines return destination |
| Flow mode | First-time setup | Distinguishes new setup from editing existing config |
| Return destination | The current invoice | Where to navigate after completion or dismissal |
| Invoice ID | Current invoice ID | Analytics; potential future invoice-specific behaviour |

**Behaviour:** The onboarding modal opens as an overlay on the invoice page. On completion or dismissal, the merchant returns to the invoice.

#### 5.2.2 Online Payments Settings entry

**Trigger:** Merchant navigates to Settings > Online Payments. Pay by bank tile shows status "Not configured."

**Surface:** Pay by bank service tile with "Enable" CTA.

**Required context for the onboarding flow:**

| Context | Value | Purpose |
|---|---|---|
| Origin surface | Online Payments Settings | Analytics attribution; determines return destination |
| Flow mode | First-time setup | Distinguishes new setup from editing existing config |
| Return destination | Online Payments Settings page | Where to navigate after completion or dismissal |

**Behaviour:** Same modal. On completion, the settings page refreshes to show Pay by bank as enabled.

### 5.3 Onboarding modal (primary screen)

**Container:** XUI Modal, size `large`, dismissible via close button or Cancel.

**Layout (top to bottom):**

#### Header

| Element | Content | Token/style |
|---|---|---|
| Title | "Enable Pay by Bank" | `font-size: 28px; font-weight: 600; color: var(--text-heading)` |
| Subtitle | "powered by Akahu" | `font-size: 14px; color: var(--text-muted)` |
| Intro copy | "Pay by bank lets your customers pay you via direct bank transfer, initiated by Akahu. Funds move directly between bank accounts; Xero and Akahu do not hold or manage your customers' money." | `font-size: 15px; color: var(--text-body)` |

#### Value proposition list

Four items, each with a checkmark icon (green, `var(--xero-green-success)`):

1. "Instant bank-to-bank transfers from major NZ banks"
2. "Lower fees than traditional card payments"
3. "No setup fees or monthly costs"
4. "Automatic reconciliation in Xero"

#### Settlement account selector

| Element | Detail |
|---|---|
| Label | "Settlement account" |
| Description | "Customer payments will be transferred directly to this account" |
| Component | `XUISelectBox` with `XUISelectBoxOption` children |
| Options | All bank accounts from the merchant's Xero chart of accounts where `Type === BANK` and `EnablePaymentsToAccount === true` |
| Display format | `{Account Name} - {Full Bank Account Number}` (e.g. "ANZ Business Account - 06-0123-0456789-00") |
| Default selection | None. The dropdown shows "Select an account..." until the merchant explicitly chooses |
| Single account edge case | If only one eligible account exists, pre-select it |

#### AML/CFT disclosure

| Element | Detail |
|---|---|
| Container | Grey box (`background-color: var(--xero-grey-100); border: 1px solid var(--border-strong)`) |
| Copy | "Funds move directly between bank accounts; Xero and Akahu do not hold or manage your customers' money." |
| Helper text | "Xero records payments against your invoices once your bank confirms the transfer." |
| Learn more | Inline text button "Learn more" opens a secondary modal (see 5.4) |

#### Actions

| Button | Variant | Behaviour |
|---|---|---|
| Cancel | `standard` | Closes modal. No state change. Returns to origin |
| Enable | `main` | Disabled until a settlement account is selected and consent checkbox is checked. On click, triggers backend enablement (see section 6). Shows "Enabling..." during processing |

#### Consent checkbox

| Element | Detail |
|---|---|
| Component | `XUICheckbox` |
| Label | "I understand that Pay by bank uses direct bank transfers between bank accounts, and that Xero and Akahu do not hold or manage customer funds." |
| Required | Yes. Enable button is disabled until checked |

### 5.4 AML disclaimer detail modal

**Trigger:** Merchant clicks "Learn more" on the AML disclosure.

**Container:** XUI Modal, size `small`, overlays the onboarding modal.

**Content:**

Title: "How Pay by bank works"

Body:
> Akahu is our open banking partner. Your customers authorise payments in their own bank app, and their bank executes the transfer.
>
> When your customer pays an invoice:
>
> 1. They click "Pay by bank" on their invoice
> 2. They are redirected to Akahu to initiate a direct bank transfer
> 3. They authorise the payment in their bank app
> 4. Their bank executes the transfer directly to your settlement account
> 5. Xero records the payment against your invoice once your bank confirms the transfer
>
> **Important:** Funds move directly between bank accounts; Xero and Akahu do not hold or manage your customers' money.

Dismissible via close button. Returns to onboarding modal.

### 5.5 Empty state (no eligible accounts)

**Trigger:** Merchant has no bank accounts with `Type === BANK` and `EnablePaymentsToAccount === true`.

**Container:** XUI Modal, size `small`.

**Content:**

Title: "No eligible bank accounts"
Body: "You need to add a New Zealand bank account to Xero before setting up Pay by bank."
CTA: "Go back" (closes modal, returns to origin).

Decision: no deep link in v1 beta. The rollout targets orgs that already have bank-feed-authorised accounts that look like valid NZ account numbers. Keep the settlement account editable before save in the main flow.

---

## 6. Backend Integration

Backend API design (endpoints, data model, Akahu registration, branding theme attachment) is covered in the separate **Akahu A2A Merchant Onboarding API Spec**. This section describes only the UI-side behaviour during enablement.

### 6.1 UI sequence on "Enable" click

1. Button label changes to "Enabling...", spinner shown, button and form inputs disabled (`isProcessing` state)
2. Frontend calls the enablement endpoint (see backend API spec)
3. On **success**: modal transitions to an in-product confirmation state with a context-aware end-of-task CTA (for example, "View invoice" from invoice entry, or "Back to Online Payments" from settings entry). After CTA, user returns to the originating surface
4. On **error**: modal stays open, error message displayed (see Section 8), retry button shown
5. On **user closes modal mid-processing**: in-flight request is cancelled, no partial state persisted. Merchant can restart from the same entry point

---

## 7. Flow Context Requirements

The onboarding flow must receive the following context from its caller, regardless of how that context is passed (URL parameters, component props, internal state, etc.):

| Context | Required | Description |
|---|---|---|
| Origin surface | Yes | Which surface launched the flow (e.g. invoice view, settings page). Used for analytics and return navigation |
| Flow mode | Yes | Whether this is a first-time setup or managing an existing configuration |
| Return destination | No | Where to navigate after completion. Defaults to the originating surface |
| Invoice ID | No | The invoice being viewed, if the flow was launched from an invoice |
| Campaign ID | No | Attribution identifier if the flow was launched from a marketing campaign or deep link |

---

## 8. Error States

| Scenario | User experience | Recovery |
|---|---|---|
| No eligible bank accounts | Empty state modal: "No eligible bank accounts... You need to add a New Zealand bank account to Xero." CTA: "Go back" | Merchant adds a bank account in Xero, then re-enters the flow |
| Non-NZ organisation | Hide Pay by bank entry points entirely (same pattern as other region-specific features). No ineligibility message in v1 | N/A. Feature is NZ-only |
| Enablement API failure | Show error state in modal. Copy: "Something went wrong. Please try again." Retry button | Retry. If persistent, show "Contact support" link |
| Session expired mid-flow | Redirect to Xero login, then return to flow | Re-authenticate and resume |
| Bank slot conflict | [TBC] Depends on backend slot conflict resolution (see backend API spec) | [TBC] |
| Concurrent setup | [TBC] Last-write-wins? Optimistic locking? | [TBC] |
| User closes modal mid-processing | Cancel the in-flight request. No partial state persisted. Merchant can restart | Re-enter the flow from the same entry point |

---

## 9. Post-Enablement Behaviour

### 9.1 Immediate (same session)

| Behaviour | Detail |
|---|---|
| Modal closes | On successful enablement, transition to confirmation state first, then close after user action |
| Navigation | Context-aware confirmation CTA returns the merchant to the originating surface (invoice view or settings page) |
| Settings page | If entry was from settings: Pay by bank tile updates to show `SETUP_COMPLETE` status with settlement account display |
| Invoice page | If entry was from invoice: Pay by bank availability reflects branding theme attachment after setup. If another payment service already occupies the relevant slot, merchant may need manual activation on the branding theme |

### 9.2 Downstream effects

| Effect | Detail | Status |
|---|---|---|
| New invoices | Invoices created after enablement include Pay by bank as a payment option (assuming branding theme attachment) | Depends on backend branding theme behaviour (see backend API spec) |
| Existing draft invoices | Any invoice (including existing drafts) that uses a branding theme with the custom payment service attached now has Pay by bank available. If another payment service is already active on that branding theme (for example Stripe), merchant must manually activate/swap as required | Aligned (10 Mar 2026), subject to branding theme slot logic |
| Payer experience | Online invoices display a "Pay by bank" button. Clicking initiates the Akahu payment flow | Covered in separate spec (payment-execution-pattern.md) |
| Auto-reconciliation | Payments include invoice reference in bank statement, enabling one-to-one matching | Covered in payment-execution-pattern.md |
| Notifications | Show an in-product confirmation screen after enablement, with configurable copy and a context-relevant end-of-task CTA. Email confirmation is not required for v1 | Aligned for v1 |

---

## 10. Manage / Edit Flow

After initial enablement, the merchant may need to:

- **Change settlement account:** Re-open the modal from Online Payments Settings (Pay by bank tile > "Change settlement account"). This entry point is required so merchants can update payout account details after setup. Same modal, pre-populated with current account. The flow mode should be "manage" (not first-time setup). Copy on the manage screen: "You can change the bank account where you receive Pay by bank transfers at any time. Changes only affect future payments." Helper text: "Pay by bank deposits will be sent directly to this bank account. Xero and Akahu do not hold or manage your customers' money."
- **Disable Pay by bank:** Follow the existing Custom URL provider disable pattern in Online Payments settings. Merchant confirms disable action, and the payment service is removed/deactivated from branding themes per current provider behaviour.

---

## 11. Analytics Events

| Event | Trigger | Properties |
|---|---|---|
| `onboarding.modal_opened` | Onboarding modal rendered | `source`, `mode`, `invoiceId` (if applicable) |
| `onboarding.account_selected` | Merchant selects a settlement account | `source`, `accountId` (hashed) |
| `onboarding.learn_more_opened` | "Learn more" AML disclaimer clicked | `source` |
| `onboarding.enable_clicked` | Enable button clicked | `source`, `accountId` (hashed) |
| `onboarding.enable_succeeded` | Backend returns success | `source`, `durationMs` |
| `onboarding.enable_failed` | Backend returns error | `source`, `errorType` |
| `onboarding.modal_dismissed` | Modal closed without enabling | `source`, `dismissMethod` (close button, cancel, escape) |
| `onboarding.empty_state_shown` | No eligible accounts modal rendered | `source` |

---

## 12. Compliance Copy Validation Checklist

All copy in this flow must comply with [AML_CFT_LLM_CONTEXT.md](../../compliance/AML_CFT_LLM_CONTEXT.md) and the approved legal phrase set.

### 12.1 Assertions (must hold for any copy change)

- [ ] Xero is never described as transferring, holding, or managing funds
- [ ] Akahu is never described as transferring money (only "initiating" transfers)
- [ ] Banks are identified as the entities that execute transfers
- [ ] The phrase "direct bank transfer" or "direct bank-to-bank" is used when describing the payment path
- [ ] No copy implies an intermediary settlement account
- [ ] The phrase "powered by Akahu" or "initiated by Akahu" is used (not "by Akahu" alone)

### 12.2 Banned phrases (must never appear in product copy)

- "Bank transfer by Xero/Akahu."
- "Payment processed by Xero/Akahu."
- "Xero/Akahu transfers the money to your account."
- "Funds held by Xero / held in your Xero account."
- "Funds held by Akahu."
- "Akahu processes payments on your behalf."
- "Xero receives your customers' funds and passes them to you."
- "Funds are held in your Xero account until they are paid out."
- "Pay now with Xero."
- "Pay via Xero bank transfer."
- "Pay via Xero account."
- "We'll process your payment through Xero."
- "Akahu will move the money to your supplier's account."
- "Your funds will be held by Xero until the invoice is settled."
- "Xero has received your funds."
- "Your payment has been processed and settled by Xero/Akahu."
- "Your money is now held safely by Xero."
- "Payment confirmed by Xero."
- "Moving your settlement account will move existing funds held by Xero."
- "Xero will transfer your balance from the old settlement account to the new one."

### 12.3 Approved reference phrases

- "Pay securely via direct bank transfer, initiated by Akahu."
- "Funds move directly between bank accounts; Xero and Akahu do not hold or manage your customers' money."
- "Funds move directly between bank accounts; Xero and Akahu do not hold or manage your money."
- "This payment is initiated via Akahu, and executed by your bank."
- "Payment received via bank transfer (initiated via Akahu)."
- "Xero records the payment against your invoice once your bank confirms the transfer."
- "Akahu is our open banking partner. Your customers authorise payments in their own bank app, and their bank executes the transfer."

**Legal review status:** Legal phrase set provided and incorporated. Copy in this spec uses approved phrases. Bell Gully review of final in-product copy is pending.

---

## 13. Design and Component Requirements

### 13.1 Components used

| Component | Import | Key props |
|---|---|---|
| `XUIModal` | `@xero/xui/react/modal` | `id`, `isOpen`, `size` ("large" for main, "small" for AML detail and empty state), `closeButtonLabel`, `onClose` |
| `XUIModalBody` | `@xero/xui/react/modal` | Children only |
| `XUIButton` | `@xero/xui/react/button` | `variant` ("main" for Enable, "standard" for Cancel), `isDisabled`, `onClick` |
| `XUISelectBox` | `@xero/xui/react/selectbox` | `id`, `label`, `buttonContent` |
| `XUISelectBoxOption` | `@xero/xui/react/selectbox` | `id`, `value`, `isSelected`, `onSelect` |

### 13.2 Layout rules

- All spacing on 4px grid (8px, 12px, 16px, 20px, 24px, 32px)
- Colours via CSS custom properties only (never hardcoded hex)
- Font family: `var(--xui-font-family-sans)`
- Heading: 28px/600 for modal title
- Body text: 14-15px
- Muted text: `var(--text-muted)`
- Success checkmarks: `var(--xero-green-success)`
- Disclaimer box: `var(--xero-grey-100)` background, `var(--border-strong)` border

### 13.3 Responsive behaviour

Responsive/mobile variant should be supported for v1 using standard XUI responsive behaviour. This is a default expectation, but minor responsive polish should not block an initial controlled beta.

---

## 14. Open Questions Summary

| # | Area | Question | Priority |
|---|---|---|---|
| C1 | Compliance | Bell Gully sign-off on final in-product copy (legal phrase set incorporated, pending final review) | P1 |
| ~~C2~~ | ~~Compliance~~ | ~~Implied consent (button click) vs. explicit consent (checkbox)~~ | ~~Resolved: explicit checkbox adopted per legal phrase set~~ |
| ~~C3~~ | ~~Compliance~~ | ~~AML disclaimer detail modal, final approved copy~~ | ~~Resolved: legal phrase set incorporated~~ |
| ~~U1~~ | ~~UX~~ | ~~Empty state, deep link to add bank account~~ | ~~Resolved: no deep link in v1 beta; target cohort should already have eligible accounts~~ |
| ~~U2~~ | ~~UX~~ | ~~Ineligible org (non-NZ) handling~~ | ~~Resolved: hide entry points for non-NZ orgs~~ |
| ~~U3~~ | ~~UX~~ | ~~Responsive/mobile layout~~ | ~~Resolved: responsive modal expected for v1 (non-blocking polish)~~ |
| ~~U4~~ | ~~UX~~ | ~~Confirmation notification after enablement~~ | ~~Resolved: in-product confirmation state with context-aware CTA~~ |
| ~~P1~~ | ~~Permissions~~ | ~~Which Xero roles can enable Pay by bank~~ | ~~Resolved direction: use bank-feed-authorised permissions, preferably edit-capable roles; final role-name mapping remains follow-up~~ |
| ~~P2~~ | ~~Product~~ | ~~Do existing draft invoices get Pay by bank after enablement~~ | ~~Resolved: branding-theme-driven behaviour applies to existing drafts; manual activation may be required where another provider is active~~ |
| ~~P3~~ | ~~Product~~ | ~~Disable/revoke flow~~ | ~~Resolved: follow existing Custom URL provider disable behaviour, plus clear edit-account entry point~~ |

---

## 15. Appendix: Prototype Reference

The following files are from the stakeholder alignment prototype. They illustrate the intended UX and flow but are **not** the codebase to build from. Production implementation should follow Xero's standard patterns and component libraries.

| File | What it demonstrates |
|---|---|
| `A2APaymentsApp/ClientApp/src/pages/OnboardingWizardAggressiveFast.tsx` | Primary onboarding flow React component |
| `A2APaymentsApp/ClientApp/src/pages/OnboardingWizardAggressiveFast.css` | Styles for onboarding modal |
| `A2APaymentsApp/ClientApp/src/pages/InvoiceView.tsx` | Invoice entry point with banner + OPMM modal |
| `A2APaymentsApp/ClientApp/src/pages/OnlinePaymentsSettings.tsx` | Settings entry point with service tiles |
| `A2APaymentsApp/ClientApp/src/pages/OnboardingRouter.tsx` | Flow variant router |
| `A2APaymentsApp/ClientApp/src/types/EntryContext.ts` | Flow context types (prototype-specific, for reference only) |
| `A2APaymentsApp/ClientApp/src/mocks/xeroOrgData.ts` | Mock bank accounts and org data |
| `A2APaymentsApp/ClientApp/src/config/onboardingContent.ts` | Configurable flow copy |
| `A2APaymentsApp/ClientApp/src/components/ComplianceDisclosure.tsx` | Reusable compliance disclosure component |
| `A2APaymentsApp/ClientApp/src/components/SetupBanner.tsx` | Entry point banner component |
| `compliance/AML_CFT_LLM_CONTEXT.md` | Compliance guardrails (canonical) |
| `docs/architecture/branding-themes-3-slot-problem.md` | Branding theme constraints |
