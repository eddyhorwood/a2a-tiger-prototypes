# Xero Design Guidelines for Prototypes

> **Scope:** Pragmatic guidelines to make a React + TypeScript prototype using `@xero/xui` feel very close to production Xero. Use this as a fast, copy‑pasteable baseline. For production work, always defer to **XDL/XUI tokens** and official docs.

---

## 1. Color Palette

### 1.1 Core Brand Colors

These are practical working values based on public Xero brand resources.

```css
/* Brand */
--xero-color-blue: #13B5EA;   /* Primary "Xero blue" */
--xero-color-blue-alt: #06B3E8; /* Alternative blue used in dev docs */
--xero-color-black: #000000;
--xero-color-white: #FFFFFF;

/* Neutral greys (approximate, for UI chrome) */
--xero-grey-900: #111827;
--xero-grey-800: #1F2933;
--xero-grey-700: #4B5563;
--xero-grey-600: #6B7280;
--xero-grey-500: #9CA3AF;
--xero-grey-400: #D1D5DB;
--xero-grey-300: #E5E7EB;
--xero-grey-200: #EDF1F5;
--xero-grey-100: #F3F4F6;
--xero-grey-50:  #F9FAFB;

/* Semantic status */
--xero-green-success: #22C55E;
--xero-red-error:     #EF4444;
--xero-amber-warn:    #F59E0B;
--xero-blue-info:     #0EA5E9;
```

> **Tip:** In real XDL/XUI, use semantic tokens (e.g. `--x-color-background-primary`, `--x-color-text-default`, `--x-color-border-subtle`) instead of hard‑coding hex values wherever possible.

---

### 1.2 Background Colors

```css
/* Page + surfaces */
--bg-page:           #F3F4F6;  /* Light grey page background */
--bg-page-alt:       #EDF1F5;  /* Used on settings/utility pages */
--bg-card:           #FFFFFF;  /* Panels, cards */
--bg-input:          #FFFFFF;
--bg-input-disabled: #F3F4F6;
```

**Usage:**

- **Page background:** `--bg-page` for most application surfaces.
- **Cards/tiles:** `--bg-card` on white panels over grey page background.
- **Inputs:** White on grey page; disabled inputs use `--bg-input-disabled`.

✅ **Do**

- Use **grey page + white cards** for settings and configuration flows.
- Keep **one dominant background** per page (no zebra blocky sections).

❌ **Don't**

- Put white cards on pure white page backgrounds with no separation.
- Mix multiple different greys as page backgrounds on a single screen.

---

### 1.3 Borders

```css
--border-subtle:         #E5E7EB; /* default card/input border */
--border-strong:         #D1D5DB; /* table dividers, dense boundaries */
--border-focus-blue:     #13B5EA;
--border-destructive:    #EF4444;
--border-disabled:       #E5E7EB;
```

**State usage:**

- **Default:** `--border-subtle` (`1px solid`).
- **Hover:** darken slightly or use `--border-strong`.
- **Active/focus:** use `--border-focus-blue` with a focus ring (see §8).
- **Disabled:** reuse `--border-subtle` but pair with reduced text contrast.

---

### 1.4 Text Colors

```css
--text-heading:    #111827;  /* strong, near-black */
--text-body:       #111827;
--text-muted:      #6B7280;  /* helper text, secondary meta */
--text-disabled:   #9CA3AF;
--text-link:       #0EA5E9;  /* slightly stronger cyan-blue */
--text-error:      #B91C1C;
--text-success:    #166534;
--text-on-blue:    #FFFFFF;
```

**Usage rules:**

- **Headings/titles:** `--text-heading`.
- **Body copy:** `--text-body`.
- **De‑emphasis:** `--text-muted` for timestamps, labels, helper text.
- **Links:** `--text-link` with underline on hover.
- **Errors/success:** use semantic text colors plus icons, not color alone.

---

### 1.5 Interactive & Status Colors

```css
/* Primary actions */
--button-primary-bg:           #13B5EA;
--button-primary-bg-hover:     #0EA5E9;
--button-primary-bg-active:    #0891D2;
--button-primary-text:         #FFFFFF;

/* Secondary / standard */
--button-standard-bg:          #FFFFFF;
--button-standard-border:      #D1D5DB;
--button-standard-bg-hover:    #F9FAFB;
--button-standard-text:        #111827;

/* Borderless / tertiary */
--button-borderless-text:      #0EA5E9;
--button-borderless-bg-hover:  #E0F2FE;

/* Destructive */
--button-danger-bg:            #EF4444;
--button-danger-bg-hover:      #DC2626;
--button-danger-text:          #FFFFFF;

/* Status fills (badges, tags, banners) */
--status-success-bg:           #ECFDF3;
--status-success-border:       #BBF7D0;
--status-success-text:         #166534;

--status-warn-bg:              #FFFBEB;
--status-warn-border:          #FDE68A;
--status-warn-text:            #92400E;

--status-error-bg:             #FEF2F2;
--status-error-border:         #FECACA;
--status-error-text:           #B91C1C;

--status-info-bg:              #ECFEFF;
--status-info-border:          #BAE6FD;
--status-info-text:            #0E7490;
```

**Usage rules:**

- **Primary:** one primary action per container; use Xero blue background.
- **Secondary:** bordered white buttons for secondary actions.
- **Destructive:** only when the action is high‑impact (delete, disconnect).
- **Status banners:** light status background, left accent border, semantic icon.

✅ **Do**

- Use **status background + border + icon + text** together.
- Reserve **red** for destructive or error contexts only.

❌ **Don't**

- Use Xero blue as a generic accent everywhere (keep it for actions/selection).
- Communicate status with color alone (always pair icon/text).

---

## 2. Typography System

XDL principles:

- **Display:** National 2 for headings.
- **Content:** Inter for UI text and data‑heavy views.
- **Native:** SF Pro (iOS) / Roboto (Android) on mobile.

For a web prototype:

```css
:root {
  --font-display: "National 2", system-ui, -apple-system, BlinkMacSystemFont,
                  "Segoe UI", sans-serif;
  --font-ui:      "Inter", system-ui, -apple-system, BlinkMacSystemFont,
                  "Segoe UI", sans-serif;
}
```

If you can't load National 2/Inter, fall back to system‑UI but **keep the sizes/weights**.

### 2.1 Type Scale (Approximate)

Map to your own utility classes or to XUI text components.

```css
/* Headings */
.x-heading-xxl { font-family: var(--font-display); font-size: 32px; line-height: 40px; font-weight: 600; }
.x-heading-xl  { font-family: var(--font-display); font-size: 24px; line-height: 32px; font-weight: 600; }
.x-heading-lg  { font-family: var(--font-display); font-size: 20px; line-height: 28px; font-weight: 600; }
.x-heading-md  { font-family: var(--font-display); font-size: 18px; line-height: 24px; font-weight: 600; }

/* Body */
.x-text-lg     { font-family: var(--font-ui); font-size: 16px; line-height: 24px; font-weight: 400; }
.x-text-md     { font-family: var(--font-ui); font-size: 14px; line-height: 20px; font-weight: 400; }
.x-text-sm     { font-family: var(--font-ui); font-size: 12px; line-height: 16px; font-weight: 400; }

/* Label / UI chrome */
.x-label       { font-family: var(--font-ui); font-size: 12px; line-height: 16px; font-weight: 500; letter-spacing: 0.02em; }
.x-mono        { font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace; }
```

### 2.2 Weights & Usage

- **600 (semi‑bold):** panel titles, section headings, primary emphasis labels.
- **500 (medium):** form labels, button text, pill badges.
- **400 (regular):** all body copy, descriptions, helper text.

### 2.3 Line Heights & Letter Spacing

- **Body 14–16px:** line‑height 1.4–1.5.
- **Headings:** slightly tighter (1.25–1.3).
- **Labels/all caps:** add `letter-spacing: 0.02em`.

✅ **Do**

- Use **display font for headings** and **Inter (or system UI)** for body.
- Keep headings compact and functional; avoid ornate type.

❌ **Don't**

- Mix many different fonts.
- Use super‑light or ultra‑bold weights for normal UI text.

---

## 3. Spacing System

Xero's layouts follow a fairly standard 4px spacing grid.

### 3.1 Core Scale

```css
:root {
  --space-0:   0px;
  --space-1:   4px;
  --space-2:   8px;
  --space-3:  12px;
  --space-4:  16px;
  --space-5:  20px;
  --space-6:  24px;
  --space-8:  32px;
  --space-10: 40px;
  --space-12: 48px;
}
```

### 3.2 Typical Usage

- **Inside cards/tiles:** `16–24px` padding.
- **Between form fields:** `12–16px` vertical.
- **Between sections:** `24–32px` vertical.
- **Page padding:** `24–32px` on desktop, `16–20px` on smaller widths.

Examples:

```css
.x-page {
  padding: 24px 32px;
}

.x-card {
  padding: 16px 20px;
}

.x-form-field + .x-form-field {
  margin-top: 16px;
}

.x-section + .x-section {
  margin-top: 32px;
}
```

✅ **Do**

- Use **consistent multiples of 4px** for all spacing.
- Increase vertical spacing between semantic groups (sections) vs. between fields.

❌ **Don't**

- Mix arbitrary spacing (13px, 19px, etc.).
- Collapse everything; Xero prefers **calm, breathable** layouts, especially in settings.

---

## 4. Component Patterns

Focus on components relevant to OPMM‑style flows.

### 4.1 Cards / Tiles

**Visual treatment:**

```css
.x-card {
  background: var(--bg-card);
  border-radius: 8px;
  border: 1px solid var(--border-subtle);
  box-shadow: 0 1px 2px rgba(15, 23, 42, 0.06);
  padding: 16px 20px;
}
.x-card--hoverable:hover {
  box-shadow: 0 4px 8px rgba(15, 23, 42, 0.08);
  border-color: var(--border-strong);
  cursor: pointer;
}
```

- **Radius:** 8px is a good approximation of modern XDL rounded surfaces.
- **Shadow:** subtle ambient shadow; stronger on hover for interactive cards.
- **Content:** title, description, optional status pill, primary/secondary actions.

---

### 4.2 Payment Service Cards (OPMM‑style)

Pattern: **card with colored top border**, representing the payment service status (e.g., Stripe, GoCardless).

```css
.x-payment-card {
  background: var(--bg-card);
  border-radius: 8px;
  border: 1px solid var(--border-subtle);
  box-shadow: 0 1px 2px rgba(15, 23, 42, 0.04);
  padding: 16px 20px;
  position: relative;
}

.x-payment-card::before {
  content: "";
  position: absolute;
  inset: 0;
  border-radius: 8px 8px 0 0;
  border-top: 4px solid var(--xero-color-blue);
}

/* Disabled / unavailable service */
.x-payment-card--disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
.x-payment-card--disabled::before {
  border-top-color: var(--border-subtle);
}
```

Layout inside:

- **Left:** service logo + name.
- **Middle:** short description, status pill ("Connected", "Not connected").
- **Right:** primary action (e.g. "Connect", "Manage") + overflow menu.

---

### 4.3 Buttons

Use XUI button components where possible; match their general treatment:

```css
.x-btn {
  border-radius: 9999px; /* pillish but not cartoonish */
  font-family: var(--font-ui);
  font-weight: 500;
  font-size: 14px;
  line-height: 20px;
  padding: 8px 16px;
  border-width: 1px;
  border-style: solid;
  transition: background-color 120ms ease-out,
              border-color 120ms ease-out,
              color 120ms ease-out,
              box-shadow 120ms ease-out,
              transform 80ms ease-out;
}

/* Primary (main) */
.x-btn--primary {
  background-color: var(--button-primary-bg);
  color: var(--button-primary-text);
  border-color: transparent;
}
.x-btn--primary:hover {
  background-color: var(--button-primary-bg-hover);
}
.x-btn--primary:active {
  background-color: var(--button-primary-bg-active);
  transform: translateY(1px);
}

/* Standard / secondary */
.x-btn--standard {
  background-color: var(--button-standard-bg);
  color: var(--button-standard-text);
  border-color: var(--button-standard-border);
}
.x-btn--standard:hover {
  background-color: var(--button-standard-bg-hover);
}

/* Borderless / tertiary */
.x-btn--borderless {
  background: transparent;
  border-color: transparent;
  color: var(--button-borderless-text);
}
.x-btn--borderless:hover {
  background-color: var(--button-borderless-bg-hover);
}

/* Destructive */
.x-btn--danger {
  background-color: var(--button-danger-bg);
  border-color: transparent;
  color: var(--button-danger-text);
}
.x-btn--danger:hover {
  background-color: var(--button-danger-bg-hover);
}

/* Disabled */
.x-btn:disabled,
.x-btn[aria-disabled="true"] {
  opacity: 0.5;
  cursor: default;
  box-shadow: none;
}
```

---

### 4.4 Form Inputs

```css
.x-input {
  width: 100%;
  padding: 8px 12px;
  font-family: var(--font-ui);
  font-size: 14px;
  line-height: 20px;
  border-radius: 6px;
  border: 1px solid var(--border-subtle);
  background-color: var(--bg-input);
  color: var(--text-body);
  transition: border-color 120ms ease-out,
              box-shadow 120ms ease-out,
              background-color 120ms ease-out;
}
.x-input:hover {
  border-color: var(--border-strong);
}
.x-input:focus-visible {
  outline: none;
  border-color: var(--border-focus-blue);
  box-shadow: 0 0 0 2px rgba(19, 181, 234, 0.35);
}
.x-input[disabled],
.x-input[aria-disabled="true"] {
  background-color: var(--bg-input-disabled);
  color: var(--text-disabled);
  cursor: not-allowed;
}

/* Validation states */
.x-input--error {
  border-color: var(--border-destructive);
}
.x-input--error:focus-visible {
  box-shadow: 0 0 0 2px rgba(239, 68, 68, 0.35);
}
.x-input__error-text {
  color: var(--text-error);
  font-size: 12px;
  margin-top: 4px;
}
.x-input__helper {
  color: var(--text-muted);
  font-size: 12px;
  margin-top: 4px;
}
```

---

### 4.5 Wizard Steppers

OPMM‑style setup flows often use a horizontal stepper.

```css
.x-stepper {
  display: flex;
  gap: 16px;
  margin-bottom: 24px;
}
.x-stepper__step {
  display: flex;
  align-items: center;
  color: var(--text-muted);
  font-size: 14px;
}
.x-stepper__bullet {
  width: 20px;
  height: 20px;
  border-radius: 9999px;
  border: 2px solid var(--border-subtle);
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-size: 12px;
  margin-right: 8px;
  background: #FFFFFF;
}
.x-stepper__step--active {
  color: var(--text-heading);
}
.x-stepper__step--active .x-stepper__bullet {
  border-color: var(--button-primary-bg);
  background: var(--button-primary-bg);
  color: #FFFFFF;
}
.x-stepper__step--complete .x-stepper__bullet {
  border-color: var(--button-primary-bg);
  background: #FFFFFF;
  color: var(--button-primary-bg);
}
```

---

### 4.6 Settings Pages

Pattern (similar across many Xero settings views):

- **Page header:** title + short description; optional breadcrumb.
- **Two‑column layout at desktop:** left nav list, right content; single‑column on mobile.
- **Tiles/cards:** for individual configuration areas.

```css
.x-settings-page {
  max-width: 1040px;
  margin: 0 auto;
  padding: 24px 32px 40px;
}

.x-settings-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 16px;
}
```

Use **status pills** inside tiles for connection status:

```css
.x-status-pill {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 2px 8px;
  border-radius: 9999px;
  font-size: 12px;
  font-weight: 500;
}
.x-status-pill--success {
  background: var(--status-success-bg);
  color: var(--status-success-text);
}
.x-status-pill--error {
  background: var(--status-error-bg);
  color: var(--status-error-text);
}
.x-status-pill--warn {
  background: var(--status-warn-bg);
  color: var(--status-warn-text);
}
```

---

## 5. Shadows & Elevation

XDL principle: **shadows express depth and layering**; larger shadow = higher elevation. No blur‑heavy, fuzzy shadows.

Pragmatic levels:

```css
/* Level 0: flat (inputs, inline elements) */
--shadow-none: none;

/* Level 1: cards, inline overlays */
--shadow-sm: 0 1px 2px rgba(15, 23, 42, 0.06);

/* Level 2: hoverable cards, dropdowns */
--shadow-md: 0 4px 8px rgba(15, 23, 42, 0.10);

/* Level 3: dialogs, flyouts */
--shadow-lg: 0 16px 32px rgba(15, 23, 42, 0.18);
```

**Usage:**

- **Default cards/panels:** `--shadow-sm`.
- **Hover states for interactive tiles:** go from `--shadow-sm` → `--shadow-md`.
- **Modals/popovers:** `--shadow-lg`.

✅ **Do**

- Use **shadows only when an element is above another** (overlays, raised cards).
- Keep **soft but tight** shadows (short spread, subtle alpha).

❌ **Don't**

- Use multiple competing shadows on the same element.
- Add neon/glow shadows; Xero's look is clean and restrained.

---

## 6. Borders & Corners

XDL uses rounded corners to soften interactive elements; non‑interactive panels can also be rounded but with fewer affordances.

### 6.1 Radius Values

```css
--radius-xs: 2px;  /* small chips, tags */
--radius-sm: 4px;  /* inputs, small buttons */
--radius-md: 6px;  /* most UI controls */
--radius-lg: 8px;  /* cards, panels */
--radius-full: 9999px; /* pills, round badges */
```

### 6.2 Border Widths

- **Default:** `1px`.
- **Accent (top border, focus rings combined with existing border):** 2–4px.

Special patterns:

```css
/* Colored top border for key tiles (e.g. payment methods) */
.x-card--accent-top::before {
  content: "";
  position: absolute;
  left: 0;
  right: 0;
  top: 0;
  height: 4px;
  border-radius: 8px 8px 0 0;
  background: var(--xero-color-blue);
}

/* Focus ring (outside border) */
.x-focus-ring {
  box-shadow: 0 0 0 2px rgba(19, 181, 234, 0.35);
}
```

✅ **Do**

- Use **border + elevation** thoughtfully (subtle border, subtle shadow).
- Keep radii small/medium (2–8px); enough to feel modern, not toy‑like.

❌ **Don't**

- Over‑round corners (e.g., 16px+ on normal cards).
- Use thick borders on many elements; reserve 2–4px mostly for focus/accents.

---

## 7. Layout Patterns

### 7.1 Page Structure

Typical desktop content page:

```text
[ Global nav / App bar ]
[ Page heading + actions ]
[ Optional filters / tabs ]
[ Main content area ]
```

```css
.x-page {
  max-width: 1120px;
  margin: 0 auto;
  padding: 24px 32px 40px;
}
.x-page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 16px;
  margin-bottom: 24px;
}
.x-page-header__title {
  display: flex;
  flex-direction: column;
  gap: 4px;
}
```

### 7.2 Max Widths & Grid

- **Settings / configuration pages:** `max-width: 1040–1120px`.
- **Data‑heavy dashboards/invoicing:** can extend wider, but keep line lengths controlled.

Responsive grid:

```css
@media (min-width: 1024px) {
  .x-two-column {
    display: grid;
    grid-template-columns: minmax(0, 2fr) minmax(0, 1.5fr);
    gap: 24px;
  }
}
@media (max-width: 1023.98px) {
  .x-two-column {
    display: flex;
    flex-direction: column;
    gap: 16px;
  }
}
```

### 7.3 White Space

- **Vertical rhythm:** 16px between related inputs; 24–32px between sections.
- **Horizontal gutters:** 24–32px edges at desktop, 16px on small screens.

✅ **Do**

- Center content with a **stable max‑width** and balanced white space.
- Give complex tiles/flows air; avoid crowding long forms edge to edge.

❌ **Don't**

- Stretch everything full‑bleed to viewport edges in settings/payment flows.
- Create inconsistent ad‑hoc grid columns.

---

## 8. Interactive States

XDL: use **step changes** in palette for states; elements darken on hover.

### 8.1 Buttons

```css
.x-btn { /* default styles from §4.3 */ }

/* Hover */
.x-btn--primary:hover {
  background-color: var(--button-primary-bg-hover);
}

/* Active / pressed */
.x-btn--primary:active {
  background-color: var(--button-primary-bg-active);
  transform: translateY(1px);
}

/* Focus */
.x-btn:focus-visible {
  outline: none;
  box-shadow: 0 0 0 2px rgba(19, 181, 234, 0.35);
}

/* Disabled */
.x-btn:disabled {
  opacity: 0.5;
  cursor: default;
  box-shadow: none;
}
```

### 8.2 Cards & Tiles

```css
.x-card--interactive {
  transition: box-shadow 120ms ease-out,
              border-color 120ms ease-out,
              transform 80ms ease-out;
}
.x-card--interactive:hover {
  box-shadow: var(--shadow-md);
  border-color: var(--border-strong);
}
.x-card--interactive:active {
  transform: translateY(1px);
}
.x-card--interactive:focus-visible {
  outline: none;
  box-shadow: 0 0 0 2px rgba(19, 181, 234, 0.35);
}
```

### 8.3 Inputs & Links

- **Inputs:** see §4.4; use border color + focus ring.
- **Links:**

```css
a {
  color: var(--text-link);
  text-decoration: none;
}
a:hover {
  text-decoration: underline;
}
a:focus-visible {
  outline: 2px solid rgba(19, 181, 234, 0.7);
  outline-offset: 2px;
}
a[aria-disabled="true"] {
  color: var(--text-disabled);
  pointer-events: none;
  text-decoration: none;
}
```

Transition timing:

- **Duration:** 80–150ms.
- **Easing:** `ease-out` for hover/focus; `linear` or short `ease-out` for pressed.

✅ **Do**

- Clearly differentiate **default / hover / active / disabled**.
- Support **keyboard focus** with visible outlines.

❌ **Don't**

- Rely on hover‑only indications for interactivity; some platforms don't support hover.

---

## 9. Real‑World Pattern Approximations

These are **approximations** of how common Xero surfaces look; use them as starting points.

### 9.1 OPMM‑style Settings Page

**Backgrounds:**

- Page: `--bg-page-alt` (#EDF1F5).
- Cards: `--bg-card` (#FFFFFF).

**Card styling:**

- `border-radius: 8px;`
- `border: 1px solid var(--border-subtle);`
- `box-shadow: var(--shadow-sm);`
- 16–20px padding.

**Buttons:**

- "Connect payment service" → primary.
- "Manage" → standard.
- "Disconnect" → danger or borderless red link in overflow menu.

**Typography:**

- Page heading: `x-heading-xl`.
- Card titles: `x-heading-md`.
- Descriptions: `x-text-md` / `x-text-sm` with `--text-muted`.

**Spacing:**

- 24px under page heading.
- 16px between cards in grid.

---

### 9.2 New Invoicing UI (Approximate)

**Backgrounds:**

- Page background: light grey.
- Invoice form embedded in **white card** with clear borders.

**Card/panel:**

- Invoice header (contact, dates, status) in a lightly separated band.
- Line items in table with subtle row dividers (`--border-subtle`).

**Buttons:**

- "Approve" / "Send" primary in Xero blue.
- "Save" standard.
- Secondary actions via overflow menu.

**Typography & spacing:**

- Dense but readable: most body text at 14px with 20px line height.
- 8–12px row padding in tables.
- 24px vertical space between main sections.

---

### 9.3 Generic Settings Pages

**Patterns:**

- Left aligned heading & description; right aligned primary action.
- Cards for different settings domains (e.g., "Online payments", "Invoice settings").
- Status pills to show connection/enablement.

Use the settings layout from §4.6 and adjust content per page.

---

## 10. Common Patterns

### 10.1 Modal / Dialog

```css
.x-modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(15, 23, 42, 0.45);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 50;
}

.x-modal {
  background: #FFFFFF;
  border-radius: 8px;
  box-shadow: var(--shadow-lg);
  max-width: 520px;
  width: 100%;
  padding: 24px 24px 20px;
}

.x-modal__header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 16px;
}

.x-modal__footer {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  margin-top: 24px;
}
```

### 10.2 Empty States

Pattern:

- Illustration or icon.
- Short, friendly heading.
- Single line of explanatory text.
- Clear primary action.

```css
.x-empty-state {
  text-align: center;
  padding: 40px 24px;
  color: var(--text-muted);
}
.x-empty-state__title {
  font-size: 18px;
  font-weight: 600;
  color: var(--text-heading);
  margin-top: 16px;
}
.x-empty-state__body {
  margin-top: 8px;
}
.x-empty-state__actions {
  margin-top: 16px;
}
```

### 10.3 Loading States

- Prefer **inline skeletons or shimmer** for cards/tables.
- Use small **spinners next to labels** for short waits (e.g., "Connecting…").

```css
.x-spinner {
  width: 16px;
  height: 16px;
  border-radius: 9999px;
  border: 2px solid #E5E7EB;
  border-top-color: var(--button-primary-bg);
  animation: x-spin 0.8s linear infinite;
}

@keyframes x-spin {
  to { transform: rotate(360deg); }
}
```

### 10.4 Error States

For inline field errors, combine:

- Red border + red helper text + icon.

For form‑level or network errors, show a top‑of‑panel banner:

```css
.x-alert {
  border-radius: 6px;
  padding: 8px 12px;
  display: flex;
  align-items: flex-start;
  gap: 8px;
  font-size: 14px;
}

.x-alert--error {
  background: var(--status-error-bg);
  border: 1px solid var(--status-error-border);
  color: var(--status-error-text);
}
.x-alert--success {
  background: var(--status-success-bg);
  border: 1px solid var(--status-success-border);
  color: var(--status-success-text);
}
```

### 10.5 Success Confirmation Screens

Pattern:

- Left‑aligned icon (checkmark in a circle).
- Heading ("Payment method connected").
- Short, concrete copy.
- Primary CTA ("Done", "Return to settings").

Use the success alert styling, but with more vertical padding.

### 10.6 Form Layouts

```css
.x-form-field {
  display: flex;
  flex-direction: column;
  gap: 4px;
}
.x-form-label {
  font-size: 12px;
  font-weight: 500;
  color: var(--text-heading);
}
.x-form-row {
  display: flex;
  gap: 16px;
}
@media (max-width: 767.98px) {
  .x-form-row {
    flex-direction: column;
  }
}
```

---

## 11. Anti‑Patterns to Avoid

✅ **Do**

- Follow **XDL principles**: multiple affordances for interactivity, semantic colors, system‑level thinking.
- Favour a **calm, trustworthy** look: clear hierarchy, legible typography, consistent spacing.

❌ **Don't**

1. **Over‑rounded corners**

   - Avoid 16–24px radii on standard cards and buttons.
   - Use 4–8px for most surfaces; `9999px` only for pills.

2. **Wrong greys / high contrast noise**

   - Don't mix very dark borders with ultra‑light backgrounds.
   - Avoid `#CCCCCC`‑style generic greys; use a consistent neutral ladder.

3. **Excessive animations**

   - No bouncy, long transitions.
   - Keep all UI transitions under ~150ms and mostly limited to **opacity, color, shadow**.

4. **Color misuse**

   - Don't use red for neutral actions.
   - Don't overuse Xero blue on text and icons; reserve it for **primary interactions and selection**.

5. **Inconsistent spacing**

   - Don't eyeball padding/margins.
   - Don't cram inputs with 4px vertical spacing; Xero favours **roomy, scannable** forms.

6. **Custom ad‑hoc components where XUI exists**

   - Don't reinvent buttons, inputs, alerts when a XUI component is available.
   - Don't override XUI styles in ways that break accessibility (e.g., removing focus outlines).

---

## 12. XUI‑Specific Guidance

XUI is the shared design system that underpins XDL in code.

### 12.1 Use XUI Before Custom CSS

- Prefer XUI **buttons, inputs, alerts, tooltips, panels** wherever possible.
- Compose your layouts using XUI components, then **layer light custom CSS** to approximate new patterns.

✅ **Do**

- Wrap XUI components in layout containers (`div`s) that you control.
- Use **tokens** from `@xero/xui-tokens` for colours, typography, etc., where you can.

```scss
/* Example: Use semantic XDL tokens in your SCSS */
@use 'node_modules/@xero/xui-tokens/css/xero-design-language/foundation/web/xui-tokens' as semantic;

.my-panel {
  /* Map semantic tokens into component-level custom properties */
  --x-panel-bg: var(--x-color-background-primary);
  --x-panel-text: var(--x-color-text-default);
}
```

❌ **Don't**

- Hard‑override internal XUI classnames directly (e.g., via deep selectors) unless absolutely necessary.
- Change **layout or behaviour** XUI depends on (display type, focus outlines, aria attributes).

---

### 12.2 Utility‑Style Patterns

Even without explicit XUI utilities documented here, you can create lightweight equivalents aligned to this guide:

```css
/* Spacing utilities */
.u-mt-4  { margin-top: 16px; }
.u-mb-4  { margin-bottom: 16px; }
.u-mt-6  { margin-top: 24px; }
.u-mb-6  { margin-bottom: 24px; }
.u-p-4   { padding: 16px; }

/* Layout */
.u-flex        { display: flex; }
.u-flex-between{ justify-content: space-between; }
.u-flex-center { align-items: center; }

/* Text */
.u-text-muted  { color: var(--text-muted); }
.u-text-danger { color: var(--text-error); }
```

Use these **only in your prototype** to avoid colliding with any real XUI utility naming.

---

### 12.3 Layering Custom CSS on Top of XUI

1. **Component first, overrides second**

   - Render the relevant XUI React component.
   - Add a **`className`** to a wrapper element.
   - Apply spacing/positioning/approximate colours in your own CSS.

2. **Respect tokens**

   - Where possible, use XDL token variables (e.g., `--x-color-background-primary`) by referencing them through `@xero/xui-tokens` as shown in the XDL component modernisation guide.

3. **Know when to override**

   - Override when:
     - You need **extra spacing** around a component in a prototype.
     - You're approximating **a UI pattern that XUI doesn't yet expose**.
   - Avoid overriding:
     - Focus styles.
     - Contrast/colour decisions that affect accessibility.

---

### 12.4 Limitations & Practical Advice

- You **won't** fully replicate every XDL nuance (tokens, typography tweaks, motion) without direct access to XUI docs and design libraries.
- For your prototype, aim for:
  - **Correct hierarchy** (layout, spacing, typography scale).
  - **Correct overall colour story** (Xero blue as primary, light greys, semantic status colours).
  - **Using XUI components for behaviours**, not reinventing them.

If you stick to:

- The colour + spacing system from §§1–3,
- Component treatments from §4,
- States from §8,
- And a "settings page" layout from §7,

your React + TypeScript prototype will read as **very close to a real Xero product**, especially for flows like **Online Payment Method Management** that are primarily settings‑style configuration surfaces.



---

## Sources

- [XDL Component Modernisation](https://xero.atlassian.net/wiki/spaces/~6050652ee394c30069a2068d/blog/2025/09/16/271105754072/XDL+Component+Modernisation)
- [XDL foundations](https://xero.atlassian.net/wiki/spaces/XUI/pages/271029993684)
- [XUI Design System](https://xero.atlassian.net/wiki/spaces/XUI/pages/270998407821)
