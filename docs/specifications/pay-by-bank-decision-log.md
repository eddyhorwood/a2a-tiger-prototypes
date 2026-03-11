---
purpose: "Lightweight decision log for Pay by Bank / Safer A2A"
audience: "Product, Design, Engineering, Risk, CX, and AI/LLM agents"
last_updated: "2026-03-11"
status: "current"
---

# Pay by Bank / Safer A2A Decision Log

Lightweight decision log for Pay by Bank / Safer A2A NZ and global rollout.

## How to use this doc

- Changelog gives the latest status for each decision ID.
- Open decisions capture active alignment topics.
- Closed decisions capture aligned outcomes, rationale, and implications.
- Reference decisions from specs, tickets, and designs using the decision ID (for example `PBB-0005`).

## Changelog (summary)

| Date | Decision ID | Title | Area | Status |
|---|---|---|---|---|
| 2026-03-10 | PBB-0001 | Merchant Settlement Account Prefill Strategy | Merchant onboarding | Accepted |
| 2026-03-10 | PBB-0002 | Merchant Payout Bank Account Storage Pattern | Payouts / global data model | Open |
| 2026-03-10 | PBB-0003 | Launch Surfaces & Cohorts | Launch surfaces & rollout | Open (updated with aligned UX defaults) |
| 2026-03-10 | PBB-0004 | Merchant Permissions | Roles and permissions | Accepted (directional) |
| 2026-03-10 | PBB-0005 | Existing Invoices | Invoicing behaviour | Accepted |
| 2026-03-10 | PBB-0006 | Disable / Wind-back | Settings, risk, wind-back | Accepted |
| 2026-03-11 | PBB-0003 | Launch Surfaces & Cohorts | Launch surfaces & rollout | Open (updated: beta eligibility criteria added) |
| 2026-03-11 | PBB-0004 | Merchant Permissions | Roles and permissions | Accepted (directional, updated) |
| 2026-03-11 | PBB-0007 | Bank Account Storage and CoA Liability Boundary | Bank account data | Accepted |
| 2026-03-11 | PBB-0008 | Pay by Bank is Read-Only for CoA | Bank account data | Accepted |
| 2026-03-11 | PBB-0009 | Bank Account Naming Source | Bank account UX | Accepted |
| 2026-03-11 | PBB-0010 | Branding Theme Slot 1: Pay by Bank vs Stripe | Branding themes | Accepted |
| 2026-03-11 | PBB-0011 | DB Schema for Settlement Account Configuration | Bank account data | Open |
| 2026-03-11 | PBB-0012 | Evals and Observability Tooling Strategy | Observability | Open |

## Open decisions

### PBB-0002 - Merchant Payout Bank Account Storage Pattern

- Status: Open
- Owner / DRI: Safer A2A Tiger Team (TBC)
- Theme: Merchant payout account storage and reference pattern (NZ + global)

Problem / what is open:
- Finalise how Pay by Bank stores and references merchant payout account details across NZ and global contexts.

Current leaning:
- Reuse GoCardless / PayPal model.
- Use Chart of Accounts (CoA) as source of truth for bank details.
- Store reference and operational metadata in A2A (not raw bank details).

Options:
- Option A: CoA reference only (preferred), with optional A2A metadata.
- Option B: A2A stores full account details.
- Option C: Hybrid CoA + A2A snapshot.

Next steps:
- Validate GoCardless / PayPal implementation details in code.
- Lock A2A payout reference schema and required metadata.
- Confirm as global pattern or document regional exceptions.

### PBB-0003 - Launch Surfaces & Cohorts

- Status: Open (partial alignment completed)
- Owner / DRI: TBC
- Theme: Launch surfaces, eligibility gating, and rollout strategy

Aligned defaults from PM direction (consolidates U1-U4 without creating duplicate decision IDs):
- Non-NZ handling: hide entry points for non-NZ orgs (regional feature pattern).
- Empty-state deep link: no deep link in v1 beta from No eligible bank accounts.
- Eligibility targeting: beta targets orgs with bank feed and accounts that look like valid accounts.
- Responsive UX: responsive/mobile variant should be supported by default for v1 (not a hard blocker for minor polish).
- Post-enable UX: show an in-product confirmation screen with configurable copy and end-of-task CTA (for example View invoice).

Still open:
- Final v1 invoice surface matrix (old/new invoicing, repeating invoices, API-originated invoices, mobile).
- Cohort phasing detail and feature-flag strategy.
- GTM and support readiness gates by phase.
- Definition of "valid" bank accounts for beta eligibility (for example Type="BANK", EnablePayments=true, NZD currency). Precise validation rules and the gating model (invite-only for orgs with a valid CoA bank account vs broader access with constrained feature usage) are not yet aligned.

Next steps:
- Produce final surface matrix and phased rollout plan.
- Review with Product, Design, CX, Risk, Payments.
- Close decision once rollout plan and cohort definitions are signed off.

### PBB-0011 - DB Schema for Settlement Account Configuration

- Status: Open
- Owner / DRI: Safer A2A Tiger Team (TBC)
- Theme: Bank account data storage and liability

Problem / what is open:
- Define the exact DB schema required for Pay by Bank settlement account configuration (fields, constraints, and indexes).
- Schema must store a reference to the CoA bank account (org ID, account identifier, account name/reference) and the merchant's settlement bank account details, without writing back to CoA.

Current leaning:
- Likely includes org_id, CoA bank account identifier, CoA bank account name/reference, settlement bank details (BSB, account number), and audit fields. Final field list is not yet defined.

Next steps:
- Align on final schema with engineering.
- Confirm constraints and indexing strategy.

### PBB-0012 - Evals and Observability Tooling Strategy

- Status: Open
- Owner / DRI: TBC (Product, Engineering, Observability)
- Theme: Experimentation, metrics, and AI-native dev lifecycle

Problem / what is open:
- No confirmed strategy for integrating evaluation, metrics, and observability tooling into the Pay by Bank development lifecycle.
- Unclear whether and how AI-driven tooling is used to monitor data during development and operation.

Current leaning:
- No current leaning confirmed. Needs a concrete proposal.

Next steps:
- Develop a proposal covering metrics collection, evaluation frameworks, and observability ownership.
- Align across product, engineering, and observability owners.

## Closed decisions

### PBB-0001 - Merchant Settlement Account Prefill Strategy

- Status: Accepted
- Date: 2026-03-10
- Channel / Source: `#ai-pay-by-bank-tiger-team` Slack (summarised)
- Owner / DRI: Safer A2A Tiger Team

Decision:
- Use CoA NZD bank account as prefilled suggestion in onboarding when format checks pass.
- Prefill is editable and must be explicitly reviewed by merchant before save.
- Store settlement account at onboarding rather than relying on mutable free-text fields.
- Do not productise invoice-notes extraction for v1.
- Launch beta with a controlled cohort and instrumentation.

Rationale:
- Balances lower onboarding friction with acceptable data quality.
- Uses existing product/API surfaces and avoids adding a new runtime data pipeline.

### PBB-0004 - Merchant Permissions

- Status: Accepted (directional)
- Date: 2026-03-10 (updated 2026-03-11)
- Theme: Roles that can enable/disable Pay by Bank

Context:
- Pay by Bank setup and configuration (including enabling/disabling and editing settlement account details) requires a defined permission model.
- An existing Xero bank feed authorisation capability provides a natural alignment point for who can manage bank-related configuration.

Decision:
- The permission model for enabling and disabling Pay by Bank should align with the bank feed authorisation capability.
- Minimum direction: users with relevant bank-feed-authorised visibility should be able to access Pay by Bank setup.
- Preferred direction: users who can edit bank feed/payment settings can enable/disable Pay by Bank.
- No new custom permission concept is introduced for Pay by Bank. It should be mapped onto existing Xero roles/permissions used for bank feeds and payment settings.

Rationale:
- Reusing existing bank feed/payment settings permission models reduces complexity.
- Ensures that users who manage bank-related configuration are the ones who manage Pay by Bank.

Still open:
- Mapping the directional model to concrete Xero role names and specific permission checks (access setup, enable/disable, edit settlement configuration) is not yet defined. To be resolved as a sub-issue during implementation.

### PBB-0005 - Existing Invoices

- Status: Accepted
- Date: 2026-03-10
- Theme: Behaviour of existing invoices after enablement

Decision:
- Any invoice using a branding theme that now has the custom payment service attached can show Pay by Bank.
- This applies to existing draft invoices and new invoices, subject to branding theme behaviour.
- If another payment service is active on the branding theme (for example Stripe), merchant may need to manually activate/swap Pay by Bank on that theme.

### PBB-0006 - Disable / Wind-back

- Status: Accepted
- Date: 2026-03-10
- Theme: Disable flow and technical wind-back

Decision:
- Disable behaviour follows existing Custom URL provider pattern in Online Payments settings.
- Merchant confirms disable action, then payment service is removed/deactivated from branding themes per current provider behaviour.
- Provide a clear settings entry point to edit settlement bank account.

### PBB-0007 - Bank Account Storage and CoA Liability Boundary

- Status: Accepted
- Date: 2026-03-11
- Theme: Bank account data storage and liability

Context:
- Xero Chart of Accounts (CoA) bank account records are meaningful for accounting, but data quality is not reliable enough to use directly as the settlement account for Pay by Bank.
- During onboarding, merchants may need to enter or correct settlement bank account details that differ from what is stored in the CoA.

Decision:
- Pay by Bank will store settlement bank account details in its own application database and will not write those details back to the Xero Chart of Accounts.
- The Pay by Bank DB will hold a reference to the selected CoA bank account (including its name/reference) plus the merchant's chosen settlement bank account details entered during onboarding.

Rationale:
- Avoids increasing Xero's liability by preventing Pay by Bank from editing CoA records directly.
- Lets merchants configure accurate settlement details for Akahu payments even when CoA data quality is poor.

Implications:
- The Pay by Bank app owns a separate settlement account configuration per org.
- CoA continues to be the accounting system of record but is not the source of truth for settlement details.
- Discrepancies between CoA and settlement details are tolerated and intentionally isolated in the Pay by Bank DB.

### PBB-0008 - Pay by Bank is Read-Only for CoA

- Status: Accepted
- Date: 2026-03-11
- Theme: Editing CoA vs Pay by Bank configuration

Context:
- Pay by Bank onboarding and settings need to read CoA bank accounts to let merchants choose a settlement account.
- There is concern about liability and data integrity if the Pay by Bank UI can change CoA records.

Decision:
- The Pay by Bank UI must never directly edit Xero Chart of Accounts bank records.
- Any changes to CoA accounts (including bank account details and labels) must go through existing Xero bank account / CoA flows outside Pay by Bank.

Rationale:
- Keeps CoA change authority with existing Xero surfaces and controls, avoiding new liability and complexity in Pay by Bank.
- Reduces risk of accidental or unauthorised accounting changes from the Pay by Bank experience.

Implications:
- Pay by Bank is read-only with respect to CoA.
- If merchants need to fix or rename bank accounts in CoA, they must do so in standard Xero experiences.
- The Pay by Bank app relies on its own DB records for settlement details and only reads from CoA for account selection and display.

### PBB-0009 - Bank Account Naming Source

- Status: Accepted
- Date: 2026-03-11
- Theme: Bank account naming in UI

Context:
- During onboarding, merchants choose a settlement account. There is both the CoA bank account name/reference and the settlement bank account details stored by Pay by Bank.
- Customers are already familiar with the CoA bank account name.

Decision:
- Pay by Bank will reuse the existing bank account name/reference from the Xero Chart of Accounts as the user-facing label for the settlement account.
- The Pay by Bank DB will store a reference to that CoA bank account (including its name/reference) plus the underlying settlement details.
- Pay by Bank will not introduce a separate, independent display name for the settlement account.

Rationale:
- The CoA bank account name is already meaningful and recognisable to merchants.
- Reusing it avoids confusion, duplication, and extra naming decisions in Pay by Bank.

Implications:
- Any changes to the user-facing bank account name must happen via CoA management flows.
- Pay by Bank will reflect whatever name is set in CoA for the referenced account.

### PBB-0010 - Branding Theme Slot 1: Pay by Bank vs Stripe

- Status: Accepted
- Date: 2026-03-11
- Theme: Branding themes and payment method slot behaviour

Context:
- Existing branding themes provide three payment slots. Slot 1 is used for custom payment methods or Stripe.
- Pay by Bank is implemented as a custom payment method that competes with Stripe for the same slot.
- Some merchants will already have Stripe enabled on their branding theme.

Decision:
- Pay by Bank will use the existing branding theme Slot 1 as a custom payment method, sharing the same slot as Stripe.
- After onboarding, merchants who also use Stripe will need to manually choose which method appears on each invoice by configuring their branding themes and/or changing the theme per invoice.
- We will not auto-migrate or auto-switch Stripe configurations.

Rationale:
- Leveraging existing Slot 1 behaviour minimises changes to the branding themes system and keeps Stripe and Pay by Bank mutually exclusive in the same slot.
- Requiring an explicit merchant choice per branding theme gives cleaner comparative data on how Pay by Bank performs relative to Stripe.

Implications:
- Merchants with both Stripe and Pay by Bank enabled will have some manual work: they must adjust branding themes or invoice branding choices to switch between Stripe and Pay by Bank in Slot 1.
- Product analytics can use this to compare usage and conversion between the two payment methods.
- UI copy and onboarding must clearly explain the Slot 1 trade-off and the manual steps for merchants who already use Stripe.
