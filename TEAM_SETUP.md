# A2A Tiger Prototype - Team Setup Guide

## 🎯 What is this?

This is a **clickable prototype** for exploring A2A (Account-to-Account) bank payment onboarding flows for Xero. It lets you test two different onboarding approaches:

- **Balanced Flow**: Multi-step onboarding with business details, directors, and compliance (3-5 min)
- **Aggressive Flow**: Single-step bank account selection (30 sec)

The prototype uses **XUI components** and **Xero Design Guidelines** for an authentic production-like experience.

---

## 🚀 Quick Start

### Prerequisites

- **Node.js 18+** ([Download](https://nodejs.org/))
- **Git** (already have it if you cloned this repo)
- **VS Code** (recommended for AI agent assistance)

### Setup Steps

1. **Clone the repository** (if you haven't already):
   ```bash
   git clone https://github.com/xero-internal/A2APaymentsNZ.git
   cd A2APaymentsNZ
   ```

2. **Navigate to the React app**:
   ```bash
   cd A2APaymentsApp/ClientApp
   ```

3. **Install dependencies**:
   ```bash
   npm install
   ```

4. **Start the dev server**:
   ```bash
   npm run dev
   ```

5. **Open in browser**:
   ```
   http://localhost:5173
   ```

---

## 🧭 Testing the Prototype

### Entry Points

1. **Invoice Page** → Click "Set up online payments"
   - URL: `http://localhost:5173/invoice/INV-001`
   - Mimics the merchant's first-time setup from an invoice

2. **Settings Page** → Click "Set up bank payments"
   - URL: `http://localhost:5173/settings/online-payments`
   - Mimics the settings-based onboarding flow

### Prototype Controls

In the modal that appears, you'll see **Prototype settings** (collapsible):

- **Flow length**: Toggle between Balanced (long) and Aggressive (fast)
- **Compliance disclosure**: Choose how compliance info is shown (modal/banner/fullscreen)

### Test Scenarios

#### Scenario 1: Fast Onboarding (Aggressive)
1. Go to invoice page
2. Click "Set up online payments"
3. Expand "Prototype settings" → Select **Short (30 sec)**
4. Click "Get set up with Akahu"
5. Select a bank account from the dropdown
6. Review details (read-only)
7. Click "Edit" to modify account details (inline editing)
8. Click "Confirm and continue"

#### Scenario 2: Full Onboarding (Balanced)
1. Go to settings page
2. Click "Set up bank payments"
3. Keep "Prototype settings" → **Long (3-5 min)** selected
4. Click "Get set up with Akahu"
5. Walk through multi-step wizard:
   - Business details
   - Directors/owners
   - Select bank account
   - Compliance acknowledgments
   - Review and confirm

---

## 🛠️ Working with AI Agents

This prototype was built using **GitHub Copilot** and Claude. To iterate on the prototype with AI assistance:

### Using GitHub Copilot Chat in VS Code

1. Open the project in VS Code
2. Open Copilot Chat (⌘+Shift+I on Mac)
3. Example prompts:
   - "Add a tooltip explaining what 'Settlement account' means"
   - "Make the success screen display the bank account name"
   - "Add a loading spinner when selecting a bank account"

### Using Claude Desktop

If your team has Claude Desktop set up:
1. Open Claude Desktop
2. Reference this workspace
3. Example requests:
   - "Can you add validation that BSB must be 6 digits?"
   - "Update the modal heading to match the latest design"
   - "Add a 'Skip for now' option to the aggressive flow"

---

## 📂 Project Structure

```
A2APaymentsApp/ClientApp/
├── src/
│   ├── pages/
│   │   ├── InvoiceDetail.tsx           # Invoice entry point
│   │   ├── InvoiceDetail.css
│   │   ├── OnlinePaymentsSettings.tsx  # Settings entry point
│   │   ├── OnboardingWizardBalanced.tsx    # Multi-step flow
│   │   ├── OnboardingWizardAggressive.tsx  # Single-step flow
│   │   └── ...
│   ├── components/
│   │   ├── ComplianceDisclosure.tsx    # Compliance modals/banners
│   │   └── ...
│   ├── types/
│   │   └── EntryContext.ts             # Tracks where user came from
│   └── data/
│       └── xeroOrgData.ts              # Mock data (bank accounts, etc.)
├── package.json
└── vite.config.ts
```

### Key Files to Know

- **OnboardingWizardAggressive.tsx**: The simplified fast flow (most recent work)
- **InvoiceDetail.tsx**: Invoice page with modal entry point
- **xeroOrgData.ts**: Mock Xero organization and bank account data
- **ComplianceDisclosure.tsx**: AML/KYC compliance UI variants

---

## 🎨 Design System

The prototype uses:
- **@xero/xui** component library (XUIButton, XUISelectBox, XUITextInput, XUIModal)
- **Xero Design Guidelines** (colors, spacing, typography)

See `XERO_DESIGN_GUIDELINES.md` for the full design system reference.

---

## 🔧 Common Tasks

### Adding a new field to the aggressive flow

Edit `OnboardingWizardAggressive.tsx`:
```tsx
// Add state
const [newField, setNewField] = useState('')

// Add input
<XUITextInput
  label="New field"
  value={newField}
  onChange={(e) => setNewField(e.target.value)}
/>
```

### Changing mock bank accounts

Edit `A2APaymentsApp/ClientApp/src/data/xeroOrgData.ts`:
```tsx
export const mockXeroOrg = {
  // Modify organization details
  name: 'Your Company Name',
  // ...
}

export const getEligibleSettlementAccounts = () => [
  // Add/modify bank accounts
  { id: '...', name: '...', code: '...', bankAccountNumberMasked: '...' }
]
```

### Updating the modal heading

Edit `InvoiceDetail.tsx`:
```tsx
<h2 className="x-heading-xl" ...>
  Your New Heading Here
</h2>
```

---

## 🐛 Troubleshooting

### Port 5173 already in use
```bash
lsof -ti:5173 | xargs kill -9
npm run dev
```

### Changes not showing up
- Hard refresh browser: `Cmd+Shift+R` (Mac) or `Ctrl+Shift+R` (Windows)
- Check browser console for errors
- Verify Vite dev server is running and compiled successfully

### TypeScript errors
- Most XUI component prop issues: check `node_modules/@xero/xui/react/*.md` docs
- Common gotchas:
  - `disabled` → `isDisabled` on XUITextInput
  - `helperText` → `hintMessage` on XUITextInput
  - XUISelectBox uses `buttonContent` + `isSelected` pattern, not `value` prop

---

## 📝 Documentation 

- **PROJECT_SUMMARY.md**: Overall project context
- **PROTOTYPE_PRD_V2.md**: Product requirements for the prototype
- **FLOW_VARIANTS.md**: Details on balanced vs aggressive flows
- **MULTI_ENTRY_IMPLEMENTATION.md**: How entry points work
- **XUI_COMPONENT_STANDARDS.md**: XUI component usage guide

---

## 🤝 Contributing

This is a prototype for exploration and iteration. Feel free to:
- Experiment with different flows
- Try new UI patterns
- Test with stakeholders
- Gather feedback and iterate

When you make changes you want to share:
```bash
git add .
git commit -m "Describe your changes"
git push
```

---

## 💬 Questions?

Reach out to the original prototype creator or your team lead. Happy prototyping! 🎉
