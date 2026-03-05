# Branding Themes and the "3 Slot Problem" – Deep Context for AI/LLM Use

This document encodes the branding themes + payment services model and the "3 slot problem" in a way that is explicit, repetitive, and suitable for LLM grounding, feature analysis, and reasoning.

It is not optimised for human readability. Redundancy is intentional.

---

## 0. Glossary (Canonical Terms)

Use these meanings consistently:

### Branding Theme (BT)
A template-level configuration object that:
- Controls invoice appearance (logo, colours, layout, etc.)
- Stores default online payment services for invoices using that theme

### Invoice
A sales document that:
- References a branding theme
- Derives its default payment services/methods from that theme and/or other defaults
- Can override defaults via OPMM at document level

### Payment Service / Payment Gateway (PS)
A provider integration such as:
- Stripe, GoCardless, PayPal, Custom Payment URL, eWay, Chase, DPS, etc.
- A payment service offers one or more payment methods (card, bank, etc.)

### Payment Method (PM)
A way to pay within a service, e.g.:
- Card (Visa / Mastercard / Amex), bank transfer, direct debit, PayPal wallet, Apple Pay, Google Pay, A2A, etc.

### Custom URL
A legacy card-type payment service that:
- Is implemented as a Custom Payment URL
- Rides in the card slot in branding themes
- Is used by various ecosystem integrations (Melio, A2A, etc.)

### Slots
The three hard-coded positions on a branding theme that can hold at most one payment service each:
- Card slot (Slot 1)
- Bank slot (Slot 2)
- PayPal slot (Slot 3)

### OPMM (Online Payment Method Management)
The newer, document-level control for:
- Turning individual payment methods on/off per invoice
- Overriding branding theme defaults at invoice level
- Providing a more accurate view of what payers can actually use

---

## 1. Slot Model – Structural Definition

### 1.1 Slot Types

Each branding theme has exactly three conceptual slots:

#### SLOT_CARD
- **Type:** card
- **Holds:** exactly zero or one card-type payment service
- **Card-type services include:**
  - Stripe
  - eWay
  - Chase
  - DPS
  - Custom URL-based services (including A2A, Melio, etc.)

#### SLOT_BANK
- **Type:** bank
- **Holds:** exactly zero or one bank/direct-debit payment service
- **Bank-type services include:**
  - GoCardless Direct Debit
  - GoCardless Instant Bank Pay (UK)
  - Stripe ACH (US), etc.

#### SLOT_PAYPAL
- **Type:** paypal
- **Holds:** exactly zero or one PayPal service
- **Only PayPal can occupy this slot**

### 1.2 Global Slot Capacity

For any given branding theme BT:

```
BT.SLOT_CARD ∈ {NONE, card-service-1}
BT.SLOT_BANK ∈ {NONE, bank-service-1}
BT.SLOT_PAYPAL ∈ {NONE, paypal-service-1}
```

There is a hard upper bound of 3 concurrently attached services on a theme:

```
COUNT_NON_EMPTY_SLOTS(BT) ≤ 3
```

There is no way (in current architecture) to:
- Add a 4th slot
- Attach more than one service to the same slot type

### 1.3 Service-Type Constraints

- Any service categorised as card-type can only be placed in SLOT_CARD
- Any service categorised as bank-type can only be placed in SLOT_BANK
- PayPal can only be placed in SLOT_PAYPAL
- Custom URL integrations are also categorised as card-type, and therefore must occupy SLOT_CARD

**Resulting invariants:**
- SLOT_CARD can never hold both Stripe and a Custom URL simultaneously
- SLOT_CARD can never hold two different Custom URLs at the same time
- You cannot exceed one provider per slot, even if they are "compatible" from a UX POV

---

## 2. Behavioural Model – How Branding Themes Influence Invoices

### 2.1 Pre-OPMM Behaviour (Legacy)

Historically, the pipeline is:

1. Merchant configures a branding theme with up to three payment services attached (one per slot)
2. When a new invoice is created:
   - The invoice references a branding theme BT
   - The invoice's default online payment options are computed from BT.SLOT_CARD, BT.SLOT_BANK, BT.SLOT_PAYPAL
   - The online invoice and PDF display payment options that correspond to these slots

**Key points:**
- The theme's slots were the primary source of truth for what payment options appeared
- Users had limited ability to override per invoice
- Most confusion and support pain stems from this era

### 2.2 Post-OPMM Behaviour (Current)

With OPMM:

1. Branding themes still exist and still store slot-based defaults
2. When an invoice is created:
   - Branding theme defaults are used as initial seeds
   - OPMM can then override or refine which methods and services appear on that specific invoice
3. Users can open the OPMM UI on the invoice:
   - See which methods are effectively active
   - Turn methods on/off, independent of the theme's slot configuration

**Important implications:**
- Branding themes are no longer the sole determinant of payment options, but the slot constraints still govern what can be stored as defaults
- OPMM can override but cannot expand the set of services attached to a theme beyond the 3-slot architecture
- Some product surfaces (e.g. recurring invoices, payment links) still partially rely on pre-OPMM semantics

---

## 3. Formal Problem Statement – "3 Slot Problem"

### 3.1 Core Rule

**Rule P0 – Slot Capacity Rule**

For each invoice and branding theme, there can be at most one payment service per slot type (card, bank, PayPal), with a maximum of three services in total.

Expanded:
```
SLOT_CARD ∈ {0 or 1 card service}
SLOT_BANK ∈ {0 or 1 bank service}
SLOT_PAYPAL ∈ {0 or 1 PayPal service}
TOTAL_SERVICES_PER_THEME ≤ 3
```

### 3.2 Practical Consequences

#### Consequence C1 – Custom URL vs Stripe Collision

- All Custom URL integrations are enforced as card-type
- Therefore:
  - If BT.SLOT_CARD = Stripe, then no Custom URL can be attached to the same branding theme
  - If BT.SLOT_CARD = CustomUrl(A2A) then Stripe cannot be attached to that theme concurrently
  - If BT.SLOT_CARD = CustomUrl(Melio), then CustomUrl(OtherPSP) cannot be added simultaneously

#### Consequence C2 – Global Cap of Three Services

Even if a merchant wants:
- Stripe (cards)
- GoCardless (DD)
- PayPal
- An additional A2A PSP (via Custom URL)

They must choose at most three, because only 3 slots exist. They cannot offer four providers as defaulted via a single branding theme.

#### Consequence C3 – Service-Level Defaulting vs Method-Level Needs

- Slots store services, not granular methods (e.g. card vs ACH vs wallet)
- Many merchants conceptually think in terms of "methods" (e.g. "turn on direct debit everywhere, keep cards off on some invoices")
- The 3-slot model forces decisions at the service layer, creating mismatches between what customers want to configure and what the platform can express

---

## 4. Scenario Library – Concrete Slot Collisions

### 4.1 Scenario A – Stripe + A2A / Akahu (Safer A2A NZ-Style)

#### Context
- A2A integration is implemented as a Custom Payment URL (card-type)
- Stripe is the primary card provider for many orgs

#### Slot Mapping
- A2A(Akahu) → SLOT_CARD (because Custom URL is card-type)
- Stripe → SLOT_CARD

#### Result
- Only one of {Stripe, A2A(Akahu)} can occupy SLOT_CARD on a given branding theme
- You cannot have BT.SLOT_CARD = Stripe + A2A(Akahu); the slot field is single-valued

#### Implications
If a merchant already has Stripe in the card slot:
- You cannot automatically default the A2A Custom URL onto the same theme
- One service (Stripe or A2A) must "win" the card slot

To deliver A2A as a defaulted option:
- **Option 1:** Create a dedicated A2A branding theme (e.g. "Pay by bank")
- **Option 2:** Rely on OPMM per-invoice overrides and keep Stripe in the theme slot

#### Current Design Pattern (Safer A2A NZ)
- Accept that A2A (Custom URL) occupies SLOT_CARD for a particular theme
- Use:
  - Branding theme selection + contact defaults to route A2A where it should be defaulted
  - OPMM to allow merchants to combine A2A with other methods per invoice, where possible

### 4.2 Scenario B – Stripe + Melio (Cross-Border Public API Integration)

#### Context
- Melio integration uses a Custom Payment URL as a card-type service
- Stripe is the default card provider for many orgs

#### Slot Mapping Rules from PRD
The PRD explicitly states:
- An invoice has exactly three slots
- SLOT_CARD is shared between Stripe and Custom URL (Melio)
- Merchant cannot have two Custom URL services on the same theme

#### Result
- If BT.SLOT_CARD = Stripe then Melio cannot be attached as a default on that BT
- If BT.SLOT_CARD = CustomUrl(Melio) then Stripe or any other Custom URL service cannot be attached concurrently

#### Mitigation Strategy
Programmatically create or guide merchants to a dedicated "Melio USD Payments" branding theme:
- BT_MELIO.SLOT_CARD = CustomUrl(Melio)
- BT_MELIO.SLOT_BANK and BT_MELIO.SLOT_PAYPAL configured if needed

Use:
- Contact defaults so US customers default to the Melio theme
- Invoice-level OPMM overrides for any cases where Stripe and Melio need to co-exist on an individual invoice (not as theme defaults)

---

## 5. Impact Model – Why This is a Problem

### 5.1 Customer Impact (Friction Patterns)

#### Pattern CI1 – Forced Trade-Offs
Merchants who want to:
- Offer Stripe (cards)
- Offer Custom URL A2A or Melio
- Offer PayPal
- Offer GoCardless

Are forced to:
- Choose at most three
- Resolve conflicts in SLOT_CARD between Stripe and any Custom URL

#### Pattern CI2 – Hidden Configuration & Unclear Mental Model
Branding theme UI historically:
- Shows "slots" with limited clarity on which service is wired where
- Does not clearly show which methods correspond to which services

Users often do not understand:
- Why a payment option they expect is missing on an invoice
- Why enabling a new service causes another one to disappear (slot collision)

#### Pattern CI3 – Manual Per-Invoice Fixes
When merchants hit slot limits:
- They may need to manually adjust payment options at invoice level
- This is particularly painful for bulk invoice creation or recurring invoices

### 5.2 Product / Platform Impact

#### Impact P1 – New Invoice Templates / "Better Branding"
Teams designing new invoice template editors must decide:
- Whether to recreate the three-slot behaviour (thus encoding a known anti-pattern into new surfaces), or
- To introduce a new model (e.g., method-first), which then must interoperate with existing slot-based semantics
- This increases complexity and risk whenever branding themes are touched

#### Impact P2 – OPMM vs Slots (Model Mismatch)
**OPMM's principles:**
- Method-first, document-level clarity
- Show the actual payment methods available on a specific invoice

**3-slot model:**
- Service-first, theme-level defaults
- Obscures which methods are really active

**Result:**
- OPMM must implement logic to reinterpret or override slot-based data to reflect reality
- Some flows still leak legacy slot behaviour (e.g. recurring, payment links)

#### Impact P3 – Legacy Surfaces Still Bound to Slots

**Recurring invoices:**
- Some recurring flows still pre-date OPMM uplift
- They rely on legacy InvoicePaymentGateway / slot logic
- Repeating templates may not reflect invoice-level changes introduced by PSA/OPMM

**Payment links:**
- Intended to be Stripe-only for some products
- Because they piggyback on branding theme / contact defaults, other services (GoCardless, PayPal) can be inadvertently offered
- This behaviour is driven by slot configuration, not a clean method-first model

---

## 6. Current Mitigations

### 6.1 OPMM – Invoice-Level Overrides

#### Behaviour
OPMM allows merchants to override default payment methods on individual invoices:
- Users can visit the OPMM UI inside invoice creation/edit
- They can see which methods are active and adjust them

#### Benefits
- Combining Stripe + ecosystem partner on a single invoice is possible, even when branding themes cannot store that combination as a shared default
- OPMM becomes the effective source of truth for what the payer actually sees on that invoice

#### Limitations
- OPMM does not change the underlying 3-slot constraint on branding themes
- OPMM is per-invoice:
  - To get consistent patterns (e.g. always offer A2A + Stripe for a region), you need:
    - Smart defaulting logic, or
    - Repeating templates that are properly uplifted, or
    - Merchant discipline in using certain templates and overrides
- Legacy flows not yet OPMM-uplifted (e.g. some recurring behaviours, some payment link flows) still primarily rely on slots

### 6.2 Dedicated / Specialised Branding Themes

#### Pattern
For new or conflicting card-type services, rather than sharing SLOT_CARD, create dedicated branding themes:

#### Examples

**Melio:**
- Create "Melio USD Payments" theme
- Attach Melio as the SLOT_CARD provider for that theme
- Route only relevant invoices/contacts through that theme

**A2A / Safer A2A NZ:**
- Use a dedicated A2A theme after onboarding (e.g. "Pay by bank")
- Avoid fighting with existing Stripe card slot on the default theme

#### Pros
- Allows new integrations to be defaulted in specific contexts
- Minimises impact on existing themes

#### Cons
- Increases theme proliferation and configuration complexity
- Does not remove the underlying slot architecture:
  - Still limited to 3 services per theme
  - Still one service per slot-type

---

## 7. Known Problem Surfaces (Where the 3-Slot Model Shows Up)

### 7.1 Branding Themes UX

Editor needs:
- To show accurate card icons / payment methods actually tied to a branding theme
- To reconcile:
  - Org-level available payment gateways and methods
  - BT-level enabled gateways constrained by slots

Result:
- Editor needs non-trivial logic to resolve which methods appear given slots + gateways
- This adds complexity and coupling between UI and underlying slot/gateway structures

### 7.2 New Invoice Templates

Design docs explicitly list:
- Option to link to legacy payment settings page
- Option to recreate the three-slot management in template settings
- Option to manage payment methods per template instead of services

There is tension between:
- Short-term shipping (reusing slot semantics)
- Long-term goal of a method-first model

### 7.3 Recurring Invoices

Recurring invoices:
- Still mapped largely to pre-OPMM slot logic in some paths
- Do not fully adhere to OPMM principles (e.g. which methods are surfaced vs actual OPMM configuration)
- When repeating templates are built from an invoice that had modified payment methods:
  - The template may not reflect PSA/OPMM-based payment method changes if it still reads from legacy tables

### 7.4 Payment Links

Payment links:
- Supposed to be Stripe-only in some product definitions
- Because they reuse branding theme / contact defaults, other methods (GoCardless, PayPal) can surface unexpectedly
- The root cause is that payment links are still indirectly governed by branding theme slots instead of a dedicated OPMM-based configuration

---

## 8. Long-Term Direction / Desired Future State

### 8.1 Target Model

High-level goals:
- Decouple payment services from branding themes, or at least eliminate:
  - The three hard-coded slots as primary defaults
  - The service-first approach in favour of a method-first model
- Move to a unified document-level OPMM paradigm:
  - Branding themes no longer impose artificial service count limits (no "3 slot" cap)
  - Merchants manage:
    - "Which payment methods can this payer use?"
    - At appropriate levels: org, contact, document, product surface

### 8.2 Implications for New Design Work

For any new feature that touches invoices, branding themes, or payment methods:

1. **Assume the 3-slot model still exists** until platform work explicitly removes it

2. **For Custom URL-based integrations:**
   - Treat them as card-type and therefore inherently conflicting with Stripe in SLOT_CARD

3. **Prefer designs that:**
   - Use dedicated themes + contact defaults to route conflicting card-type integrations
   - Use OPMM at invoice level as the primary mechanism for combining multiple methods

4. **In PRDs, clearly indicate whether you are:**
   - Attempting to change core branding theme logic (high-risk, multi-team dependency), or
   - Accepting the 3-slot constraint and designing explicit mitigations

---

## 9. Summary – Canonical "3 Slot Problem" Definition

For LLM / system reasoning, treat the 3 slot problem as:

### Structural Constraint
- Every branding theme provides exactly three logical payment service slots: card, bank, PayPal
- Each slot can hold at most one service
- Total concurrently attached services per theme ≤ 3

### Type Constraint
- Card-type services (including Custom URLs) compete for a single SLOT_CARD
- Bank-type services compete for a single SLOT_BANK
- PayPal has a dedicated SLOT_PAYPAL

### Behavioural Consequence
- Invoices seeded from a branding theme cannot, by default, expose more than 3 services or more than 1 service of each type
- Many integrations (Melio, A2A) that use Custom URL must displace Stripe or other card-type services on a theme

### Experience Consequence
- Merchants are forced into trade-offs that do not match their mental model (they think in terms of payment methods, not slots)
- OPMM works around the constraint at invoice level but does not remove it at theme level
- Multiple product surfaces (recurring, payment links, editor UX) inherit this technical debt

### Design Guidance
Until platform work deprecates or replaces the slot model, any new payment integration must explicitly design around this constraint via:
- Dedicated themes
- Contact-level defaults
- OPMM-based invoice overrides

---

## Questions & Clarifications

If you need further context or clarification on any aspect of the 3 Slot Problem, please ask about:
- Specific integration scenarios
- Technical implementation details
- Product surface interactions
- Migration strategies
- OPMM behaviour and capabilities
