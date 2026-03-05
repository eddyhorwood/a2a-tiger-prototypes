# A2A Onboarding Prototype - PRD

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
- Dashboard widgets, settings tiles, invoice modals, banners, guided setup tasks
- All funnel to the same flow (not separate implementations)

**Why this matters**: Safer A2A must be designed as **one provider** plugged into One Onboarding, not a standalone custom flow. Prototype must validate patterns that work within this architecture.

## Success Criteria (Workshop Context)
**Team can answer**:
- Which flow best balances legal requirements vs conversion optimization?
- Does legal messaging feel sufficient to Bell Gully without killing momentum?
- Does pre-filling feel helpful or intrusive?
- Which approach should we pilot first?

**Specific debates to resolve**:
- Aggressive flow (Amazon one-click style): Too risky legally?
- Safe flow (full explainers): Too slow for invoice capture?
- Balanced flow: Good enough for both?

| **Screens** | 2 screens | 3 screens | 4+ screens |
| **Pre-fill** | Auto-fill everything, "Looks right?" | Pre-fill some, ask to confirm | Ask user to type everything |
| **Legal copy** | Minimal footer text | Short inline explanations | Full explainer screens |
| **Consent** | Implied / checkbox at end | Single explicit consent step | Multiple consent checkpoints |
| **Tone** | "We've got this, click to continue" | "Here's what we need" | "Here's why + what it means" |

**One Onboarding constraint**: All flows must be 1-3 screens to match existing payment onboarding patterns (not 6+ step wizards).

## Open Questions for Workshop
1. **PII/pre-fill level**: How much can we pre-fill from Xero org data without feeling intrusive?
2. **Legal placement**: Upfront explainer vs post-OAuth vs just checkbox?
3. **CTA language**: "Enable Pay by bank" vs "Accept bank transfers" vs "Connect direct bank payments"
4. **Entry point differences**: Should invoice→fast, settings→detailed be different flows?
5. **Settlement account UX**: How do we surface the right account when orgs have 5+ bank accounts?
 Spectrum: Aggressive → Balanced → Safe

**These are NOT A/B test variants**. They represent different implementation options with a range of aggressiveness, showing the team the full compliance/friction tradeoff space.

| Dimension | Aggressive | Balanced | Safe |
|-----------|-----------|----------|------|
| **Steps** | 3-4 | 5-6 | 7-8 |
| **Pre-fill** | Auto-fill everything, "Looks right?" | Pre-fill some, ask to confirm | Ask user to type everything |
| **Legal copy** | Minimal footer text | Short inline explanations | Full explainer screens |
| **Consent** | Implied / checkbox at end | Single explicit consent step | Multiple consent checkpoints |
| **Tone** | "We've got this, click to continue" | "Here's what we need" | "Here's why + what it means" |

**Workshop value**: 
- Aggressive shows dream UX (might be legally insufficient)
- Safe shows legally bulletproof (might kill conversion)
- Balanced is the negotiation starting point

---

## Tonight's Scope: Balanced Flow + Switcher

**Deliverable**: Single complete Balanced flow from invoice entry, with config foundation for easy cloning to Aggressive/Safe later.

**Includes**:
- Switcher page to control entry point + flow variant
- Mock CoA with realistic bank accounts
- Config-driven legal copy + CTA language
- Full settlement account logic (edge cases, pre-selection)
- Mock Akahu OAuth stub

**Out of scope tonight**:
- Building Aggressive + Safe variants (clone Balanced later)
- Settings entry point variations

---

## Flows to Build

### Flow A: "Safe" (High explanation, upfront legal)
*Full implementation later—spec retained for reference*:

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

## Flows to Build

### Flow A: "Trust First" (High explanation, upfront legal)
**Hypothesis**: Merchants need to understand custody/Akahu before they'll trust OAuth

**Steps**:
1. **Welcome**
   - Hero: "Accept bank transfers with Pay by bank"
   - Value props: Lower fees, faster settlement, auto-reconciliation
   - CTA: "Get started" (main button)
   
2. **How it works**
   - Diagram/illustration: Customer bank → Akahu initiates → Your bank receives (Xero not in flow)
   - 3-panel explainer:
     - "Customers choose their bank and approve the payment"
     - "AkahuBalanced" (Middle ground—BUILD THIS FIRST)
**Hypothesis**: Balance legal clarity with conversion optimization

**Steps**:
1. **Welcome**
   - Hero: "Accept direct bank payments"
   - Value props: Lower fees (vs credit card), faster settlement, auto-reconciliation
   - Brief explainer: "Customers pay from their bank—funds go directly to your account"
   - CTA: "Get started" (main button)
   
2. **Choose settlement account**
   - **Critical UX area**: Pre-filled dropdown from mock CoA
   - Account display format: `"ANZ Business Account (090) •••• 1234"`
   - Help text: "Where should customer payments be deposited?"
   - Edge cases:
     - 0 accounts: Show message "You need at least one bank account in your chart of accounts" + disabled CTA
     - 1 account: Pre-select automatically
     - 2+ accounts: Show dropdown
   - Stub option at bottom: "+ Add new bank account..." (non-functional)
   - CTA: "Continue" (main button)
   
3. **How it works**
   - Brief explainer: 
     - "Customers choose their bank and approve the payment"
     - "Akahu securely initiates the direct bank transfer"
     - "Funds are transferred directly to your bank—Xero and Akahu never hold your money"
   - Diagram/illustration (optional for v1)
   - CTA: "Next" (main button)
   
4. **Connect to Akahu**
   - Explainer: "We use Akahu to initiate secure bank transfers"
   - What Akahu will access: Initiate payments on your behalf
   - Checkbox: "I understand that payments are direct bank-to-bank transfers. Xero and Akahu do not hold funds."
   - Link: "View full terms" (modal stub)
   - CTA: "Connect to Akahu" (main button, disabled until checkbox)
   
5. **Akahu OAuth** (mock)
   - Simple placeholder: 
     - "Authorizing with Akahu..."
     - Bank selection dropdown (ANZ, Westpac, BNZ, ASB)
     - "Approve" button
   - Auto-return after 2 seconds
   
6. **Success**
   - Confirmation: "Pay by bank is now enabled"
   - What happens next:
     - "Customers will see 'Pay by bank' as payment option on invoices"
     - "Payments typically clear in 1-2 business days"
   - CTA: "Go to Online payments settings" or "Back to invoice"

**Config extraction**:
- All copy in `src/config/onboardingContent.ts` under `balanced` key
- Legal checkbox text easily editable
- CTA labels/help text all configurable

---

### Flow C: "Aggressive" (Fast path, minimal friction)
*Clone from Balanced later—spec retained for reference*
     - "Funds go directly to your bank account—Xero never holds your money"
   - CTA: "Next" (standard button)
   
3. **Choose settlement account**

**Why mock instead of real Xero app?**
- Workshop needs controlled demos (show edge cases, not random test org data)
- Speed: Workshop-ready tonight vs 1-2 days OAuth setup
- Focus: Debate flows, not debug OAuth
- Sufficient fidelity: Settlement account UI looks identical with mock vs real data
- Decision: Consider real Xero app IF graduating from workshop to pilot testing

**Settlement accounts** (realistic mock CoA):
```typescript
[
  {
    id: "acc-001",
    name: "ANZ Business Account",
    code: "090",
   Implementation Plan

### Tonight (Phase 1):
1. ✅ Create config file: `src/config/onboardingContent.ts`
   - Legal copy for all flows
   - CTA language
   - Help text
2. ✅ Create mock data: `src/mocks/xeroOrgData.ts`
   - Chart of accounts with bank accounts
   - Org details for pre-fill
3. ✅ Build switcher page: `/prototype-config`
   - Entry point selector (Invoice, Settings)
   - Flow variant selector (Aggressive, Balanced, Safe)
   - "Start Demo" button with routing
4. ✅ Build Balanced flow: `/merchant-onboarding?flow=balanced&source=invoice`
   - All 6 steps with realistic XUI components
   - Settlement account logic with edge cases
   - Mock Akahu OAuth stub
   - Success screen with navigation

### Later (Phase 2):
- Clone Balanced → Aggressive (remove steps 3-4, auto-select account)
- Clone Balanced → Safe (add more explainer steps, split consent)
- Add Settings entry point variant flows
- Test CTA language variants if valuable

---

## Next Steps After Workshop
1. Get team feedback: Which flow feels right?
2. Get Legal sign-off: Which flow meets Bell Gully requirements?
3. Decision: Which variant(s) to pilot with real merchants?
4. If piloting: Consider building real Xero OAuth app for actual org data
    isEnablePayments: false,
    bankAccountNumberMasked: "•••• 9012"
  }
]
```

**Xero org data** (pre-fill):
```typescript
{
  organisationName: "Acme Consulting Ltd",
  contactName: "Jordan Smith",
  address: "123 Queen Street, Auckland 1010",
  email: "jordan@acmeconsulting.co.nz"
}
```

**Akahu OAuth**: Simple placeholder screen with bank dropdown + approve button. Auto-return after 2s.
4. **Terms & conditions**
   - Checkbox: "I understand that Xero and Akahu do not hold customer funds. Payments are direct bank-to-bank transfers."
   - Link: "View full terms" (modal with legal text)
   - CTA: "Connect to Akahu" (main button, disabled until checkbox)
   
5. **Connect to Akahu** (mock OAuth)
   - Redirect to mock Akahu screen showing:
     - Xero is requesting to initiate payments
     - Permissions list
     - Bank selection dropdown
     - "Approve" button
   - Return to next step
   
6. **Success**
   - Confirmation: "Pay by bank is now enabled"
   - What happens next:
     - "Customers will see Pay by bank as a payment option on invoices"
     - "Payments typically clear in 1-2 business days"
   - CTA: "Go to Online payments settings" or "Back to invoice"

**Test questions**:
- Does early legal explanation increase trust or slow momentum?
- Is checkbox friction worth the explicit consent?
- Do merchants feel confident about what they enabled?

---

### Flow B: "Minimal Friction" (Fast path, delay legal)
**Hypothesis**: Merchants want speed—explain after OAuth, not before

**Steps**:
1. **Welcome**
   - Hero: "Enable Pay by bank in 2 steps"
   - Brief: "Let customers pay you via direct bank transfer. Lower fees, faster settlement."
   - CTA: "Enable Pay by bank" (main button)
   
2. **Choose settlement account**
   - Pre-filled dropdown (same as Flow A step 3)
   - Help text: "Where should payments be deposited?"
   - CTA: "Connect to Akahu" (main button)
   
3. **Connect to Akahu** (mock OAuth)
   - Same mock Akahu screen as Flow A
   
4. **Success + explainer**
   - Confirmation: "Pay by bank is now enabled"
   - Post-connection explainer expandable section: "How it works"
     - Brief custody explanation (bank-to-bank, no Xero holding)
     - Link to full terms
   - What happens next (same as Flow A)
   - CTA: "Done" or "Go to settings"

**Test questions**:
- Do merchants complete faster without upfront explanation?
- Do they feel surprised or under-informed post-OAuth?
- Is post-completion explainer ever read/opened?

---

### Flow C: "Contextual Entry" (Entry point determines flow)
**Hypothesis**: Invoice-entry merchants need speed; settings-entry merchants want detail

**Implementation**:
- **From invoice entry** → Use Flow B (minimal friction)
- **From settings entry** → Use Flow A (trust first)
- URL param `?from=invoice` or `?from=settings` determines which variant to show

**Test questions**:
- Does entry context predict completion preference?
- Do invoice merchants abandon settings-style detail?
- Do settings merchants feel under-informed with fast path?

---

## Architecture Notes

### Current State
- Entry points: 
  - `/settings/online-payments` → "Connect" button on Pay by bank card
  - `/invoice/INV-001` → "Connect payment service" button in OPMM modal
- Onboarding wizard: `/merchant-onboarding?source=X&mode=Y`
- Entry context tracking: URL params (source, mode, returnTo, metadata)

### Flow Routing
Add URL param `?flow=A|B|C` to control which onboarding variant displays:
- Flow A: `/merchant-onboarding?source=settings&flow=A`
- Flow B: `/merchant-onboarding?source=invoice&flow=B`
- Flow C: Auto-selects based on `source` param (invoice→B, settings→A)

### Data Mocks
- **Settlement accounts**: Mock 2-3 bank accounts from "API.Accounting"
  - Business Cheque Account (03-XXXX-XXXXXX78-00)
  - Operating Account (12-XXXX-XXXXXX23-01)
- **Akahu OAuth**: Mock redirect with bank selection, approval flow
- **Org context**: Pre-fill org name, current user name where appropriate

### Out of Scope (prototype)
- Actual Akahu API integration
- Real OAuth flow
- Persistent state across sessions
- Payment execution/webhook handling
- Metrics/analytics instrumentation (manual observation only)

---

## CTA Language Testing (Bonus Variation)
If time permits, test 3x CTA variants within Flow B structure:

**Variant B1**: "Enable Pay by bank"
**Variant B2**: "Accept bank transfers" 
**Variant B3**: "Connect direct bank payments"

Same flow, different button copy on step 1. Test which resonates most.

---

## Next Steps
1. Build Flow A + Flow B this week
2. Click through with product/legal stakeholders
3. If neither feels right, build Flow C as hybrid
4. If time permits, add CTA language variants
5. Document findings: which flow completed fastest, felt clearest, addressed legal concerns

---

## References
- ERD: `[DRAFT] High-level ERD_ NZ A2A Safe Payments`
- Bell Gully legal constraints (AML/CFT, custody language)
- Current entry points: `/settings/online-payments`, `/invoice/INV-001`
- One Onboarding initiative (FY27 alignment)
