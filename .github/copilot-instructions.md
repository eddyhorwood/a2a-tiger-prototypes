# GitHub Copilot Instructions for A2A Tiger Prototype

## Overview

You are working on the A2A Tiger prototype - a React + TypeScript application for Xero's Account-to-Account bank payment onboarding flows. This repository has strict compliance, design, and component standards that **must be followed**.

---

## CRITICAL: Read Documentation First

Before generating ANY code, designs, or copy:

1. **Start here**: Read [docs/INDEX.md](../docs/INDEX.md) to find relevant documentation for your task
2. **For payment flows**: Read [compliance/AML_CFT_LLM_CONTEXT.md](../compliance/AML_CFT_LLM_CONTEXT.md) - legal guardrails (MANDATORY)
3. **For styling**: Read [docs/reference/xero-design-guidelines.md](../docs/reference/xero-design-guidelines.md) - design system
4. **For components**: Read [docs/reference/xui-component-standards.md](../docs/reference/xui-component-standards.md) - XUI patterns

---

## HARD CONSTRAINTS (Cannot Violate)

### 1. AML/CFT Compliance (Legal Requirements)

When working on payment flows, copy, or UX involving money movement:

**PROHIBITED LANGUAGE - NEVER USE:**
- ❌ "Xero transfers funds"
- ❌ "Akahu transfers money"
- ❌ "Payment processed by Xero"
- ❌ "Xero holds your funds"
- ❌ "We'll transfer the money to..."
- ❌ "Bank transfer by Akahu"

**REQUIRED LANGUAGE - ALWAYS USE:**
- ✅ "Bank transfer initiated by Akahu"
- ✅ "Your bank executes the transfer"
- ✅ "Neither Xero nor Akahu takes custody of funds"
- ✅ "Direct bank-to-bank transfer"

**Architecture Constraints:**
- Xero NEVER holds, pools, or manages customer funds
- NO intermediate settlement accounts controlled by Xero or Akahu
- Payment flow is always: `Payer Bank → Merchant Bank` (direct)
- Xero role: instruction collector + ledger updater ONLY
- Akahu role: payment initiator via Open Banking APIs ONLY

**When in doubt**: Read [compliance/AML_CFT_LLM_CONTEXT.md](../compliance/AML_CFT_LLM_CONTEXT.md) sections 4 and 8.

---

### 2. Design System (Xero Design Guidelines)

**Colors - ALWAYS use CSS variables:**
```css
/* ✅ CORRECT */
color: var(--xui-color-text-primary);
background: var(--xui-color-surface-raised);
border-color: var(--xui-color-border-default);

/* ❌ NEVER DO THIS */
color: #2E3944;
background: #ffffff;
border-color: #E0E0E0;
```

**Available color tokens:**
- Text: `--xui-color-text-{primary|secondary|tertiary|inverse|disabled|link|danger|success}`
- Surface: `--xui-color-surface-{default|raised|overlay|subtle|brand|danger}`
- Border: `--xui-color-border-{default|subtle|strong|brand|danger}`
- Status: `--xui-color-{blue|purple|turquoise|green|orange|red|pink|yellow}-{50-900}`

**Spacing - ALWAYS use 4px grid:**
```css
/* ✅ CORRECT - multiples of 4px */
margin: 8px;
padding: 16px 24px;
gap: 12px;

/* ❌ NEVER DO THIS */
margin: 10px;
padding: 15px 20px;
gap: 13px;
```

**Common spacing values:** 4px, 8px, 12px, 16px, 20px, 24px, 32px, 40px, 48px, 64px

**Typography:**
```css
/* ✅ CORRECT - use design system classes or variables */
font-family: var(--xui-font-family-sans);
font-size: var(--xui-font-size-body-md);
line-height: var(--xui-line-height-body);

/* ❌ NEVER DO THIS */
font-family: 'Arial', sans-serif;
font-size: 14px;
line-height: 1.5;
```

**Reference**: [docs/reference/xero-design-guidelines.md](../docs/reference/xero-design-guidelines.md) (1,230 lines)

---

### 3. XUI Components (Mandatory Component Library)

**ALWAYS use XUI components from `@xero/xui` - NEVER use native HTML elements:**

```tsx
// ✅ CORRECT
import { XUIButton, XUITextInput, XUISelectBox } from '@xero/xui';

<XUIButton variant="primary" onClick={handleClick}>
  Continue
</XUIButton>

<XUITextInput
  label="Account Number"
  value={accountNumber}
  onChange={setAccountNumber}
  isDisabled={loading}
  hintMessage="Enter your bank account number"
/>

<XUISelectBox label="Bank">
  <XUISelectBoxOption value="anz">ANZ</XUISelectBoxOption>
  <XUISelectBoxOption value="bnz">BNZ</XUISelectBoxOption>
</XUISelectBox>

// ❌ NEVER DO THIS
<button onClick={handleClick}>Continue</button>
<input type="text" disabled={loading} />
<select><option value="anz">ANZ</option></select>
```

**Critical Prop Differences (Common TypeScript Errors):**
- ✅ `isDisabled` not `disabled`
- ✅ `hintMessage` not `helperText` or `hint`
- ✅ `id` is required for form elements
- ✅ XUISelectBoxOption uses `buttonContent` + `isSelected` pattern

**Available XUI Components:**
- Form: `XUIButton`, `XUITextInput`, `XUISelectBox`, `XUISelectBoxOption`, `XUICheckbox`, `XUIRadio`
- Layout: `XUIModal`, `XUIModalBody`, `XUICard`, `XUIBanner`
- Navigation: `XUITabs`, `XUIBreadcrumbs`
- Feedback: `XUILoader`, `XUIToast`, `XUIProgressBar`

**Reference**: [docs/reference/xui-component-standards.md](../docs/reference/xui-component-standards.md)

---

### 4. React + TypeScript Patterns

**Entry Context System:**
All onboarding flows must use EntryContext to track how users arrived:

```tsx
import { parseEntryContext, type EntryContext } from '../types/EntryContext';

// In page component
const searchParams = new URLSearchParams(window.location.search);
const entryContext = parseEntryContext(searchParams);

// Pass to wizard
<OnboardingWizard entryContext={entryContext} />
```

**Entry points:**
- `invoice` - User came from invoice page
- `settings` - User came from settings page

**Reference**: [docs/architecture/multi-entry-implementation.md](../docs/architecture/multi-entry-implementation.md)

---

## MANDATORY PRE-GENERATION CHECKLIST

Before generating code, verify:

- [ ] **Read docs/INDEX.md** to find relevant documentation
- [ ] **If payment flow**: Reviewed compliance/AML_CFT_LLM_CONTEXT.md §4 (copy rules)
- [ ] **If styling**: Using CSS variables from docs/reference/xero-design-guidelines.md
- [ ] **If UI components**: Using XUI components from docs/reference/xui-component-standards.md
- [ ] **If new flow**: Checked docs/specifications/flow-variants.md for patterns
- [ ] **If new entry point**: Documented in docs/architecture/multi-entry-implementation.md

---

## Common Tasks Quick Reference

### "Add a new onboarding step"
1. Read: [docs/specifications/flow-variants.md](../docs/specifications/flow-variants.md)
2. Pattern: See OnboardingWizardBalanced.tsx or OnboardingWizardAggressive.tsx
3. Use: XUI components only
4. Style: CSS variables only

### "Update payment flow copy"
1. Read: [compliance/AML_CFT_LLM_CONTEXT.md §4](../compliance/AML_CFT_LLM_CONTEXT.md)
2. Verify: No "Xero transfers" language
3. Use: "Bank transfer initiated by Akahu" pattern

### "Style a component"
1. Read: [docs/reference/xero-design-guidelines.md](../docs/reference/xero-design-guidelines.md)
2. Use: `var(--xui-color-*)` for colors
3. Use: 4px grid for spacing (8px, 12px, 16px, etc.)

### "Add a form field"
1. Read: [docs/reference/xui-component-standards.md](../docs/reference/xui-component-standards.md)
2. Use: `XUITextInput` or `XUISelectBox`
3. Props: `isDisabled` (not disabled), `hintMessage` (not helperText)

### "Understand project structure"
1. Read: [docs/architecture/project-summary.md](../docs/architecture/project-summary.md)
2. Entry points: invoice page, settings page
3. Flows: Balanced (multi-step), Aggressive (single-step)

---

## When You're Uncertain

**If you're unsure about compliance**:
→ STOP. Flag the issue. Read [compliance/AML_CFT_LLM_CONTEXT.md](../compliance/AML_CFT_LLM_CONTEXT.md) fully.

**If you're unsure about design**:
→ Use CSS variables. Check [docs/reference/xero-design-guidelines.md](../docs/reference/xero-design-guidelines.md).

**If you're unsure about components**:
→ Use XUI. Check [docs/reference/xui-component-standards.md](../docs/reference/xui-component-standards.md).

**If you're unsure what documentation exists**:
→ Read [docs/INDEX.md](../docs/INDEX.md) first.

---

## Error Prevention

**TypeScript errors with XUI components?**
- Check prop names: `isDisabled` not `disabled`
- Ensure `id` prop is provided for form elements
- Import from `@xero/xui` not `@xero/xui/react`

**CSS not working?**
- Check for hardcoded colors - use CSS variables
- Check spacing is on 4px grid
- Ensure design tokens are loaded (index.css imports XUI styles)

**Compliance concerns?**
- Avoid fund transfer language
- Always say "initiated by Akahu" not "by Akahu"
- State that Xero doesn't hold funds

---

## Documentation Structure

```
docs/
├── INDEX.md                                    # Start here - doc manifest
├── specifications/                             # Product requirements
│   ├── prototype-prd.md
│   ├── flow-variants.md
│   └── payment-execution-pattern.md
├── architecture/                               # Technical design
│   ├── project-summary.md
│   ├── multi-entry-implementation.md
│   └── branding-themes-3-slot-problem.md
├── reference/                                  # Current state context
│   ├── payment-onboarding-entry-points.md
│   ├── xero-design-guidelines.md
│   └── xui-component-standards.md
├── guides/                                     # Setup & dev
│   └── team-setup.md
└── archive/                                    # Historical (ignore)

compliance/
├── AML_CFT_LLM_CONTEXT.md                     # Legal guardrails (CRITICAL)
└── README.md
```

---

## Summary: The Three Rules

1. **Compliance First**: If touching payment flows → read compliance docs, avoid "Xero transfers funds" language
2. **Design System Always**: Use CSS variables for colors, 4px grid for spacing
3. **XUI Components Only**: No native HTML form elements, use @xero/xui components

When in doubt, read [docs/INDEX.md](../docs/INDEX.md) to find the right documentation for your task.
