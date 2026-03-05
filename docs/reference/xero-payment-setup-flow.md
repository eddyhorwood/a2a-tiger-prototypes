# Xero Payment Setup Flow - Complete Context for LLMs

**Purpose:** Comprehensive reference for how payment service setup actually works in Xero production. Use this to inform realistic prototype design, entry point implementation, and state management patterns.

**Audience:** AI agents, developers building payment onboarding flows

**Last Updated:** 2026-03-05

---

## Table of Contents

1. [Key Concepts](#key-concepts)
2. [Preconditions and Constraints](#preconditions-and-constraints)
3. [Entry Points](#entry-points)
4. [Core Setup Flow (5 Phases)](#core-setup-flow-5-phases)
5. [Provider State Machine](#provider-state-machine)
6. [Post-Conditions and Observability](#post-conditions-and-observability)
7. [Integration Pattern for New Providers](#integration-pattern-for-new-providers)

---

## Key Concepts

### Organisation (Org)
Xero business entity with:
- Region (AU/NZ/UK/US/CA/etc.)
- Plan (Starter/Standard/Premium)
- Bank accounts
- Branding themes
- Invoices and bills

### User
Authenticated person in an org with specific permissions:
- Can/cannot set up payment services
- Can/cannot manage payment services
- Role-based access controls

### Payment Service / Provider
Third-party payment provider integrated with Xero:
- **Examples:** Stripe, GoCardless, PayPal, Akahu (bank payments)
- Each may expose multiple **payment methods** (card, direct debit, bank pay, wallets)
- Provider owns KYC/KYB and compliance flows

### Payment Option (in UI)
The merchant-facing toggle/setting visible in:
- **Payment Options Modal (OPMM)** - "green modal", historically "online payments modal"
- **Invoice onboarding modal** - first-time template + payment method setup
- **Payment Services landing page** - `/app/{shortcode}/payment-services`

### Configuration vs. Attachment

**Configured:**
- Provider onboarding completed
- Mapped to Xero bank account (settlement)
- Mapped to fee account
- Surcharging rules defined (optional)

**Attached:**
- Specific invoice or branding theme has payment option enabled
- Pay-now UI appears on online invoice/quote
- Can be toggled per-invoice via OPMM

---

## Provider State Machine

For each provider, per org:

### States

| State | Meaning | UI Treatment |
|-------|---------|--------------|
| `NOT_CONFIGURED` | No relationship yet | Show "Get set up" CTA |
| `SETUP_STARTED` | User started but didn't finish onboarding | Show "Resume setup" CTA + "Almost there!" banner |
| `SETUP_COMPLETE` | Payments attached and usable | Show "Connected" badge, settlement account, "Manage" CTA |
| `ERROR` / `BLOCKED` | Provider or KYC/KYB failure | Show "Fix issues" CTA, error banner |

### State Transitions

```
NOT_CONFIGURED
    ↓ (user clicks "Get set up")
SETUP_STARTED
    ↓ (completes provider onboarding + Xero config)
SETUP_COMPLETE
    ↓ (can disconnect or encounter errors)
ERROR or back to NOT_CONFIGURED
```

---

## Preconditions and Constraints

For **any** new payment option to be set up:

### 1. Region & Eligibility
- Org must be in region supported by provider AND Xero's payments platform
- Examples:
  - Stripe card: AU/NZ/UK/US/CA
  - GoCardless: NZ/AU/UK/EU
  - Akahu (NZ bank payments): NZ only

### 2. User Permissions
- Only users with appropriate permissions see "set up payment options" CTAs
- Must be able to manage payment services / make payments

### 3. Bank Accounts
- At least one Xero bank account required for:
  - **Settlement account:** where payouts land
  - **Fee account:** where processing fees are coded
- Some flows prompt to connect bank account before/during provider onboarding

### 4. KYC/KYB / Verification
- Provider owns identity verification flows (Stripe, GoCardless, etc.)
- Xero must understand verification state to determine if option is usable
- Some legacy flows have separate verification sub-flows that can fail independently

### 5. Beta / Feature Flagging
- New payment options gated by:
  - LaunchDarkly / Statsig feature flags
  - Experiment targeting (region, plan, cohorts)
- Must work when **on** for target cohorts and be **invisible** when flags block it

---

## Entry Points

Multiple entry points into the same underlying setup journey:

### 1. Invoice Onboarding Modal (New Org / First Invoice)

**When:** Shown during invoice template setup for new users

**Contains:**
- Invoice template section (logo, address, tax number)
- Payment method section (online payments, payment advice)

**User Journey:**
1. User ticks checkbox: "online payments"
2. CTA changes from "Save" to "Save and set up [provider]" (e.g., Stripe)
3. On click:
   - Validates + saves invoice template fields
   - Redirects to provider onboarding

### 2. Payment Options Modal (OPMM / "Green Modal")

**Shown from:**
- Invoice preview modal: "Set up online payments" CTA
- Invoice send modal (experiment): checkbox "accept online payments"
- Invoice view/edit high-contrast banners: "Set up new payment options"

**Purpose:**
- Provider-agnostic list of all available payment services
- Let user start setup from invoicing context without visiting Settings

### 3. Payment Services Landing Page (Settings)

**URL:** `https://go.xero.com/app/{shortcode}/payment-services`

**Accessed via:**
- Navigation: Sales → Online payments
- Help links: "learn more about payments"

**Contains:**
- List of **configured** and **available** payment services
- CTAs: "Set up [provider]", "Resume setup", "Edit connection"

### 4. Other Product Entry Points

**Quotes:**
- Contextual banner in quote creation flow
- "Win the work" stage encouragement

**Payment Links:**
- Payment Service MFE surfaced with same "Get set up" flows

**Mobile (Xero Accounting App):**
- Tap to Pay (TTP) entry points in invoice UI
- "Enable Tap to Pay" CTAs launch Stripe TTP onboarding

---

## Core Setup Flow (5 Phases)

### Phase 1: Discover & Decide

**User sees prompt in:**
- Invoice onboarding modal (new org)
- Invoice view/edit banners: "Set up new payment options", "Add online payments"
- Invoice preview side panel with payments toggle
- Invoice send modal checkbox
- Payment Services page

**Prompt communicates:**
- Value prop: "Get paid faster", "Offer more ways to pay", "Reduce late payments"
- Signal: "Almost there!" (for incomplete setup)

**User action:**
- Clicks primary CTA: "Set up online payments", "Finish setup", "Get set up"

### Phase 2: Launch Payment Options Modal (Xero-side)

**Xero opens OPMM:**
- Lists available payment services (Stripe, GoCardless, PayPal, bank pay, etc.)
- For each provider:
  - Short description + key benefits
  - Method badges (card icons, direct debit, bank, wallets)
  - Status: Not started / Setup started / Setup complete

**Two primary actions per provider:**
- "Get set up" / "Finish setup": start/resume onboarding
- "Learn more": link to help content

**User chooses provider:**
- Clicks "Get set up" for desired provider (e.g., Pay by Bank)

### Phase 3: Provider Onboarding (External or Embedded)

**Xero initiates provider onboarding via CP Provisioning / Payment Service MFE:**

**Web flow:**
- Renders "Get set up" modal (Payment Service MFE) inside Xero
- On confirmation, redirects or embeds provider's onboarding UI
- Uses hosted or embedded components

**Mobile flow:**
- Similar pattern for Tap to Pay on iPhone
- Launches Stripe TTP onboarding

**Provider-side onboarding steps (typical):**
1. Collects legal/business info (KYC/KYB)
2. Collects payout bank account details (may differ from Xero's bank mapping)
3. Configures payment methods (card, direct debit, bank pay, wallets)
4. Handles identity and risk checks
5. At completion, redirects back to Xero URL (Payment Services page, invoice context, dashboard)

**Error / Partial Completion Cases:**
- User can drop out before finishing
- CP Provisioning synthetic monitors test "resume" from:
  - New invoicing
  - Payment Services landing page
  - Resume landing pages (different regions)
- Historical: separate **verification** step (GoCardless) could leave users stranded

### Phase 4: Xero-side Configuration and Finalisation

**On successful provider onboarding:**
1. Xero updates provider status:
   - `SETUP_STARTED` (if additional Xero config needed)
   - `SETUP_COMPLETE` (if ready to use)

2. Returns user to:
   - Payment Services landing page, OR
   - Dedicated "resume setup" modal / "Almost there!" banner

**Xero asks user to complete internal mapping:**
- **Payment account:** where incoming payments land (Xero bank account)
- **Fee account:** where processing fees are coded (Xero expense account)
- **Optional:**
  - Surcharging rules
  - Which branding themes/invoice templates have this provider enabled

**When mappings saved:**
- Provider state → `SETUP_COMPLETE`
- Resume banners/prompts hidden or downgraded

### Phase 5: Attach to Invoices / Quotes

**Once provider is `SETUP_COMPLETE`, methods can be attached:**

**Default attachment:**
- Branding theme / invoice settings show new payment option
- When enabled there, appears on all invoices using that theme

**Per-invoice toggles (OPMM):**
- On invoice edit page, open Payment Options modal
- Toggle specific methods (Card, Bank pay) on/off per invoice
- Preview invoice with payment buttons enabled

**Preview modal:**
- Invoice preview includes payments preview toggle
- Shows how online invoice looks with payment buttons
- Promotes setup if not yet configured

**On payer side:**
- Payer opens online invoice/quote
- Sees available payment options (buttons, card icons, bank pay CTAs)
- If option supports mandates/autopay: "save card", "set up autopay" experiences

---

## Post-Conditions and Observability

### Attach Metrics (Tracked by Xero)
- Payments setup started
- Payments attached (org-level)
- Conversion along attach funnel: impression → click → setup → attached

### Guardrails
New Relic synthetics validate key flows:
- Starting/resuming Stripe, GoCardless, PayPal onboarding from:
  - New invoicing
  - Payment Services landing page
  - Resume landing page
  - Payment Links

### Experiments Driving Entry Points
- IO-03, IO-10, IO-11, etc. experiment with:
  - Invoice onboarding modal checkboxes
  - Send-modal checkboxes
  - Preview-modal CTAs
  - Banners in quotes / invoice view

---

## Integration Pattern for New Providers

### How Pay by Bank (A2A) Should Plug In

For a new payment option (e.g., Akahu bank payments) to integrate with existing Xero patterns:

### 1. Surface as First-Class Provider Everywhere
Add new provider tile ("Pay by bank") into:
- Payment Options Modal (OPMM) from invoices, send modal, preview
- Payment Services landing page (`/payment-services`)
- Future One Onboarding unified flows for Get Paid (pay-in)

### 2. Reuse the Same State Machine
For each org, maintain states:
- `NOT_CONFIGURED` → no relationship
- `SETUP_STARTED` → onboarding started, not completed
- `SETUP_COMPLETE` → can be attached to invoices
- `ERROR/BLOCKED` → provider cannot be used

Expose states to:
- Banners ("Almost there!")
- Resume setup CTAs
- Attach banners/modals

### 3. Plug Into CP Provisioning / Payment Service MFE
Implement provider-specific onboarding behind same:
- "Get set up" CTAs
- New Relic synthetic coverage for:
  - Start from Invoicing
  - Start from Payment Services
  - Resume flows

### 4. Respect Existing Preconditions
- Region + plan gating
- Permission checks
- Bank account mapping + fee account mapping
- KYC/KYB and verification state propagation
- Beta flags + experimental cohorts

### 5. Attachment Rules for OPMM
Provider should **only appear as attachable option** in OPMM when:
- Provider state = `SETUP_COMPLETE` for that org
- At least one method enabled at org level (not blocked by plan/region)

OPMM then allows per-invoice toggling, like card/direct debit.

### 6. Future: One Onboarding Alignment
Long-term, Xero's One Onboarding aims for:
- Single KYC/KYB journey
- Shared data across Get Paid / Pay Out / Payroll
- Multi-user delegation (advisor/client shared tasks)

New payment options should:
- Reuse shared onboarding/identity data
- Expose only minimal provider-specific steps

---

## Real-World Examples

### Example 1: Stripe Card Payments (Existing)

**Entry:** Invoice preview → "Set up online payments"
**Flow:**
1. Opens OPMM
2. User clicks "Get set up" on Stripe
3. Xero launches Stripe Connect onboarding (embedded in iframe)
4. Stripe collects business info, bank account, verification
5. Redirects back to Xero Payment Services page
6. Xero prompts for settlement account + fee account mapping
7. User saves → status = `SETUP_COMPLETE`
8. Returns to invoice → can now toggle "Cards" in OPMM

### Example 2: GoCardless Direct Debit (Existing)

**Entry:** Settings → Payment Services → "Set up GoCardless"
**Flow:**
1. Clicks "Get set up"
2. Xero redirects to GoCardless hosted page
3. GoCardless collects business details, bank verification
4. Redirects back to Xero with success token
5. Historical: separate verification step (could fail independently)
6. Xero maps settlement + fee accounts
7. Status = `SETUP_COMPLETE`
8. Direct Debit now available in OPMM for invoices

### Example 3: Akahu Bank Payments (A2A Prototype Pattern)

**Entry:** Invoice view → banner "Get paid 2× faster with online payments"
**Flow:**
1. User clicks "Set up online payments" on banner
2. Entry context: `{ source: 'banner', mode: 'first_time', metadata: { invoiceId, invoiceAmount } }`
3. Launches onboarding wizard
4. Wizard collects:
   - Settlement account selection
   - Akahu OAuth authorization
5. Wizard creates provider relationship via API
6. Returns to invoice with status = `SETUP_COMPLETE`
7. Banner hidden (payment service configured)
8. "Pay by bank" now available in OPMM

---

## Key Terminology Reference

| Term | Meaning |
|------|---------|
| **OPMM** | Online Payments Modal / Payment Options Modal / "green modal" |
| **CP Provisioning** | Commerce Platform Provisioning service (manages provider relationships) |
| **Payment Service MFE** | Micro-frontend for payment service setup flows |
| **KYC/KYB** | Know Your Customer / Know Your Business (identity verification) |
| **Settlement account** | Xero bank account where payment funds land |
| **Fee account** | Xero expense account where processing fees are coded |
| **Branding theme** | Invoice/quote template with specific styling |
| **Attach** | Enable payment method on specific invoice or theme |
| **Resume landing page** | Xero page user returns to after incomplete provider onboarding |
| **One Onboarding** | Future unified KYC flow across all payment capabilities |

---

## Design Patterns for Entry Points

### Banner Pattern (Context-Aware)
- Appears inline where merchant works (invoice, quote, dashboard)
- High-contrast for critical actions (overdue, high-value)
- Standard blue for informational nudges
- Dismissible with user preference tracking

### Modal Pattern (Explicit)
- Merchant explicitly requests payment setup
- Shows all available providers
- Provider comparison (methods, pricing, features)
- Single provider selection flows to onboarding

### Settings Pattern (Management)
- Destination for all configured services
- "Resume setup" for incomplete flows
- "Manage" for editing existing connections
- Help resources and documentation links

---

## Provider Selection Criteria (Xero Side)

When showing available providers to merchant:

**Must be:**
- ✅ Supported in merchant's region
- ✅ Compatible with merchant's plan
- ✅ Not blocked by feature flags
- ✅ User has permission to set up

**Show as:**
- Available (NOT_CONFIGURED): "Get set up"
- In progress (SETUP_STARTED): "Resume setup" + warning banner
- Connected (SETUP_COMPLETE): "Manage" + connection details
- Blocked (ERROR): "Fix issues" + error details

---

## LLM Usage Guidelines

When using this document to inform code generation:

### Do:
- Follow the 5-phase setup flow structure
- Implement provider state machine with 4 states
- Respect preconditions (region, permissions, bank accounts)
- Track entry context for analytics
- Use Xero terminology consistently

### Don't:
- Invent new provider states beyond the 4 documented
- Skip Xero-side configuration (settlement/fee accounts)
- Assume provider onboarding happens in Xero UI (often external)
- Create payment flows that bypass CP Provisioning patterns
- Use non-standard terminology (e.g., "wallet" instead of "payment method")

### When Uncertain:
- Default to Payment Services landing page patterns
- Use `NOT_CONFIGURED` → `SETUP_COMPLETE` simple flow
- Follow existing Stripe/GoCardless patterns
- Check with docs/reference/payment-onboarding-entry-points.md for current state

---

## Document History

| Date | Author | Change |
|------|--------|--------|
| 2026-03-05 | Eddy Horwood | Initial creation - captured from session context |

---

## Related Documentation

- [Payment Onboarding Entry Points](./payment-onboarding-entry-points.md) - Current Xero entry points catalog
- [Multi-Entry Implementation](../architecture/multi-entry-implementation.md) - Entry context tracking system
- [AML/CFT LLM Context](../../compliance/AML_CFT_LLM_CONTEXT.md) - Legal guardrails for payment flows
- [XUI Component Standards](./xui-component-standards.md) - Component usage patterns
- [Xero Design Guidelines](./xero-design-guidelines.md) - Visual design rules
