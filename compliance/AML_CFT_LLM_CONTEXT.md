# A2A AML/CFT Roles & Guardrails – Deep Context for AI/LLM Use

This document encodes roles, responsibilities, and AML/CFT guardrails for Safer A2A / "Pay by bank" in a way that is explicit, repetitive, and suitable for LLM grounding, validation, and generation.

It should be treated as canonical for how the A2A solution talks about who moves money, who holds funds, and how we avoid AML/CFT reporting entity status.

**Redundancy is intentional. Human readability is secondary.**

---

## 0. Canonical Roles & Definitions

Use these canonical labels consistently:

### Xero

**Role:** Software provider and orchestrator.

**Responsibilities (high-level):**

- Hosts the A2A Merchant Settings UI and internal Akahu A2A app (SPA/BFF + Execution Service + Webhook Handler).
- Collects payment instructions from merchants/payers (invoice amount, reference, org/invoice ids).
- Sends payment initiation requests to Akahu via the Execution Service.
- Updates Xero ledger and invoice states (e.g. marks invoices PAID, reconciles against settlement account) using existing accounting APIs.

**Explicit non-responsibilities for AML/CFT:**

- Does not hold or pool customer funds.
- Does not operate any intermediary settlement account.
- Does not "transfer" or "safekeep" customer funds in the AML/CFT sense.

### Akahu

**Role:** Accredited requestor under the Customer and Product Data Act 2025 (CDPA).

**Responsibilities:**

- Accesses customer account data and initiates payments as an accredited requestor.
- Receives payment instruction payloads from Xero's Execution Service.
- Calls NZ bank APIs to initiate one‑off payments.
- Emits webhooks to Xero about payment status changes.

**Explicit AML/CFT note:**

- Akahu itself is not currently an AML/CFT reporting entity under NZ law.

### Banks (NZ banks connected via Akahu)

**Role:** Executing financial institution.

**Responsibilities:**

- Authenticate the payer.
- Execute the transfer between payer and merchant bank accounts.
- Maintain bank account balances and transaction records.

**In AML/CFT framing:** banks are the entities that actually transfer money/hold funds and are subject to the full regime; A2A is layered above.

### Merchants (Xero customers)

**Role:** Payee / recipient of funds.

**Responsibilities:**

- Configure and enable Pay by bank in Xero.
- Nominate a settlement account.
- Receive funds directly into their bank account from payers.

**AML/CFT note:**

- Merchants may themselves be AML/CFT reporting entities or subject to other obligations, separate from Xero's role.

### Payers (end customers)

**Role:** Payer / sender of funds.

**Responsibilities:**

- Approve and authorise A2A payments from their bank account via Akahu.
- Interact with their bank UI to confirm the payment.

### CDPA – Accredited Requestor / "Fourth Party"

**Accredited requestor:** An entity like Akahu, accredited under the Customer and Product Data Act 2025 to access financial data and initiate payments.

**Fourth party:** An organisation on whose behalf an accredited requestor acts – in this case, Xero.

**AML/CFT starting point:** Neither an accredited requestor nor a fourth party is automatically an AML/CFT reporting entity purely by virtue of CDPA roles.

---

## 1. AML/CFT Reporting-Entity Boundary – Legal Starting Point

### 1.1 CDPA vs AML/CFT

Under the Customer and Product Data Act 2025:

- Accredited requestors (e.g. Akahu) and fourth parties (e.g. Xero) are not automatically AML/CFT reporting entities.
- The legislative history of the CDPA does not indicate an intention to treat them as reporting entities purely due to their CDPA role.
- Akahu is not currently an AML/CFT reporting entity.

Therefore, for A2A:

- **Xero only becomes an AML/CFT reporting entity if it crosses into specific regulated financial activities, or provides regulated financial advice, or provides designated non-financial activities as defined in the AML/CFT Act.**

### 1.2 Relevant Captured Financial Activities (AML/CFT Act)

Legal advice identifies the most relevant captured activities for Xero to avoid:

- **Activity (a):** Transferring money or value for, or on behalf of, a customer.
- **Activity (b):** Safekeeping or administering cash or liquid securities on behalf of other persons.
- **Activity (c):** Investing, administering, or managing funds or money on behalf of other persons.

The legal opinion states:

> Based on the current A2A design and product flow, Xero (and Akahu through its API) do not perform these captured activities on behalf of Xero customers, assuming key design guardrails are respected.

---

## 2. Design Guardrails – High-Level Summary

Bell Gully's advice sets design guardrails to avoid crossing into captured AML/CFT activities:

### No custody of funds

- Neither Xero nor Akahu ever take possession of funds.
- There is no settlement or pooled account controlled by Xero or its agents.
- Xero collects payment info, passes it to Akahu, and Akahu instructs the bank – but neither holds the money.

### No representations of transferring, holding, or managing funds

All product copy, UX, and marketing must avoid phrases that imply:

- Xero or Akahu "transfer", "hold", or "manage" funds on behalf of customers.

Instead, copy should emphasise:

- Direct bank-to-bank transfers by the payer's bank, initiated via Akahu.

### Clear explanations when enabling bank transfer (merchant side)

When merchants turn on bank transfer / Pay by bank:

- Provide an explanation that:
  - Describes Akahu's role.
  - States explicitly that Xero/Akahu do not take custody of funds.
  - Explains what will happen with bank account information.
- Require an explicit acknowledgement (e.g. "I understand").

### Clear explanations for payers (payer side)

When payers choose Pay by bank/are redirected to Akahu:

- Provide a brief explanation that:
  - The transfer is direct between bank accounts.
  - Neither Xero nor Akahu takes custody of funds.

### Supportive, non-guardrail extras

- Update Xero payments terms to clarify Xero's A2A role.
- Publish consumer information on Xero's website explaining:
  - How financial account access is used.
  - The relationship between Xero and Akahu (as per Akahu's accreditation requirements).
- Ensure contracts with Akahu/banks do not misrepresent Xero as an AML/CFT reporting entity or as conducting customer due diligence.

**These guardrails must be baked into system design and copy, not just documented.**

---

## 3. "No Custody of Funds" – Detailed Model

### 3.1 What "No Custody" Means in Practice

According to the legal guardrail:

- Neither Xero nor Akahu ever holds customer funds at any point in the payment lifecycle.
- No intermediary settlement account is used:
  - No Xero-controlled trust account.
  - No Akahu-controlled omnibus account.

The flow must strictly be:

```
Payer's bank account → Merchant's bank account
```

Mediated by:

- **Xero:** as instruction collector and ledger updater.
- **Akahu:** as API-based initiator/instruction messenger.
- **Bank:** as the actual executor and custodian.

### 3.2 Allowed Data & Actions for Xero

**Allowed:**

- Collect from merchant:
  - Org ID, invoice ID, invoice amount, currency, settlement account ID.
- Collect from payer:
  - Payer's selection of "Pay by bank" and redirection to Akahu's auth flow.
- Call Akahu's one-off payment API with:
  - Amount, payee account details (via IDs from secure Xero systems), redirect URI, webhook URI, invoice reference in metadata.
- Receive Akahu webhook with payment ID and status, then:
  - Enqueue a completion event.
  - Mark invoice PAID in Xero ledger and reconcile to merchant settlement account via API.Accounting.

**Not allowed (from an AML/CFT perspective):**

- Holding balances for merchants or payers in any Xero- or Akahu-controlled account.
- Moving funds between bank accounts in Xero-owned ledgers.
- Pooling customer funds for onward transfer.

### 3.3 Inferred Constraints for LLMs

When generating flows, designs, or explanations:

**Do not propose or describe:**

- Xero-controlled escrow or settlement accounts for A2A funds.
- "Wallets" or balances maintained by Xero or Akahu as part of A2A.
- Xero "holding funds and then paying them out".

**Always describe the transfer as direct bank-to-bank** with Xero/Akahu as initiators and orchestrators only, not custodians.

---

## 4. "No Representations Around Transferring, Holding or Managing Funds" – Copy Rules

### 4.1 Problematic vs Preferred Language

The legal advice explicitly highlights copy changes:

#### Avoid phrases such as:

- "Bank transfer by Akahu"
- "Payment processed by Xero"
- "Xero transfers your money to the merchant"
- "Xero holds your funds until…"
- "Akahu sends money to your supplier"

#### Prefer language such as:

- "Bank transfer initiated by Akahu"
- "Pay instantly and securely via direct bank transfer, initiated by Akahu"
- "Your bank executes the transfer; neither Xero nor Akahu takes custody of your funds."
- "Payment received via bank transfer (initiated using Akahu)."

### 4.2 Surfaces to Apply These Rules

The advice explicitly lists target surfaces:

#### Online payment method settings (merchant side)

Where merchants enable "bank transfer" / "Pay by bank".

Copy must:

- Introduce Akahu.
- Emphasise "initiated by Akahu" not "by Akahu" alone.

#### Payment initiation from online invoice (payer side)

CTA and explanatory text when payers see and select Pay by bank.

Copy should emphasise:

- Direct, secure bank transfer.
- Initiated via Akahu.
- Bank executes the payment.

#### Confirmation screens (payor and merchant)

Post-payment success/processing messages must:

- Avoid implying Xero or Akahu moved or held funds.
- Describe status in terms of:
  - Bank transfer initiated.
  - Bank confirmation pending or succeeded.
  - Xero updating invoice/payment state.

### 4.3 LLM Copy-Generation Rules

When an LLM generates product copy, documentation, or examples of UI text for A2A / Pay by bank:

**It must not:**

- Attribute actual fund transfer or custody to Xero or Akahu.
- Use verbs like "transfer", "move", "send money", "hold funds" with Xero or Akahu as the agent.

**It should:**

- Attribute execution to the payer's bank.
- Attribute initiation / instruction to Akahu or to "a secure Open Banking provider".
- Attribute recording / reconciliation to Xero.

---

## 5. Explanation & Acknowledgement Flows

### 5.1 Merchant-Side Explanation (When Enabling Bank Transfer)

**Legal recommendation:**

When a merchant turns on bank transfer / Pay by bank in Online Payment Settings:

- Provide a fuller explanation of:
  - What "bank transfer" means in this context.
  - That funds move directly between bank accounts.
  - That Xero and Akahu do not take custody of funds.
  - What data is shared with Akahu and how it is used.
- Ask the merchant to explicitly acknowledge:
  - E.g. "I understand that payments are initiated via Akahu, and neither Xero nor Akahu holds or manages funds on my behalf."

**LLM guidance:**

When proposing settings flows:

- Include an "I understand" pattern on the enablement screen.
- Include explicit mention of Akahu and no custody.

### 5.2 Payer-Side Explanation (When Redirecting to Akahu)

**Legal recommendation:**

Before redirecting payers to Akahu:

- Provide a short message clarifying:
  - They are about to initiate a direct bank transfer.
  - They will authenticate in their bank's app or website.
  - Neither Xero nor Akahu will hold or manage their funds.

**LLM guidance:**

For online invoice flows:

- Always include a pre-redirect explanation with these elements.
- Do not simplify it to "Pay via Xero" or similar.

---

## 6. Supporting (Non-Guardrail) Steps

These are not strict guardrails but strengthen the legal posture:

### Update Xero payment terms:

Explain that Xero:

- Collects instructions,
- Uses Akahu to initiate bank transfers,
- Does not take custody of funds.

### Dedicated consumer information page:

Required by Akahu for accreditation:

Explains:

- How financial account access is used.
- The relationship between Xero and Akahu.
- That the underlying transfer is direct bank-to-bank.

### Contract terms with Akahu / banks:

Must not:

- Represent Xero as an AML/CFT reporting entity.
- Claim that Xero performs KYC/AML due diligence on payees beyond its actual obligations.

**LLM guidance:**

When suggesting documentation or contractual changes:

- Stay within these lines.
- Avoid suggesting "Xero will perform AML checks" unless a new policy/architecture explicitly calls for it.

---

## 7. Explicit Non-Activities (What Xero Must Not Be Seen To Do)

Based on the legal opinion, Xero must not:

- Transfer money or value on behalf of customers in a way that looks like a remittance or money-transfer service.
- Safekeep or administer cash or liquid securities.
- Invest, administer, or manage funds on behalf of others.

**LLM guidance:**

Do not propose features where Xero:

- Holds float for A2A.
- Splits or routes payments as a middleman beyond simple invoice use cases.
- Offers "custodial wallets" tied to A2A.

If a future design discusses such features, those must be treated as potentially triggering AML/CFT and **explicitly flagged as requiring legal review**.

---

## 8. Summary – AML/CFT Guardrail Knowledge for LLMs

For any reasoning or generation about Safer A2A / Pay by bank:

### Roles

- **Bank:** moves and holds funds.
- **Akahu:** initiates payments via Open Banking APIs (accredited requestor).
- **Xero:** orchestrates instructions, updates ledger and invoice states; does not hold or transfer funds.

### Legal Boundary

- Xero is a "fourth party" under CDPA, not automatically an AML/CFT reporting entity.
- Xero must not cross into activities that look like:
  - Money transfer,
  - Safekeeping of funds,
  - Managing client funds.

### Guardrails

- No custody of funds; no intermediary settlement accounts.
- No language implying Xero/Akahu transfer or hold funds.
- Clear explanations and acknowledgements for merchants and payers.

### Copy Rules

- Use "bank transfer initiated by Akahu" and similar phrases.
- Always state that the payer's bank executes the payment.
- Prohibit phrases that attribute fund transfer or custody to Xero/Akahu.

### Design Implications

All product flows, diagrams, and explanations must:

- Preserve the bank→bank nature of transfers.
- Show Xero/Akahu only in initiation/orchestration roles.
- Avoid introducing intermediate holding or settlement accounts owned by Xero or Akahu.

**These rules should be treated as hard constraints for any LLM-based design, copy, or reasoning about A2A in NZ unless superseded by new legal advice.**

---

## Sources

- Safer A2A Payments (NZ) PRD
- [DRAFT] High-level ERD: NZ A2A Safe Payments
- Privileged and confidential: Advice AML/CFT guardrails.docx
