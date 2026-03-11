---
purpose: "Backbone decision log for Pay by Bank / Safer A2A implementation constraints"
audience: "Product, Design, Engineering, Risk, CX, and AI/LLM agents"
last_updated: "2026-03-11"
status: "current"
---

# Pay by Bank / Safer A2A Decision Log

Canonical decision log for product, engineering, and UX/design decisions that materially constrain how Pay by Bank / Safer A2A is implemented.

## How to use this doc

- This log is restricted to product, engineering, and UX/design decisions that materially constrain implementation.
- Beta/rollout, cohort, GTM, and document-scope planning belongs in the PRD / Beta Plan, not in this log.
- Changelog gives the latest status for each retained decision ID.
- Open decisions capture unresolved implementation constraints.
- Closed decisions capture accepted implementation constraints, rationale, and trade-offs.
- Reference decisions from specs, tickets, and designs using the decision ID (for example `PBB-0007`).

## Changelog (summary)

| Date | Decision ID | Title | Area | Status |
|---|---|---|---|---|
| 2026-03-10 | PBB-0001 | Merchant Settlement Account Prefill Strategy | Merchant onboarding | Accepted |
| 2026-03-10 | PBB-0002 | Merchant Payout Bank Account Storage Pattern | Data model / account storage | Open |
| 2026-03-11 | PBB-0003 | Organisation Prerequisites for Pay by Bank Availability | Eligibility / product availability | Open |
| 2026-03-10 | PBB-0004 | Merchant Permissions | Permissions / access control | Accepted (directional) |
| 2026-03-10 | PBB-0005 | Existing Invoices | Invoicing / branding theme behaviour | Accepted |
| 2026-03-10 | PBB-0006 | Disable / Wind-back | Settings / provider lifecycle | Accepted |
| 2026-03-11 | PBB-0004 | Merchant Permissions | Permissions / access control | Accepted (directional, updated) |
| 2026-03-11 | PBB-0007 | Bank Account Storage and CoA Liability Boundary | Data storage / liability boundary | Accepted |
| 2026-03-11 | PBB-0008 | Pay by Bank is Read-Only for CoA | CoA edit boundaries | Accepted |
| 2026-03-11 | PBB-0009 | Bank Account Naming Source | Settlement account labelling | Accepted |
| 2026-03-11 | PBB-0010 | Branding Theme Slot 1: Pay by Bank vs Stripe | Branding themes / payment method behaviour | Accepted |
| 2026-03-11 | PBB-0011 | DB Schema for Settlement Account Configuration | Database schema / settlement configuration | Open |

## Open decisions

### PBB-0002 - Merchant Payout Bank Account Storage Pattern

- Status: Open
- Date: 2026-03-10
- Theme: Merchant payout account storage and reference pattern (NZ + global)
- Area: Data model / account storage

Context:
- Pay by Bank needs a durable pattern for storing and referencing merchant payout account details across NZ and any future global implementations.
- The main architectural question is whether A2A should rely on a CoA reference, store full bank details itself, or use a hybrid snapshot model.

Decision:
- No final storage pattern is accepted yet.
- Current preferred direction is CoA reference only, with optional A2A operational metadata rather than duplicated raw bank details.
- Alternative options still under consideration are full A2A account storage and a hybrid CoA + A2A snapshot.

Rationale:
- Reuses an existing provider pattern already seen in GoCardless / PayPal.
- Avoids unnecessary duplication of bank details if CoA references are sufficient.

Implications:
- API contracts and DB design should remain flexible until this storage pattern is closed.
- Regional exceptions may still be required if the NZ pattern does not hold globally.

Still open:
- Whether CoA reference only is sufficient in practice.
- Whether the NZ pattern should become the global default or allow regional variation.

### PBB-0003 - Organisation Prerequisites for Pay by Bank Availability

- Status: Open
- Date: 2026-03-11
- Theme: Organisation prerequisites and product availability gating
- Area: Eligibility / product availability

Context:
- Pay by Bank should only be surfaced to organisations that meet the baseline product prerequisites required for a viable setup experience.
- This is distinct from beta cohort management or rollout phasing. The question here is what must be true about an organisation before Pay by Bank is shown as an available option at all.

Decision:
- Pay by Bank entry points should be hidden for non-NZ organisations.
- Pay by Bank should only be surfaced when the organisation passes prerequisite checks rather than being shown universally.
- Current direction is that prerequisite checks include a bank feed relationship and at least one bank account that passes valid-account checks.

Rationale:
- Treats availability as a product constraint, not just a rollout tactic.
- Avoids presenting Pay by Bank to organisations that cannot complete setup or use the product meaningfully.

Implications:
- Entry-point visibility logic must evaluate organisation prerequisites before showing setup CTAs.
- Product and engineering need a durable eligibility check that can be reused across invoice, settings, and other entry surfaces.

Still open:
- Exact definition of a valid bank account for prerequisite checks, for example whether it requires `Type="BANK"`, `EnablePayments=true`, NZD currency, or additional criteria.

### PBB-0011 - DB Schema for Settlement Account Configuration

- Status: Open
- Date: 2026-03-11
- Theme: Bank account data storage and liability
- Area: Database schema / settlement configuration

Context:
- PBB-0007, PBB-0008, and PBB-0009 require Pay by Bank to store settlement configuration separately from CoA, keep CoA read-only, and reuse CoA naming in the UI.
- The exact DB schema that implements those constraints is not yet finalised.

Decision:
- No final schema is accepted yet.
- The schema must support an org reference, a CoA bank account reference, CoA name/reference data, settlement bank details, and auditability.

Rationale:
- The accepted CoA liability boundary requires Pay by Bank to own settlement configuration in its own DB.
- Schema design must support display, editing, and audit requirements without changing CoA records.

Implications:
- Backend API contracts, migrations, and admin tooling depend on this schema.
- Field constraints, indexes, and audit fields must be finalised before implementation is locked.

Still open:
- Exact fields, constraints, and indexes.
- Audit model and any uniqueness or history requirements.

## Closed decisions

### PBB-0001 - Merchant Settlement Account Prefill Strategy

- Status: Accepted
- Date: 2026-03-10
- Theme: Merchant onboarding and settlement account prefill
- Area: Merchant onboarding UX / account data

Context:
- Onboarding should be fast and should reuse data Xero already holds where possible.
- CoA NZD bank accounts are the closest available source for a settlement-account suggestion, but data quality is not perfect.
- Invoice notes were considered as a possible signal but would add runtime complexity.

Decision:
- Use a CoA NZD bank account as the prefilled suggestion in onboarding when format checks pass.
- Prefill remains editable and must be explicitly reviewed by the merchant before save.
- Store the settlement account at onboarding rather than relying on mutable free-text fields.
- Do not productise invoice-notes extraction for v1.

Rationale:
- Reduces onboarding friction while keeping merchants in control of the final stored account.
- Reuses existing product and API surfaces instead of introducing a new runtime data pipeline.

Implications:
- Merchants can correct poor CoA data during onboarding before Pay by Bank is enabled.
- Settlement-account persistence happens during onboarding, not at payment time.

### PBB-0004 - Merchant Permissions

- Status: Accepted (directional)
- Date: 2026-03-10 (updated 2026-03-11)
- Theme: Roles that can enable/disable Pay by Bank
- Area: Permissions / access control

Context:
- Pay by Bank setup and configuration, including enabling, disabling, and editing settlement account details, requires a defined permission model.
- Existing Xero bank feed authorisation capability provides the closest alignment point for who can manage bank-related configuration.

Decision:
- Align Pay by Bank permissions with the bank feed authorisation capability.
- Minimum direction: users with relevant bank-feed-authorised visibility can access Pay by Bank setup.
- Preferred direction: users who can edit bank feed/payment settings can enable and disable Pay by Bank.
- Do not introduce a new standalone Pay by Bank permission concept.

Rationale:
- Reuses existing access-control concepts instead of creating a bespoke permissions model.
- Keeps bank-related configuration with users who already manage bank and payment settings.

Implications:
- Implementation must map Pay by Bank actions onto existing Xero roles and permissions.
- Permission checks must cover setup access, enable/disable actions, and settlement-account management.

Still open:
- Exact role-name mapping and enforcement detail.

### PBB-0005 - Existing Invoices

- Status: Accepted
- Date: 2026-03-10
- Theme: Behaviour of existing invoices after enablement
- Area: Invoicing / branding theme behaviour

Context:
- Enabling Pay by Bank changes what payment option can appear on invoices tied to a branding theme.
- Existing draft invoices may already reference themes that later gain the custom payment service.

Decision:
- Any invoice using a branding theme that has the custom payment service attached can show Pay by Bank.
- This applies to existing draft invoices and new invoices, subject to branding theme behaviour.
- If another payment service is already active on the branding theme, merchant may need to manually activate or swap Pay by Bank on that theme.

Rationale:
- Keeps invoice behaviour consistent with branding theme configuration rather than invoice creation time.
- Avoids a separate special-case model for existing drafts versus new invoices.

Implications:
- Enabling Pay by Bank can change payment availability on already-created draft invoices.
- Merchants with another active provider on the same theme may need manual theme changes before Pay by Bank appears.

### PBB-0006 - Disable / Wind-back

- Status: Accepted
- Date: 2026-03-10
- Theme: Disable flow and technical wind-back
- Area: Settings / provider lifecycle

Context:
- Merchants need a predictable way to disable Pay by Bank after enablement.
- Disablement should align with existing Online Payments patterns rather than inventing a new provider lifecycle.

Decision:
- Disable behaviour follows the existing Custom URL provider pattern in Online Payments settings.
- Merchant confirms the disable action, then the payment service is removed or deactivated from branding themes per current provider behaviour.
- Provide a clear settings entry point to edit the settlement bank account.

Rationale:
- Reuses an established settings flow that merchants and engineering already understand.
- Minimises bespoke disable and wind-back behaviour.

Implications:
- Pay by Bank settings must surface both disable and settlement-account management entry points.
- Theme detachment behaviour should match current provider conventions rather than a custom Pay by Bank rule.

### PBB-0007 - Bank Account Storage and CoA Liability Boundary

- Status: Accepted
- Date: 2026-03-11
- Theme: Bank account data storage and liability
- Area: Data storage / liability boundary

Context:
- Xero Chart of Accounts (CoA) bank account records are meaningful for accounting, but data quality is not reliable enough to use directly as the settlement account for Pay by Bank.
- During onboarding, merchants may need to enter or correct settlement bank account details that differ from what is stored in the CoA.

Decision:
- Pay by Bank will store settlement bank account details in its own application database and will not write those details back to the Xero Chart of Accounts.
- The Pay by Bank DB will hold a reference to the selected CoA bank account, including its name/reference, plus the merchant's chosen settlement bank account details entered during onboarding.

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
- Area: CoA edit boundaries

Context:
- Pay by Bank onboarding and settings need to read CoA bank accounts to let merchants choose a settlement account.
- There is concern about liability and data integrity if the Pay by Bank UI can change CoA records.

Decision:
- The Pay by Bank UI must never directly edit Xero Chart of Accounts bank records.
- Any changes to CoA accounts, including bank account details and labels, must go through existing Xero bank account / CoA flows outside Pay by Bank.

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
- Area: Settlement account labelling

Context:
- During onboarding, merchants choose a settlement account. There is both the CoA bank account name/reference and the settlement bank account details stored by Pay by Bank.
- Customers are already familiar with the CoA bank account name.

Decision:
- Pay by Bank will reuse the existing bank account name/reference from the Xero Chart of Accounts as the user-facing label for the settlement account.
- The Pay by Bank DB will store a reference to that CoA bank account, including its name/reference, plus the underlying settlement details.
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
- Area: Branding themes / payment method behaviour

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
- Requiring an explicit merchant choice per branding theme gives a cleaner behavioural model than automatic provider switching.

Implications:
- Merchants with both Stripe and Pay by Bank enabled will have some manual work: they must adjust branding themes or invoice branding choices to switch between Stripe and Pay by Bank in Slot 1.
- UI copy and onboarding must clearly explain the Slot 1 trade-off and the manual steps for merchants who already use Stripe.
