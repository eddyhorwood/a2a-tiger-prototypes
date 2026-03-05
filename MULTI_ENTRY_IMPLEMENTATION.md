# Multi-Entry Onboarding Implementation

## Overview

Successfully implemented **7 different entry points** into the Pay by bank merchant onboarding flow, all routing to a single canonical wizard with context preservation.

## Architecture

### Entry Context System

Created [EntryContext.ts](A2APaymentsApp/ClientApp/src/types/EntryContext.ts) with:
- **Source tracking**: Identifies where user came from (settings, invoice, campaign, etc.)
- **Mode differentiation**: `first_time` vs `manage` flows
- **Return path preservation**: Routes user back to originating surface after completion
- **Metadata support**: Campaign IDs, invoice IDs, task IDs, etc.

### Core Principle

**All entry points → Single onboarding wizard**

No forked flows or duplicated logic. Entry context is passed via URL params and consumed by the wizard to:
- Show conditional messaging based on source
- Track analytics/metrics per entry channel
- Return user to the right place after completion

## Entry Points Implemented

### 1. [Online Payments Settings](A2APaymentsApp/ClientApp/src/pages/OnlinePaymentsSettings.tsx)
**Primary entry point** - Settings → Online Payments
- Pay by bank tile with enabled/disabled states
- Settlement account display when enabled
- "Enable" / "Change settlement account" / "Disable" actions
- Entry identifiers: `settings` (first-time), `manage` (edit)

### 2. [Invoice – Inline Enable Prompt](A2APaymentsApp/ClientApp/src/pages/InvoiceDetail.tsx)
**In-context discovery** - Invoice detail → Payment options section
- Shown when A2A not enabled for org
- High-visibility callout promoting direct bank transfers
- CTA: "Set up Pay by bank"
- Entry identifier: `invoice.inline`

### 3. [Invoice – Payment Method Modal](A2APaymentsApp/ClientApp/src/pages/InvoiceDetail.tsx)
**Payment method selector** - Invoice detail → "Add payment method" modal
- Modal (OPMM pattern) with all available payment services
- Pay by bank as selectable option if not enabled
- Entry identifier: `invoice.modal`

### 4. [Online Payments Banner](A2APaymentsApp/ClientApp/src/pages/OnlinePaymentsSettings.tsx)
**High-contrast promo** - Top of Online Payments settings
- Awareness-focused messaging
- Scrolls to and focuses Pay by bank tile on click
- Dismissible after CTA interaction
- Entry identifier: `banner`

### 5. [One Onboarding](A2APaymentsApp/ClientApp/src/pages/OneOnboarding.tsx)
**Guided setup checklist** - Post-signup onboarding flow
- "Accept direct bank payments" task with description
- Progress bar showing completion %
- Entry identifier: `one_onboarding`
- Metadata: `taskId` for completion tracking

### 6. [Campaign / Deep Link Handler](A2APaymentsApp/ClientApp/src/pages/CampaignEntry.tsx)
**External entry** - Marketing emails, Xero Central CTAs, campaign landing pages
- Loading screen with org switching logic (mocked)
- UTM parameter capture for campaign attribution
- Entry identifier: `campaign`
- Metadata: `campaignId`, `utmSource`, `utmMedium`, `utmContent`

### 7. Manage Flow
**Edit existing setup** - Settings → Pay by bank tile (enabled state) → "Change settlement account"
- Pre-selects current settlement account
- Shows guardrail reminders
- Entry identifier: `manage`, mode: `manage`

## Updated Components

### [OnboardingWizard.tsx](A2APaymentsApp/ClientApp/src/pages/OnboardingWizard.tsx)
- ✅ Parses entry context from URL params
- ✅ Displays entry source banner (for demo visibility)
- ✅ Returns user to `returnTo` path after completion
- ✅ Logs context for debugging/analytics

### [App.tsx](A2APaymentsApp/ClientApp/src/App.tsx)
- ✅ Routes for all 7 entry surfaces
- ✅ Central onboarding wizard route accepting context params
- ✅ [EntryPointsIndex](A2APaymentsApp/ClientApp/src/pages/EntryPointsIndex.tsx) as demo home page

## Testing

### Local Dev Server
App running at: `http://localhost:5173/`

### Demo Flow
1. Visit `/` - see all 7 entry points with descriptions
2. Click any entry → routed to onboarding with correct context
3. Complete onboarding → returned to originating surface
4. Check browser console for entry context logging

### URL Parameters (Examples)
```
/merchant-onboarding?source=settings&mode=first_time&returnTo=/settings/online-payments

/merchant-onboarding?source=invoice.inline&mode=first_time&returnTo=/invoice/INV-001&invoiceId=INV-001

/merchant-onboarding?source=campaign&mode=first_time&returnTo=/settings/online-payments&campaignId=email_nz_a2a_launch&utmSource=campaign_demo

/merchant-onboarding?source=manage&mode=manage&returnTo=/settings/online-payments
```

## Key Technical Decisions

### Why URL Params vs React Context?
- ✅ **Deep linkable**: Campaign emails can link directly with context
- ✅ **Refreshable**: User can reload without losing context
- ✅ **Trackable**: Analytics can easily parse entry source from URL
- ✅ **Stateless**: No need to manage cross-route state

### Why Single Wizard vs Multiple Flows?
- ✅ **DRY**: No duplicated onboarding logic
- ✅ **Consistency**: Same experience regardless of entry
- ✅ **Maintainability**: One place to update flows, copy, guardrails
- ✅ **A/B testable**: Can conditionally vary messaging per entry context

### XUI Component Variants
Fixed all button variants to match XUI library:
- `variant="main"` (previously "primary")
- `variant="standard"` (previously "secondary")
- `variant="borderless-standard"` (previously "tertiary")

## Next Steps (Future Work)

### Backend Integration
- Store entry source in merchant config for analytics
- Track conversion funnel metrics per entry channel
- A/B test messaging variants based on entry context

### Advanced Entry Context
- User segmentation (new vs returning, org size, industry)
- Personalized messaging based on entry metadata
- Smart defaults (e.g., pre-select account if only one eligible)

### One Onboarding Integration
- Follow actual One Onboarding contract/API patterns
- Task completion callbacks
- "Manage" flows from completed task cards

### Real Xero Repos
When integrating into actual Xero codebase:
- Map entry points to real surfaces (e.g., `xero-web-app`, `invoicing-ui`)
- Use Xero's existing routing patterns
- Connect to real merchant config API endpoints
- Wire up telemetry/analytics for entry source tracking

## Files Created/Modified

### New Files
- `src/types/EntryContext.ts` - Entry context system
- `src/pages/EntryPointsIndex.tsx` - Demo home page
- `src/pages/OnlinePaymentsSettings.tsx` - Primary entry (settings)
- `src/pages/InvoiceDetail.tsx` - Invoice entries (inline + modal)
- `src/pages/OneOnboarding.tsx` - Onboarding checklist entry
- `src/pages/CampaignEntry.tsx` - Deep link handler
- Corresponding `.css` files for each page

### Modified Files
- `src/pages/OnboardingWizard.tsx` - Entry context parsing & return routing
- `src/pages/OnboardingWizard.css` - Entry context banner styles
- `src/App.tsx` - Routes for all entry points

## Summary

This implementation demonstrates a **production-ready multi-entry architecture** that:
- ✅ Supports 7 diverse entry flows
- ✅ Preserves source context through URL params
- ✅ Routes all entries to a single canonical onboarding wizard
- ✅ Returns users to originating surfaces after completion
- ✅ Provides clear entry source visibility (for demo/analytics)
- ✅ Follows Xero UX patterns (XUI components, responsive design)
- ✅ TypeScript type-safe with no compile errors
- ✅ Mirrors actual Xero product patterns (settings tiles, invoice prompts, One Onboarding)

The prototype is ready for demo, user testing, and integration planning discussions.
