# A2A Tiger Prototypes

React + TypeScript prototypes for exploring A2A (Account-to-Account) bank payment onboarding flows for Xero.

## 🚀 Quick Start

```bash
cd A2APaymentsApp/ClientApp
npm install
npm run dev
```

Then open http://localhost:5173

## 📖 Documentation

All documentation is in the [`docs/`](docs/) folder:

- **[TEAM_SETUP.md](docs/TEAM_SETUP.md)** - Start here! Complete setup guide for your team
- **[PROJECT_SUMMARY.md](docs/PROJECT_SUMMARY.md)** - Project overview and architecture
- **[PROTOTYPE_PRD_V2.md](docs/PROTOTYPE_PRD_V2.md)** - Product requirements
- **[FLOW_VARIANTS.md](docs/FLOW_VARIANTS.md)** - Balanced vs Aggressive flow comparison
- **[MULTI_ENTRY_IMPLEMENTATION.md](docs/MULTI_ENTRY_IMPLEMENTATION.md)** - Entry points (invoice, settings)
- **[PAYMENT_ONBOARDING_ENTRY_POINTS.md](docs/PAYMENT_ONBOARDING_ENTRY_POINTS.md)** - Current Xero payment onboarding entry points
- **[XERO_DESIGN_GUIDELINES.md](docs/XERO_DESIGN_GUIDELINES.md)** - Design system reference
- **[XUI_COMPONENT_STANDARDS.md](docs/XUI_COMPONENT_STANDARDS.md)** - XUI component usage

## ⚖️ Compliance & Legal

The [`compliance/`](compliance/) folder contains AML/CFT guardrails and legal requirements:

- **[AML_CFT_LLM_CONTEXT.md](compliance/AML_CFT_LLM_CONTEXT.md)** - Canonical roles and guardrails for AI agents

**Important:** If you're using AI assistance (Copilot, Claude, etc.) to work on A2A features, ensure your agent reads the compliance documentation first. This ensures all designs, copy, and flows maintain NZ legal compliance.

## 🎯 Test the Prototype

### Entry Points

1. **Invoice page**: http://localhost:5173/invoice/INV-001
2. **Settings page**: http://localhost:5173/settings/online-payments

### Flow Variants

- **Balanced** (multi-step): Business details → Bank account → Compliance → Done
- **Aggressive** (single-step): Bank account → Done (with inline editing)

See [FLOW_VARIANTS.md](docs/FLOW_VARIANTS.md) for detailed comparison.

## 🏗️ Project Structure

```
A2APaymentsApp/ClientApp/
├── src/
│   ├── pages/
│   │   ├── InvoiceDetail.tsx              # Invoice entry point
│   │   ├── OnlinePaymentsSettings.tsx     # Settings entry point
│   │   ├── OnboardingWizardBalanced.tsx   # Multi-step flow
│   │   ├── OnboardingWizardAggressive.tsx # Single-step flow
│   │   └── ...
│   ├── components/
│   │   ├── ComplianceDisclosure.tsx       # Compliance variants
│   │   └── ...
│   ├── types/
│   │   └── EntryContext.ts                # Entry point tracking
│   └── mocks/
│       └── xeroOrgData.ts                 # Mock data
├── package.json
└── vite.config.ts
```

## 🤝 Contributing

This is a prototype for exploration and iteration. See [TEAM_SETUP.md](docs/TEAM_SETUP.md) for:
- Working with AI agents (Copilot/Claude)
- Common tasks and troubleshooting
- How to make changes

## 📝 Original .NET Sample App

This project was originally a .NET MVC OAuth sample app. The React prototype lives in `A2APaymentsApp/ClientApp/`. The .NET backend is not required to run the prototype.

See [ORIGINAL_DOTNET_README.md](docs/ORIGINAL_DOTNET_README.md) for the original .NET setup instructions.
