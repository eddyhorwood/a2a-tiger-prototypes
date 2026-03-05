/**
 * Prototype Configuration
 * 
 * Defines the initial state for the A2A onboarding prototype.
 * This allows testing different merchant scenarios without rebuilding mock data.
 */

export type StripeStatus = 
  | 'none'                    // No Stripe connected
  | 'active'                  // Stripe connected, actively used (high volume)
  | 'low_use'                 // Stripe connected, low win-rate

export type BankAccountSetup =
  | 'none'                    // No bank accounts (ineligible)
  | 'single'                  // One eligible NZ bank account
  | 'multiple'                // 2-3 eligible bank accounts

export type A2AOnboardingStatus =
  | 'not_started'             // Feature available but never initiated
  | 'partially_complete'      // Started OAuth but not finished
  | 'complete_enabled'        // Setup complete, Pay by bank active
  | 'complete_disabled'       // Setup complete but paused/disabled

export type FlowVariant =
  | 'balanced'                // Multi-step flow (more hand-holding)
  | 'aggressive'              // Single-step flow (fast & minimal)

export type BusinessType =
  | 'b2c_high'                // B2C with high invoice volume
  | 'b2c_low'                 // B2C with low online win-rate (target cohort)
  | 'b2b'                     // B2B focused (edge case for A2A)

export type RegionEligibility =
  | 'eligible'                // NZ org, meets all requirements
  | 'ineligible'              // Non-NZ or missing prerequisites

/**
 * Complete prototype configuration state
 */
export interface PrototypeConfig {
  stripe: StripeStatus
  bankAccounts: BankAccountSetup
  a2aStatus: A2AOnboardingStatus
  flowVariant: FlowVariant
  businessType: BusinessType
  regionEligibility: RegionEligibility
}

/**
 * Preset scenarios for quick testing
 */
export const PRESET_SCENARIOS: Record<string, { name: string; description: string; config: PrototypeConfig }> = {
  fresh_start: {
    name: "Fresh Start",
    description: "No Stripe, single bank account, first-time setup",
    config: {
      stripe: 'none',
      bankAccounts: 'single',
      a2aStatus: 'not_started',
      flowVariant: 'balanced',
      businessType: 'b2c_high',
      regionEligibility: 'eligible'
    }
  },
  
  stripe_competitor: {
    name: "Stripe Competitor",
    description: "Stripe active, multiple accounts, low online win-rate (target cohort)",
    config: {
      stripe: 'active',
      bankAccounts: 'multiple',
      a2aStatus: 'not_started',
      flowVariant: 'balanced',
      businessType: 'b2c_low',
      regionEligibility: 'eligible'
    }
  },
  
  needs_completion: {
    name: "Needs Completion",
    description: "Started setup but didn't finish - resume flow",
    config: {
      stripe: 'low_use',
      bankAccounts: 'multiple',
      a2aStatus: 'partially_complete',
      flowVariant: 'balanced',
      businessType: 'b2c_high',
      regionEligibility: 'eligible'
    }
  },
  
  power_user: {
    name: "Power User",
    description: "Fully configured, testing management & edge cases",
    config: {
      stripe: 'active',
      bankAccounts: 'multiple',
      a2aStatus: 'complete_enabled',
      flowVariant: 'aggressive',
      businessType: 'b2c_high',
      regionEligibility: 'eligible'
    }
  },
  
  ineligible_org: {
    name: "Ineligible Org",
    description: "No bank accounts or non-NZ - blocked state",
    config: {
      stripe: 'none',
      bankAccounts: 'none',
      a2aStatus: 'not_started',
      flowVariant: 'balanced',
      businessType: 'b2c_low',
      regionEligibility: 'ineligible'
    }
  },
  
  fast_onboarding: {
    name: "Fast Onboarding (Aggressive)",
    description: "Test single-step flow variant",
    config: {
      stripe: 'none',
      bankAccounts: 'single',
      a2aStatus: 'not_started',
      flowVariant: 'aggressive',
      businessType: 'b2c_high',
      regionEligibility: 'eligible'
    }
  }
}

/**
 * Default configuration (safe starting point)
 */
export const DEFAULT_CONFIG: PrototypeConfig = PRESET_SCENARIOS.fresh_start.config

/**
 * Get user-friendly labels for config options
 */
export const CONFIG_LABELS = {
  stripe: {
    none: 'No Stripe',
    active: 'Stripe Connected & Active',
    low_use: 'Stripe Connected (Low Use)'
  },
  bankAccounts: {
    none: 'No Bank Accounts (Ineligible)',
    single: 'Single Bank Account',
    multiple: 'Multiple Bank Accounts'
  },
  a2aStatus: {
    not_started: 'Not Started',
    partially_complete: 'Partially Complete',
    complete_enabled: 'Complete & Enabled',
    complete_disabled: 'Complete & Disabled'
  },
  flowVariant: {
    balanced: 'Balanced (Multi-step)',
    aggressive: 'Aggressive (Single-step)'
  },
  businessType: {
    b2c_high: 'B2C High Volume',
    b2c_low: 'B2C Low Win-rate',
    b2b: 'B2B Focused'
  },
  regionEligibility: {
    eligible: 'NZ Eligible',
    ineligible: 'Ineligible'
  }
} as const
