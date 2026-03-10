# Documentation Index for AI/LLM Agents

## Purpose

This index helps LLMs quickly locate relevant documentation. Each entry specifies:
- **Purpose**: What the document is for
- **When to use**: Specific scenarios triggering its use
- **Audience**: Who/what should read it

---

## Specifications (Product Requirements)

### specifications/prototype-prd.md
- **Purpose**: Product requirements for A2A bank payment onboarding prototype
- **When to use**: Understanding feature scope, user stories, acceptance criteria, design decisions
- **LLM use**: Feature development, scope validation, requirement checking
- **Key topics**: Flow variants (balanced vs aggressive), entry points, compliance disclosures
- **Status**: Current version (replaces archive/prototype-prd-v1.md)

### specifications/flow-variants.md
- **Purpose**: Compares "Balanced" (multi-step) vs "Aggressive" (fast) onboarding flows
- **When to use**: Deciding between flow approaches, understanding tradeoffs, A/B test design
- **LLM use**: Flow design decisions, variant selection, UX reasoning
- **Key topics**: Step-by-step comparison, compliance placement, user friction points

### specifications/payment-execution-pattern.md
- **Purpose**: Technical pattern for payment execution flow with Akahu
- **When to use**: Understanding payment lifecycle, state transitions, webhook handling
- **LLM use**: Payment flow implementation, error handling, status updates
- **Key topics**: Payment initiation, polling, webhooks, reconciliation

### specifications/merchant-onboarding-ui/ux-spec.md
- **Purpose**: UI/UX specification for Pay by Bank merchant onboarding
- **When to use**: Defining onboarding interaction design, entry-point behaviour, and UX edge-case handling
- **LLM use**: UX requirement alignment, flow implementation guidance, copy and state behaviour checks
- **Key topics**: Entry points, modal flow, permissions assumptions, post-enable behaviour, open questions

### specifications/pay-by-bank-decision-log.md
- **Purpose**: Lightweight decision log for Pay by Bank / Safer A2A alignment
- **When to use**: Checking current product decisions, open questions, and accepted outcomes
- **LLM use**: Decision tracing, requirement alignment, avoiding duplicate decisions
- **Key topics**: Decision IDs (`PBB-0001+`), changelog, open vs closed decisions

---

## Architecture (Technical Design)

### architecture/project-summary.md
- **Purpose**: High-level architecture overview of the prototype
- **When to use**: Onboarding to codebase, understanding file structure, architectural patterns
- **LLM use**: Initial codebase orientation, understanding component relationships
- **Key topics**: React + TypeScript structure, XUI components, mock data, entry context system

### architecture/multi-entry-implementation.md
- **Purpose**: Entry point system - how users reach onboarding from different surfaces
- **When to use**: Understanding user journeys, context propagation, entry point design
- **LLM use**: Entry point design, context tracking, flow configuration based on entry
- **Key topics**: Invoice page entry, settings page entry, EntryContext type, flow routing

### architecture/branding-themes-3-slot-problem.md
- **Purpose**: Deep context on branding themes and payment service slot limitations
- **When to use**: Working with invoice templates, payment method configuration, multi-provider scenarios
- **LLM use**: Feature design involving branding themes, understanding slot constraints
- **Key topics**: Branding theme model, 3-slot limitation, OPMM (Online Payment Method Manager)
- **Format**: LLM-optimized (verbose, repetitive, explicitly verbose)
- **Note**: This is Xero product context, not prototype-specific

---

## Reference (Current State Context)

### reference/payment-onboarding-entry-points.md
- **Purpose**: Catalog of all current Xero payment onboarding entry points (web + mobile)
- **When to use**: Understanding current state before designing new flows, gap analysis
- **LLM use**: Context for design decisions, identifying gaps vs current state, competitive analysis
- **Key topics**: Stripe/GoCardless/PayPal entry points, invoice editor, settings, mobile XAA
- **Note**: Current Xero production state, not prototype implementation

### reference/xero-design-guidelines.md
- **Purpose**: Xero design system - colors, typography, spacing, patterns
- **When to use**: Styling components, maintaining design consistency, CSS implementation
- **LLM use**: CSS generation, design token selection, color palette usage
- **Key topics**: CSS variables, 4px grid system, color palette, typography scale, spacing
- **Size**: 1,230 lines (comprehensive reference)

### reference/xui-component-standards.md
- **Purpose**: XUI component library usage patterns and prop conventions
- **When to use**: Implementing UI components, fixing TypeScript errors, component selection
- **LLM use**: Component selection, prop usage, common patterns, avoiding prop mismatches
- **Key topics**: XUIButton, XUISelectBox, XUITextInput, XUIModal, prop patterns
- **Note**: Includes corrections for common TypeScript errors with XUI props

### reference/xero-payment-setup-flow.md
- **Purpose**: Complete context on how Xero payment service setup works in production
- **When to use**: Designing realistic onboarding flows, understanding state transitions, entry point patterns
- **LLM use**: Flow design, state management, provider integration patterns, preconditions
- **Key topics**: 5-phase setup flow, provider state machine, entry points, KYC/KYB, attach patterns
- **Size**: Comprehensive reference with examples (Stripe, GoCardless, A2A patterns)
- **Note**: Production Xero knowledge - use this to inform realistic prototype behavior

---

## Guides (Setup & Development)

### guides/prototype-configuration.md
- **Purpose**: Configuration landing page system for testing different merchant scenarios
- **When to use**: Setting up prototype test scenarios, understanding state management, extending configuration
- **LLM use**: Using `usePrototypeConfig` and `useConfigState` hooks, adding new dimensions
- **Key topics**: Configuration dimensions (Stripe, bank accounts, A2A status, flow variant), presets, localStorage persistence
- **Note**: Critical for adapting mock data and UX based on configured merchant state

### guides/team-setup.md
- **Purpose**: Complete setup guide for team members (human and AI agents)
- **When to use**: Initial project setup, troubleshooting dev environment, common tasks
- **LLM use**: Setup instructions, npm commands, entry points, testing scenarios
- **Key topics**: Quick start, prerequisites, test scenarios, common tasks, AI agent instructions
- **Audience**: Both human developers and AI coding assistants

---

## Archive (Historical/Deprecated)

Documents in `archive/` are kept for reference but should **not** guide new development.

### archive/prototype-prd-v1.md
- **Status**: Superseded by specifications/prototype-prd.md
- **Reason**: Earlier iteration of requirements, not current

### archive/setup-guide-react.md
- **Status**: Consolidated into guides/team-setup.md
- **Reason**: Duplicate information, less comprehensive

### archive/prototype-complete.md
- **Status**: Historical completion status
- **Reason**: Milestone document, not active guidance

### archive/quick-reference.md
- **Status**: .NET-specific references
- **Reason**: References .NET code not used in React prototype

### archive/original-dotnet-readme.md
- **Status**: Original .NET sample app documentation
- **Reason**: Preserved for historical context, not relevant to React prototype

---

## How to Use This Index (LLM Instructions)

### When a user asks about...

**Feature requirements or scope:**
→ Read `specifications/prototype-prd.md`

**Which onboarding flow to use:**
→ Read `specifications/flow-variants.md`

**Payment or webhook implementation:**
→ Read `specifications/payment-execution-pattern.md`

**Project structure or architecture:**
→ Read `architecture/project-summary.md`

**Entry points or user journeys:**
→ Read `architecture/multi-entry-implementation.md`

**Branding themes or payment slots:**
→ Read `architecture/branding-themes-3-slot-problem.md`

**Current Xero payment flows (for context):**
→ Read `reference/payment-onboarding-entry-points.md`

**Design system, colors, spacing:**
→ Read `reference/xero-design-guidelines.md`

**XUI components or TypeScript errors:**
→ Read `reference/xui-component-standards.md`

**Setup, installation, or common tasks:**
→ Read `guides/team-setup.md`

### Before generating code, always check:

1. **Compliance constraints**: ../compliance/AML_CFT_LLM_CONTEXT.md
2. **Design system**: reference/xero-design-guidelines.md
3. **XUI component patterns**: reference/xui-component-standards.md
4. **Entry context requirements**: architecture/multi-entry-implementation.md

---

## Document Metadata Standards

All documents should include this frontmatter for LLM parsing:

```yaml
---
purpose: "Brief description"
audience: "LLM agents, product team, etc."
last_updated: "YYYY-MM-DD"
status: "current | archived | deprecated"
replaces: "filename.md (if applicable)"
---
```

---

## Related Documentation

- **[../compliance/](../compliance/)** - AML/CFT legal guardrails and compliance requirements
- **[../README.md](../README.md)** - Project quick start and overview
