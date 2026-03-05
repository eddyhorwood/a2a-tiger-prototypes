# XUI Component Standards

**Purpose:** This document ensures all React components in this prototype follow Xero Design Language (XDL) patterns and use `@xero/xui` components consistently.

---

## ✅ Mandatory Checklist for Every New Component

Before marking any UI work as complete, verify:

- [ ] **No raw HTML buttons** - Always use `XUIButton`
- [ ] **No custom form inputs** - Use XUI form components
- [ ] **Typography uses XUI classes** - Not custom CSS font sizes
- [ ] **Spacing uses XUI classes** - Not custom margin/padding CSS
- [ ] **Layouts use XUI compositions** - When available (e.g., `XUICompositionDetail`)
- [ ] **Colors reference XERO_DESIGN_GUIDELINES.md** - Not arbitrary hex codes
- [ ] **Interactive states are XUI-native** - Hover, focus, disabled handled by XUI

---

## 📦 Required XUI Component Imports

### Buttons (ALWAYS use these)
```tsx
import XUIButton from '@xero/xui/react/button'

// Usage:
<XUIButton variant="main" onClick={handleClick}>Primary Action</XUIButton>
<XUIButton variant="standard" onClick={handleClick}>Secondary</XUIButton>
<XUIButton variant="borderless-main" onClick={handleClick}>Link-style</XUIButton>
<XUIButton variant="negative" onClick={handleClick}>Delete</XUIButton>

// With disabled state (use isDisabled, not disabled):
<XUIButton variant="main" onClick={handleClick} isDisabled={!isValid}>Submit</XUIButton>
```

**Important XUIButton props:**
- `variant`: "main" | "standard" | "borderless-main" | "borderless-standard" | "negative" | etc.
- `isDisabled`: boolean (NOT `disabled`)
- `onClick`: () => void
- `type`: "button" | "submit" (for forms)

### Steppers
```tsx
import XUIStepper from '@xero/xui/react/stepper'

const steps = [
  { name: 'Step 1', isComplete: true, isDisabled: false },
  { name: 'Step 2', isComplete: false, isDisabled: false },
  { name: 'Step 3', isComplete: false, isDisabled: true }
]

<XUIStepper
  steps={steps}
  currentStep={1}
  onStepClick={(index) => handleStepChange(index)}
/>
```

### Layouts
```tsx
import { XUICompositionDetail } from '@xero/xui/react/compositions'

<XUICompositionDetail
  heading="Page Title"
  description="Optional description"
  actions={<XUIButton variant="main">Action</XUIButton>}
>
  {/* Page content */}
</XUICompositionDetail>
```

### Modals
```tsx
import XUIModal, { 
  XUIModalHeader, 
  XUIModalHeading, 
  XUIModalBody, 
  XUIModalFooter 
} from '@xero/xui/react/modal'

<XUIModal isOpen={isOpen} onClose={handleClose}>
  <XUIModalHeader>
    <XUIModalHeading>Title</XUIModalHeading>
  </XUIModalHeader>
  <XUIModalBody>Content</XUIModalBody>
  <XUIModalFooter>
    <XUIButton variant="main" onClick={handleConfirm}>Confirm</XUIButton>
  </XUIModalFooter>
</XUIModal>
```

### Loaders
```tsx
import XUILoader from '@xero/xui/react/loader'

<XUILoader size="medium" />
```

---

## 🎨 XUI Class Names (Use Instead of Custom CSS)

### Typography
```tsx
// Headings
className="xui-heading-xlarge"    // Page titles
className="xui-heading-large"     // Section headings
className="xui-heading-medium"    // Card titles
className="xui-heading-small"     // Sub-headings

// Body text
className="xui-font-size-large"   // Emphasized body
className="xui-font-size-medium"  // Default body (usually omit)
className="xui-font-size-small"   // Helper text
```

### Spacing
```tsx
// Margins
className="xui-margin-top-large"
className="xui-margin-bottom-medium"
className="xui-margin-left-small"
className="xui-margin-right-xlarge"

// Padding
className="xui-padding-small"
className="xui-padding-medium"
className="xui-padding-large"

// Combined
className="xui-margin-bottom-large xui-padding-medium"
```

### Layout Utilities
```tsx
className="xui-page-width-standard"  // Max-width container
className="xui-list"                 // Styled lists
```

---

## 🚫 Anti-Patterns (NEVER Do These)

### ❌ Raw HTML Buttons
```tsx
// WRONG
<button className="btn-primary" onClick={handleClick}>Click me</button>

// CORRECT
<XUIButton variant="main" onClick={handleClick}>Click me</XUIButton>
```

### ❌ Custom Typography CSS
```tsx
// WRONG
<h1 style={{ fontSize: '24px', fontWeight: 600 }}>Title</h1>

// CORRECT
<h1 className="xui-heading-xlarge">Title</h1>
```

### ❌ Custom Spacing Values
```tsx
// WRONG
<div style={{ marginBottom: '20px' }}>Content</div>

// CORRECT
<div className="xui-margin-bottom-large">Content</div>
```

### ❌ Arbitrary Color Codes
```tsx
// WRONG
<div style={{ background: '#1E90FF' }}>Content</div>

// CORRECT - Reference XERO_DESIGN_GUIDELINES.md
<div style={{ background: 'var(--xero-color-blue)' }}>Content</div>
```

### ❌ Custom Input Components
```tsx
// WRONG
<input type="text" className="custom-input" />

// CORRECT - Use XUI form components (check @xero/xui docs)
// For prototype exceptions, at minimum use xui-* classes
```

---

## 📋 Component Review Checklist

Use this before committing new UI work:

1. **Imports Review**
   - [ ] All buttons use `XUIButton`
   - [ ] Stepper uses `XUIStepper` (or has documented exception)
   - [ ] Modals use `XUIModal`
   - [ ] Loaders use `XUILoader`

2. **JSX Review**
   - [ ] No `<button>` elements (search codebase: `<button`)
   - [ ] No inline style objects for spacing/typography
   - [ ] Headings use `xui-heading-*` classes
   - [ ] Spacing uses `xui-margin-*` or `xui-padding-*` classes

3. **CSS Review**
   - [ ] Custom CSS file is minimal (layout only, not styling)
   - [ ] No duplicate XUI patterns (e.g., custom button styles)
   - [ ] Color vars reference `--xero-color-*` from guidelines
   - [ ] Font sizes reference XUI typography scale

4. **Accessibility Review**
   - [ ] XUI components handle focus states (don't override)
   - [ ] XUI components handle keyboard navigation (don't break)
   - [ ] XUI components have proper ARIA (don't duplicate)

---

## 🎯 When Custom CSS Is Acceptable

You may write custom CSS only for:

1. **Layout-specific patterns** not covered by XUI:
   - One Onboarding left stepper sidebar (non-standard layout)
   - Grid layouts for specific card arrangements
   - Flexbox containers for custom responsive behavior

2. **Component composition** (combining XUI pieces):
   - Wrapper divs that position XUI components
   - Container elements that establish grid/flex contexts

3. **Prototype-specific behaviors**:
   - Mock states or transitions
   - Demo-only visual helpers

**Even then:**
- Use XUI color variables from XERO_DESIGN_GUIDELINES.md
- Use XUI spacing values (4px grid)
- Don't re-implement XUI component internals

---

## 🔍 Quick Validation Commands

Run these to catch violations:

```bash
# Find raw HTML buttons (should return 0 results in src/)
grep -r "<button" src/ --include="*.tsx"

# Find inline styles (review each, minimize usage)
grep -r "style={{" src/ --include="*.tsx"

# Find custom font-size (should use xui-font-size-* classes)
grep -r "fontSize" src/ --include="*.tsx" --include="*.css"

# Find custom margin/padding numbers (should use xui-margin/padding-*)
grep -rE "(margin|padding):\s*[0-9]" src/ --include="*.css"
```

---

## 📚 Reference

- **XERO_DESIGN_GUIDELINES.md**: Color palette, spacing scale, typography
- **@xero/xui docs**: Component API reference
- **Existing components**: See `IntroStep.tsx`, `OnlinePaymentsSettings.tsx` for good examples

---

## 🔄 Refactoring Guide

If you find existing code violating these patterns:

1. **Identify violations** using validation commands above
2. **Import XUI components** needed
3. **Replace HTML elements** with XUI equivalents
4. **Replace custom classes** with xui-* classes
5. **Minimize custom CSS** to layout-only needs
6. **Test interactivity** (focus, hover, disabled states should work)
7. **Verify visually** against XERO_DESIGN_GUIDELINES.md

---

**Last Updated:** 2026-03-04  
**Applies To:** All React components in `/A2APaymentsApp/ClientApp/src/`
