# Compliance Documentation

This folder contains legal and compliance documentation for the A2A Tiger prototype project.

## Contents

### AML_CFT_LLM_CONTEXT.md

Canonical documentation of AML/CFT (Anti-Money Laundering / Countering Financing of Terrorism) roles and guardrails for the Safer A2A / "Pay by bank" solution.

**Purpose:** Provides explicit, repetitive guidance for LLM/AI agents working on this project to ensure all designs, copy, and flows maintain compliance with NZ legal requirements.

**Key Topics:**
- Canonical role definitions (Xero, Akahu, banks, merchants, payers)
- AML/CFT reporting entity boundaries
- Design guardrails ("no custody of funds", "no representations around transferring funds")
- Copy rules and prohibited language
- Explanation and acknowledgement flow requirements

**Important:** This document is designed for AI/LLM consumption and should be treated as hard constraints when generating any A2A-related designs, flows, or copy.

**Single Source of Truth:** Section 4.1 contains the canonical list of prohibited and preferred language for payment flows. When you update this section, the changes automatically propagate to all AI agents via [../.github/copilot-instructions.md](../.github/copilot-instructions.md) which references this file.

---

## For Contributors

If you're working on A2A-related features with AI assistance, ensure your AI agent has read and understood the guardrails in AML_CFT_LLM_CONTEXT.md before generating:

- Product copy or UX text
- Payment flow designs
- System architecture diagrams
- Feature proposals involving fund movement

Any proposed features that might involve Xero holding, pooling, or transferring funds must be flagged for legal review.
