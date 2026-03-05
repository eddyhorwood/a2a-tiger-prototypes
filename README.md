# A2A Tiger Prototypes

React + TypeScript prototypes for exploring A2A (Account-to-Account) bank payment onboarding flows for Xero.

## 🚀 Quick Start

```bash
cd A2APaymentsApp/ClientApp
npm install
npm run dev
```

Then open http://localhost:5173

**First-time users:** You'll land on the **Prototype Setup** page where you can configure your test scenario (Stripe status, bank accounts, flow variant, etc.) before exploring onboarding flows.

## 🎛️ Configuration System

The prototype includes a flexible configuration system that lets you test different merchant scenarios without rebuilding mock data:

### 7 Configuration Dimensions
- **Stripe Status**: None, Configured, Competitor
- **Bank Accounts**: None, Single, Multiple
- **A2A Onboarding Status**: Not Started, In Progress, Completed
- **Flow Variant**: Balanced (multi-step) or Aggressive (single-step)
- **Business Type**: Sole Trader, Small Business, Enterprise
- **Region Eligibility**: Eligible, Ineligible
- **Other Payment Methods**: None, Direct Debit, Multiple Providers

### 7 Preset Scenarios
Quick-load configurations for common testing scenarios:
- **Fresh Start** - Brand new merchant, no payments configured
- **Stripe Competitor** - Existing Stripe customer considering A2A
- **Complex OPMM** - Multiple providers (Stripe + Direct Debit + A2A)
- **Needs Completion** - Started A2A onboarding but didn't finish
- **Power User** - Large business with multiple bank accounts
- **Ineligible Org** - Business not eligible for A2A (e.g., wrong region)
- **Fast Onboarding** - Aggressive flow for quick testing

See [docs/guides/prototype-configuration.md](docs/guides/prototype-configuration.md) for detailed usage and extension guide.

## 📖 Documentation

**For AI/LLM agents:** Start with [docs/INDEX.md](docs/INDEX.md) - a complete manifest of all documentation with guidance on when to use each document.

**For humans:** Start with [docs/guides/team-setup.md](docs/guides/team-setup.md) for setup and development instructions.

### Quick Links

#### Guides
- [Prototype Configuration](docs/guides/prototype-configuration.md) - Configuration system usage and extension
- [Team Setup](docs/guides/team-setup.md) - Development environment setup

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

### Getting Started

1. **Configure your scenario**: http://localhost:5173/
   - Choose a preset (e.g., "Fresh Start", "Stripe Competitor")
   - Or customize individual dimensions (Stripe status, bank accounts, flow variant, etc.)
   
2. **Explore entry points**: http://localhost:5173/demo
   - View all entry points (invoice, settings, payment terms)
   - See your active configuration
   - Jump to any entry point with your chosen configuration

### Entry Points

Once configured, test onboarding from:

1. **Invoice page**: http://localhost:5173/invoice/INV-001
2. **Settings page**: http://localhost:5173/settings/online-payments

All entry points respect your chosen configuration (flow variant, merchant state, etc.).

### Flow Variants

- **Balanced** (multi-step): Business details → Bank account → Compliance → Done
- **Aggressive** (single-step): Bank account → Done (with inline editing)

Choose your flow variant in the configuration page. See [docs/specifications/flow-variants.md](docs/specifications/flow-variants.md) for detailed comparison.

## 🏗️ Project Structure

```
A2APaymentsApp/ClientApp/
├── src/
│   ├── pages/
│   │   ├── PrototypeSetup.tsx             # Configuration landing page
│   │   ├── DemoLanding.tsx                # Entry points overview
│   │   ├── OnboardingRouter.tsx           # Smart router (respects config)
│   │   ├── InvoiceDetail.tsx              # Invoice entry point
│   │   ├── OnlinePaymentsSettings.tsx     # Settings entry point
│   │   ├── OnboardingWizardBalanced.tsx   # Multi-step flow
│   │   ├── OnboardingWizardAggressive.tsx # Single-step flow
│   │   └── ...
│   ├── config/
│   │   └── PrototypeConfigContext.tsx     # Configuration state management
│   ├── components/
│   │   ├── ComplianceDisclosure.tsx       # Compliance variants
│   │   └── ...
│   ├── types/
│   │   ├── PrototypeConfig.ts             # Configuration types & presets
│   │   └── EntryContext.ts                # Entry point tracking
│   └── mocks/
│       └── xeroOrgData.ts                 # Mock data
├── package.json
└── vite.config.ts
```

### Key Architecture Components

- **PrototypeConfigContext**: Global configuration provider with localStorage persistence
- **OnboardingRouter**: Smart routing component that respects flow variant configuration
- **7 Presets**: Quick-load scenarios for common merchant situations
- **Derived State Hooks**: `useConfigState()` provides computed boolean checks (e.g., `isEligible`, `hasStripe`, `isAggressiveFlow`)

## 🤝 Contributing

This is a prototype for exploration and iteration. See [docs/guides/team-setup.md](docs/guides/team-setup.md) for:
- Working with AI agents (Copilot/Claude)
- Common tasks and troubleshooting
- How to make changes

## 📝 Original .NET Sample App

This project was originally a .NET MVC OAuth sample app. The React prototype lives in `A2APaymentsApp/ClientApp/`. The .NET backend is not required to run the prototype.

See [docs/archive/original-dotnet-readme.md](docs/archive/original-dotnet-readme.md) for the original .NET setup instructions.
