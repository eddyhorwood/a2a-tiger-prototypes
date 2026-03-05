# A2A Payment Onboarding Flow Variants

## Overview
Three flow variants to balance compliance requirements with user experience, based on risk appetite and regulatory constraints.

---

## 1. Balanced Flow (Default) ✅ IMPLEMENTED
**Status:** Complete  
**File:** `OnboardingWizardBalanced.tsx`

**Pattern:** One Onboarding (Stripe Connect style)  
**Screens:** 5 screens + left stepper  
**Time:** 3-5 minutes

### Flow
1. **Checklist** - What you'll need (with business structure selector)
2. **Business Details** - Review auto-filled Xero org data
3. **Personal Details** - Select settlement account
4. **Review & Submit** - Final review with consent
5. **Success** - Confirmation

### When to Use
- First-time setup
- Users who want to review/verify details
- Compliance-heavy regions (EU, UK)
- Building trust with new payment providers

### Pros
- Clear progress indication
- Opportunity to review/edit at each step
- Feels thorough and trustworthy
- Aligns with Stripe Connect/One Onboarding patterns

### Cons
- Takes longer (3-5 minutes)
- More clicks to complete
- May feel like overkill for returning users

---

## 2. Aggressive Flow (Fast Track) 🚧 TODO
**Status:** Not implemented  
**Target File:** `OnboardingWizardAggressive.tsx`

**Pattern:** Modal → Instant Review  
**Screens:** 2 screens (modal format)  
**Time:** 30 seconds

### Flow
1. **Connect Service Modal** - "No payment services connected" + CTA
2. **Review & Approve** - One-page review with all auto-filled data
3. Done

### Key Difference
- Skip checklist, skip business details review, skip personal details
- Go straight from "Connect" to final review page
- All data pre-filled from Xero (business + settlement account auto-selected)
- Single "Agree and submit" button

### When to Use
- Returning users (already verified)
- Low-risk jurisdictions (NZ, AU)
- Users with complete Xero profiles
- Internal/trusted beta testers
- Reducing drop-off in conversion funnels

### Pros
- Fastest possible completion (30 seconds)
- Minimal friction
- High conversion rate
- Great for A/B testing against balanced

### Cons
- Less opportunity to review details
- May feel "too fast" for first-time users
- Risk of users not understanding what they agreed to
- Compliance concerns in some regions

### Open Questions
1. **Initial config page needed?**
   - Do we need a settings page to choose flow type?
   - Or auto-detect based on org attributes (country, verification status)?
   
2. **Settlement account auto-selection**
   - Rule: Pick first eligible account? Most used account? Let user choose later?
   
3. **Akahu legal wording**
   - Where does Akahu-required consent text go?
   - Can we compress it into the final review page?

---

## 3. Safe Flow (Maximum Compliance) 🚧 TODO
**Status:** Not implemented  
**Target File:** `OnboardingWizardSafe.tsx`

**Pattern:** Full KYC/KYB Flow  
**Screens:** 7+ screens  
**Time:** 10+ minutes

### Flow
1. Checklist (with document upload requirements by business structure)
2. Business Details (full form with validation)
3. **Beneficial Owners** - Add multiple owners/directors with % ownership
4. **Identity Verification** - Document upload (passport, driver's license)
5. **Business Verification** - Company registration docs
6. Settlement Account Selection
7. Review & Submit (with checkboxes for each consent item)
8. Pending Review - Manual approval required

### When to Use
- High-risk jurisdictions
- Large transaction volumes
- Banks/financial institutions
- Users flagged for additional verification

### Implementation Notes
- Would need file upload components
- Document validation logic
- Manual review workflow
- Email notifications for approval status

---

## Implementation Strategy

### Phase 1: Balanced Flow (Complete) ✅
- [x] Checklist screen with business structure
- [x] Business details review
- [x] Settlement account selection
- [x] Final review with consent
- [x] Success confirmation

### Phase 2: Aggressive Flow (Next) 🎯
**Prerequisites:**
- [ ] Decide on modal entry point (invoice page vs settings)
- [ ] Define auto-selection rules for settlement account
- [ ] Get Akahu legal wording finalized
- [ ] Create 2-screen aggressive variant

**Implementation:**
1. Create `OnboardingWizardAggressive.tsx`
2. Strip down to 2 screens:
   - Connect service modal (with empty state illustration)
   - Final review (all data pre-filled, no intermediate steps)
3. Route: `/invoice/:invoiceId` → modal overlay → review → done
4. Success: Close modal, return to invoice with payment methods enabled

### Phase 3: Safe Flow (Future)
- Depends on regulatory requirements
- Would need Xero File Upload API integration
- Manual approval workflow TBD

---

## Config/Settings Page Consideration

**Do we need a settings page to choose flow type?**

### Option A: Auto-detect Flow Based on Context
```typescript
function determineFlowType(context: EntryContext, org: XeroOrg): FlowType {
  // Invoice entry = aggressive (user wants to get paid fast)
  if (context.source === 'invoice') return 'aggressive'
  
  // Settings entry + first time = balanced (user is exploring)
  if (context.source === 'settings' && !org.isVerified) return 'balanced'
  
  // High-risk country = safe flow
  if (HIGH_RISK_COUNTRIES.includes(org.country)) return 'safe'
  
  return 'balanced' // Default
}
```

### Option B: Let User Choose
Settings page with:
- "Quick setup (30 seconds)" → Aggressive
- "Standard setup (3-5 minutes)" → Balanced  
- "Comprehensive setup (10+ minutes)" → Safe

### Option C: A/B Test
- Randomly assign 50/50 between aggressive and balanced
- Track completion rates, drop-off, time to complete
- Choose winner

**Recommendation:** Start with Option A (auto-detect) for MVP, then add Option B for power users.

---

## Next Steps

1. **Finalize Akahu Legal Wording**
   - Get from Akahu docs/compliance team
   - Determine where it must appear (every screen? just final?)
   
2. **Implement Aggressive Flow**
   - Create 2-screen version
   - Test on invoice page entry point
   - Measure completion time
   
3. **A/B Test**
   - Aggressive vs Balanced from invoice page
   - Track: completion rate, time to complete, user satisfaction
   
4. **Add Config Page** (if needed)
   - Let users switch flows
   - Show preview of what each flow entails
