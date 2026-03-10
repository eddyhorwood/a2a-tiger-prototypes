---
purpose: "Lightweight decision log for Pay by Bank / Safer A2A"
audience: "Product, Design, Engineering, Risk, CX, and AI/LLM agents"
last_updated: "2026-03-10"
status: "current"
---

# Pay by Bank / Safer A2A Decision Log

Lightweight decision log for Pay by Bank / Safer A2A NZ and global rollout.

## How to use this doc

- Changelog gives the latest status for each decision ID.
- Open decisions capture active alignment topics.
- Closed decisions capture aligned outcomes, rationale, and follow-ups.
- Reference decisions from specs, tickets, and designs using the decision ID (for example `PBB-0005`).

## Changelog (summary)

| Date | Decision ID | Title | Area | Status |
|---|---|---|---|---|
| 2026-03-10 | PBB-0001 | Merchant Settlement Account Prefill Strategy | Merchant onboarding | Accepted |
| 2026-03-10 | PBB-0002 | Merchant Payout Bank Account Storage Pattern | Payouts / global data model | Open |
| 2026-03-10 | PBB-0003 | Launch Surfaces & Cohorts | Launch surfaces & rollout | Open (updated with aligned UX defaults) |
| 2026-03-10 | PBB-0004 | Merchant Permissions | Roles and permissions | Accepted (directional, role mapping follow-up) |
| 2026-03-10 | PBB-0005 | Existing Invoices | Invoicing behaviour | Accepted |
| 2026-03-10 | PBB-0006 | Disable / Wind-back | Settings, risk, wind-back | Accepted |

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

Next steps:
- Produce final surface matrix and phased rollout plan.
- Review with Product, Design, CX, Risk, Payments.
- Close decision once rollout plan and cohort definitions are signed off.

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
- Date: 2026-03-10
- Theme: Roles that can enable/disable Pay by Bank

Decision:
- Permission model should align to bank feed authorisation capability.
- Minimum direction: users with relevant bank-feed-authorised visibility should be able to access setup.
- Preferred direction: users who can edit bank feed/payment settings can enable/disable Pay by Bank.

Follow-up:
- Map this direction to exact Xero role names and enforce in permission checks.

### PBB-0005 - Existing Invoices

- Status: Accepted
- Date: 2026-03-10
- Theme: Behaviour of existing invoices after enablement

Decision:
- Any invoice using a branding theme that now has the custom payment service attached can show Pay by Bank.
- This applies to existing draft invoices and new invoices, subject to branding theme behaviour.
- If another payment service is active on the branding theme (for example Stripe), merchant may need to manually activate/swap Pay by Bank on that theme.

Follow-up:
- Validate exact behaviour across invoice states in implementation matrix.

### PBB-0006 - Disable / Wind-back

- Status: Accepted
- Date: 2026-03-10
- Theme: Disable flow and technical wind-back

Decision:
- Disable behaviour follows existing Custom URL provider pattern in Online Payments settings.
- Merchant confirms disable action, then payment service is removed/deactivated from branding themes per current provider behaviour.
- Provide a clear settings entry point to edit settlement bank account.

Follow-up:
- Finalise technical wind-back runbook and ownership for operational triggers.
- Confirm legal/risk requirements for retention and audit trail behaviour.
