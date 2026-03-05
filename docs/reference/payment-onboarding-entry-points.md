# Payments onboarding entry points (current state)

**Scope:** Web invoicing "Online payments" setup (Stripe, GoCardless, PayPal, other providers) plus key mobile (XAA) entry points. Bills and payroll onboarding flows are out of scope for this doc.

---

## 1. Shared web entry points (all payment services)

These surfaces exist regardless of specific provider and typically route into the Payment services / Online payments settings experience.

### 1.1 Navigation & settings

#### Sales navigation (New/Xero Blue)

**Navigation bar → Sales → Online payments settings**

Opens Online payments settings page (Payment services management).

#### Organisation-level payment settings (modern)

**Organisation settings → Payment services → Online payments settings**

Path used by Stripe, GoCardless, and (historically) others.

#### Legacy invoice settings route

**Organisation settings → Invoice settings → Payment Services**

From there, Online payments settings section and provider-specific CTAs (e.g. Start with Card payments, Start with Bank payments, Set up PayPal).

#### Direct URL (modern Payment services landing)

**https://go.xero.com/app/{shortcode}/payment-services**

Shared landing for online payments onboarding and management, used by multiple flows (invoicing, setup guide, app-store campaigns, etc.).

### 1.2 From create/edit invoice (one-off invoices)

On new or existing invoices (classic + new invoicing):

#### Inline CTA in invoice editor

**Create/edit invoice → "Set up online payments"**

If payments aren't configured, opens Online payments setup modal, then routes to Payment services page or provider-specific onboarding (e.g. Stripe, GoCardless, PayPal).

#### "Manage online payments" link (when some services exist)

**Create/edit invoice → "Manage online payments"**

Opens management modal; "Connect"/"Set up" links can deep-link into specific provider flows (Stripe/GC/PayPal/Other).

#### Invoice onboarding modal (step 2 – payments)

**First use of new invoicing → invoice onboarding modal → Step 2 (Payments onboarding)**

Educates about benefits of online payments and provides a CTA into payment service setup (typically Stripe).

### 1.3 From repeating invoices

#### New repeating invoice CTA

**New repeating invoice → Online payments – "Get set up now" → "Get started now"**

Routes to Payment services/Online payments settings, then provider onboarding (Stripe, GoCardless, PayPal, Other).

#### Repeating invoice template/editor

**Repeating invoice template → Online payments section ("Set up", "Get set up now")**

Same underlying route as one-off invoice editor, but scoped to template.

### 1.4 From invoice list, dashboard & in-product comms

#### Invoice list view banner / inline nudge

Contextual banners or IPCs in Invoices list to "Add payment sign up" / "Set up online payments" → opens Online payments setup (via Payment services page or modal).

#### Dashboard → Invoice widget CTA

**Dashboard → Invoice widget banner** prompting to "Add a pay now button" / "Set up online payments" → routes to Payment services onboarding.

#### Invoice onboarding tours (Intercom / setup guide)

Tours or setup tasks that include a "Set up online payments" step linking into Payment services or invoice onboarding payments step.

#### Marketing / campaign & App Store entry points

Marketing landing pages, Xero App Store pages, promotional articles/banners with CTAs like "Set up online payments" or "Connect Stripe/GoCardless/PayPal" → typically deep-link to Payment services or provider app install.

---

## 2. Provider-specific web entry points

### 2.1 Stripe (card & digital wallets)

Primary entry points to start Stripe setup:

#### Sales navigation

**Navigation bar → Sales → Online payments settings → "Start with Card payments"**

#### Organisation payment services settings

**Organisation settings → Payment services → Online payments settings → "Start with Card payments"**

#### Invoice editor

**Create/edit invoice → "Set up online payments" / "Manage online payments" → "Set up Stripe"**

#### Repeating invoices

**New repeating invoice → Online payments – "Get set up now" → "Get started now"**

#### Legacy invoice settings route

**Organisation settings → Invoice settings → Payment Services → Online payments settings → "Start with Card payments"**

#### Promotional banner/article

Any promotional banner or article → Online payment settings → Card payments/Stripe setup

#### High-contrast attach banner (Stripe)

**Edit/view invoice high-contrast banner ("Add online payments")**

Shown to orgs without a payment service attached. CTA "Add online payments" starts Stripe setup journey from the invoice view/edit page.

#### Abandoned setup banner (Stripe)

For orgs that started but didn't complete Stripe setup, a "Finish Stripe setup" high-contrast banner appears on invoice edit/view, with "Remind me in 2 days" and "Dismiss" options.

#### Overdue invoice / attach experiments (web)

Experiments use similar high-contrast banners on invoice view to drive Stripe setup from overdue/awaiting payment contexts.

### 2.2 GoCardless / "Bank payments"

Entry points to start GoCardless setup (bank payments):

#### Sales navigation

**Navigation bar → Sales → Online payments settings → "Start with Bank payments" / "Set up GoCardless"**

#### Organisation payment services settings

**Organisation settings → Payment Services → Online payments settings → "Set up Bank payments"**

#### Invoice editor

**Create/edit invoice → "Set up online payments" / "Manage online payments" → "Connect" → "Get started now" → GoCardless onboarding (new tab)**

#### Repeating invoices

**New repeating invoice → Online payments – "Get set up now" → "Get started now" → GoCardless onboarding (new tab)**

#### Legacy invoice settings route

**Organisation settings → Invoice settings → Payment Services → Online payments settings → "Start with Bank payments" / "Set up GoCardless"**

#### Promotional banner/article

Any promotional banner or article → Online payment settings → "Start with Bank payments" / "Set up GoCardless"

### 2.3 PayPal

Entry points to start PayPal setup:

#### Organisation-level payment settings

**Organisation settings → Payment Settings → "Set up PayPal" → PayPal onboarding (external)**

#### Invoice editor

**Create/edit invoice → "Set up online payments" / "Manage online payments" → "Connect" → "connect it here" → Payment Settings (new tab) → PayPal setup**

#### Repeating invoices

**New repeating invoice → Online payments – "Get set up now" → "To add another provider connect it here" → Payment Settings (new tab) → PayPal setup**

#### Legacy invoice settings route

**Organisation settings → Invoice settings → Payment Services…** (lists configured PayPal services and other providers)

### 2.4 "Other payment services"

Catch-all for non-Stripe/GoCardless/PayPal providers (varies by region).

#### Organisation-level payment settings

**Organisation settings → Payment Settings → "… add another online payment option"**

Opens provider gallery / app-store linked experience, then Payment services.

#### Invoice editor & repeating invoice

- **Create/edit invoice → "Set up online payments" / "Manage online payments" → "Connect" → "To add another provider connect it here" → Payment Settings (new tab)**
- **New repeating invoice → Online payments – "Get set up now" → "To add another provider connect it here" → Payment Settings (new tab)**

#### Legacy invoice settings route

**Organisation settings → Invoice settings → Payment Services…** (shows all connected/available services)

---

## 3. Additional attach / onboarding surfaces (web invoicing)

### 3.1 Invoice onboarding modal – payments step

New orgs using new invoicing see an onboarding modal on first invoice:

- Banner on invoice page → "Set up invoice template" (Phase 1).
- Step 2 "Payments onboarding" introduces online payments, previewing invoice + payment buttons, with CTA into Stripe setup.
- In some experiments, this modal is accessible via Setup guide widget ("Invoicing" task) instead of banner; banner is hidden for treatment cohorts.

### 3.2 Quotes (pre-invoice) banner – Q-01

#### Create Quote screen (web)

High-contrast banner for non-attached orgs:

- **Messaging** around "1 in 4 customers would go elsewhere if they can't pay their preferred way".
- **CTA:** "Set up online payments" → Stripe sign-up flow (modal or Payment services page).
- **Overflow:** "Dismiss", "Learn more about payments" (to Payment services page).

### 3.3 Overdue invoices attach banners (web)

Overdue invoices experiments (Zinger) add banners on invoice view/list for orgs without payments attached, driving into Stripe setup modal or Payment services page.

---

## 4. Mobile (Xero Accounting App – XAA) entry points

### 4.1 Guided Setup (dashboard)

#### Guided Setup → "Payments" / "Online payments with Stripe" task

- New orgs see a Payments category in Guided Setup with tasks like "Set up online payments" or "Add a payment service".
- Tapping opens Stripe onboarding flow via Touch API and Global Stripe Onboarding API.

### 4.2 Settings → Card & wallet payments

From the XAA app:

**Settings (More) → Card and wallet payments**

- Controlled by "Global Stripe Onboarding Settings" flags (iOS & Android).
- Entry point into the Stripe onboarding screens (connect Stripe, configure bank/fee accounts, pricing, etc.).

### 4.3 Overdue invoices list banner (XAA)

Planned experiment (Stripe-01).

**Sales → Overdue invoices list**

Treatment cohort sees contextual banner:

- **Value prop:** e.g. "86% of customers prefer to pay online – add online payments to get paid faster".
- **CTA:** "Set up Stripe" (via deep link into mobile Stripe onboarding).
- After completing setup, user is returned to Overdue invoices list; banner no longer appears.

### 4.4 In-journey invoice entry points (mobile)

From the Stripe payments setup user journey mapping for XAA:

#### Invoice document banners

Banners on invoice screens (or send flow) prompting "Set up online payments" if Stripe is not yet configured.

#### Global create / Sales surfaces

Entry via Sales tab → invoices/overdue or global create invoice may show attach prompts that lead into the same Stripe onboarding flow.

#### Post-setup surfaces

After Stripe is connected, users can review connection status & key settings in XAA (e.g. via Payments or Settings surfaces), but initial attach is via Guided Setup / Settings / banners above.

---

## 5. Summary view (by surface)

### 5.1 Web – main surfaces

**Settings / admin**
- Sales → Online payments settings
- Organisation settings → Payment services → Online payments settings
- Organisation settings → Invoice settings → Payment Services

**Within invoicing**
- Create/edit invoice → "Set up online payments" / "Manage online payments"
- New repeating invoice → Online payments CTA
- Invoice onboarding modal – payments step
- High-contrast attach/abandoned setup banner on invoice view/edit

**List & dashboard**
- Invoices list view banners
- Dashboard → Invoice widget banner

**Campaigns / external**
- Xero App Store, marketing pages, help articles → Payment services / provider setup

### 5.2 Mobile (XAA) – main surfaces

- Guided Setup → Payments tasks
- Settings → Card and wallet payments
- Overdue invoices list banner
- Invoice-related banners / flows initiating Stripe setup

These surfaces collectively form the current entry points into payment method setup/onboarding across Stripe, GoCardless, PayPal, and other payment services on web and mobile.

---

## Sources

- Sign up to payment services
- Settings
- Online payments onboarding [screen flows]
- PRD - Get Paid - Invoicing Onboarding Experience- Global
- Thread between Henry and Katie
- IO-17: payments popover vs banner (new sign up)
- PRD - Get Paid - Invoice Onboarding API - Global
- GRW-10: Generic payer demand insight in the high contrast banner on invoice view/edit - Start setup
- IO-15: Abandoned set up high contrast banner
- Sign up to payment services
- Q-01 : Contextual Payments Signup High Contrast Banner in Quotes
- GSOB Feature Runbook
- User Journey Map | Stripe Payments Setup XAA
- Stripe-01: Add banner in overdue invoice list to drive Stripe setups
