# Quick Start Guide - Enhanced Entry Points

## What Was Built

Two realistic Xero-style entry points for payment setup:

1. **Settings → Payment Services** (Enhanced existing page)
2. **Invoice → Banner** (NEW page)

Plus a demo landing page to showcase both.

---

## How to Demo

### Start the Dev Server

```bash
cd "A2APaymentsApp/ClientApp"
npm run dev
```

Open: **http://localhost:5173/**

---

## Demo Flows

### 1. Landing Page Overview

**URL:** `http://localhost:5173/`

Shows all three entry points with descriptions:
- Entry Point 1: Settings flow (enhanced)
- Entry Point 2: Invoice banner (NEW)
- Entry Point 3: Invoice modal (existing)

Click any "View..." button to explore.

---

### 2. Settings Flow (Enhanced)

**URL:** `http://localhost:5173/settings/online-payments`

**What to show:**
- Three payment providers displayed (Pay by bank, Cards, Direct Debit)
- Pay by bank in `NOT_CONFIGURED` state
- Click "Get set up" → launches onboarding wizard
- Complete onboarding → returns to settings
- Now shows "Connected" badge + settlement account details
- Can click "Manage" or "Disconnect"

**Demo provider states:**
Edit line 49 in `OnlinePaymentsSettings.tsx` to change states:
```tsx
status: 'SETUP_STARTED' // Shows "Resume setup" + "Almost there!" banner
status: 'SETUP_COMPLETE' // Shows "Manage" + account details
status: 'ERROR' // Shows "Fix issues" + error badge
```

---

### 3. Invoice Banner Flow (NEW)

**URL:** `http://localhost:5173/invoice-view/INV-002`

**What to show:**
- Realistic Xero invoice layout
- Blue informational banner at top: "Get paid up to 2× faster..."
- Click "Set up online payments" → launches onboarding
- Complete onboarding → return to invoice
- Banner disappears (payment service now configured)

**Features highlighted:**
- Contextual nudge in-context where merchant works
- Direct flow from invoice to onboarding
- Entry tracking includes invoice metadata
- Dismissible banner option

---

### 4. Compare All Three Entry Points

| Entry Point | URL | When Used | UX Pattern |
|-------------|-----|-----------|------------|
| **Settings** | `/settings/online-payments` | Merchant explicitly looking for payment setup | Payment Services landing page with provider cards |
| **Invoice Banner** (NEW) | `/invoice-view/INV-002` | Merchant working on invoice, hasn't set up payments yet | High-contrast banner above invoice |
| **Invoice Modal** (existing) | `/invoice/INV-001` | Merchant clicking "Set up online payments" inline button | Payment Options Modal (OPMM) |

---

## Key Components Created

### SetupBanner
Reusable banners for prompting payment setup.

**Variants:**
- `info` - Blue, standard informational
- `promotional` - Purple, promotional campaigns
- `high-contrast` - Yellow, urgent attention

**Usage:**
```tsx
<SetupBanner
  variant="info"
  title="Get paid faster"
  description="Add online payments..."
  primaryAction={{ label: "Set up", onClick: handleSetup }}
  onDismiss={handleDismiss}
/>
```

---

## Provider Status States

Settings page shows realistic provider lifecycle:

| State | Badge | CTAs | Displayed Info |
|-------|-------|------|----------------|
| `NOT_CONFIGURED` | None | "Get set up" | Features list, pricing, setup time |
| `SETUP_STARTED` | "Setup started" (yellow) | "Resume setup", "Cancel" | Minimal info |
| `SETUP_COMPLETE` | "Connected" (green) | "Manage", "Disconnect" | Settlement account, fee account |
| `ERROR` | "Action required" (red) | "Fix issues", "Disconnect" | Error details |

---

## Design Compliance

✅ **Xero Design Guidelines:**
- CSS variables for all colors
- 4px spacing grid throughout
- XUI components only (no native HTML form elements)
- Proper typography scale
- Consistent border radius and shadows

✅ **AML/CFT Compliance:**
- No "Xero transfers funds" language
- Clear "Powered by Akahu" attribution
- No fund custody claims
- Proper role descriptions

---

## Files Structure

```
src/
├── components/
│   ├── SetupBanner.tsx         # NEW reusable banner component
│   └── SetupBanner.css
├── pages/
│   ├── DemoLanding.tsx         # NEW landing page
│   ├── DemoLanding.css
│   ├── InvoiceView.tsx         # NEW invoice with banner
│   ├── InvoiceView.css
│   ├── OnlinePaymentsSettings.tsx  # ENHANCED with states
│   └── OnlinePaymentsSettings.css  # ENHANCED styling
└── App.tsx                     # Updated routing
```

---

## Documentation

- **[IMPLEMENTATION_SUMMARY.md](./IMPLEMENTATION_SUMMARY.md)** - Full completion summary
- **[ENHANCED_ENTRY_POINTS.md](./ENHANCED_ENTRY_POINTS.md)** - Detailed implementation guide
- **[.github/copilot-instructions.md](./.github/copilot-instructions.md)** - AI agent rules

---

## Next Steps

1. **Demo to stakeholders** using landing page as starting point
2. **Gather feedback** on which entry points feel most natural
3. **Iterate on banner copy** based on user research
4. **Add more entry points** (dashboard, invoice list, repeating invoices)
5. **Test with real users** to measure conversion rates

---

## Need Help?

- **Compile errors?** Run `npm run build` to check
- **Port in use?** Kill process: `lsof -ti:5173 | xargs kill -9`
- **State not changing?** Edit `OnlinePaymentsSettings.tsx` line 49
- **Banner not showing?** Check InvoiceView.tsx line 30 - `paymentServiceConfigured` should be `false`

---

## Status

✅ All components built and tested  
✅ Zero TypeScript/ESLint errors  
✅ Design system compliant  
✅ AML/CFT compliant  
✅ Committed to git (commit aa90081)  
✅ Pushed to GitHub  

**Ready for demo! 🚀**
