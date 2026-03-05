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

**For AI/LLM agents:** Start with [docs/INDEX.md](docs/INDEX.md) - a complete manifest of all documentation with guidance on when to use each document.

**For humans:** Start with [docs/guides/team-setup.md](docs/guides/team-setup.md) for setup and development instructions.

### Quick Links

#### Specifications
- [Product Requirements](docs/specifications/prototype-prd.md) - Feature scope and user stories
- [Flow Variants](docs/specifications/flow-variants.md) - Balanced vs Aggressive comparison
- [Payment Execution Pattern](docs/specifications/payment-execution-pattern.md) - Payment lifecycle

#### Architecture
- [Project Summary](docs/architecture/project-summary.md) - Overview and file structure
- [Multi-Entry Implementation](docs/architecture/multi-entry-implementation.md) - Entry point system
- [Branding Themes 3-Slot Problem](docs/architecture/branding-themes-3-slot-problem.md) - Xero context

#### Reference
- [Payment Onboarding Entry Points](docs/reference/payment-onboarding-entry-points.md) - Current Xero flows
- [Xero Design Guidelines](docs/reference/xero-design-guidelines.md) - Design system
- [XUI Component Standards](docs/reference/xui-component-standards.md) - Component usage

## ⚖️ Compliance & Legal

The [`compliance/`](compliance/) folder contains AML/CFT guardrails and legal requirements:

- **[AML_CFT_LLM_CONTEXT.md](compliance/AML_CFT_LLM_CONTEXT.md)** - Canonical roles and guardrails for AI agents
- **[README.md](compliance/README.md)** - Compliance documentation guide

**Important:** If you're using AI assistance (Copilot, Claude, etc.) to work on A2A features, ensure your agent reads the compliance documentation first. This ensures all designs, copy, and flows maintain NZ legal compliance.

## 🎯 Test the Prototype

### Entry Points

1. **Invoice page**: http://localhost:5173/invoice/INV-001
2. **Settings page**: http://localhost:5173/settings/online-payments

### Flow Variants

- **Balanced** (multi-step): Business details → Bank account → Compliance → Done
- **Aggressive** (single-step): Bank account → Done (with inline editing)

See [docs/specifications/flow-variants.md](docs/specifications/flow-variants.md) for detailed comparison.

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

This is a prototype for exploration and iteration. See [docs/guides/team-setup.md](docs/guides/team-setup.md) for:
- Working with AI agents (Copilot/Claude)
- Common tasks and troubleshooting
- How to make changes

## 📝 Original .NET Sample App

This project was originally a .NET MVC OAuth sample app. The React prototype lives in `A2APaymentsApp/ClientApp/`. The .NET backend is not required to run the prototype.

See [docs/archive/original-dotnet-readme.md](docs/archive/original-dotnet-readme.md) for the original .NET setup instructions.
