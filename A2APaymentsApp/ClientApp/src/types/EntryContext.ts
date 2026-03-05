/**
 * Entry Context
 * 
 * Tracks how a user entered the Pay by bank onboarding flow
 * and where they should return after completion.
 */

export type EntrySource =
  | 'settings'                  // Online Payments settings tile
  | 'invoice.inline'           // Invoice screen inline prompt
  | 'invoice.modal'            // Invoice payment method modal
  | 'banner'                   // Online Payments banner/promo
  | 'one_onboarding'           // One Onboarding checklist
  | 'campaign'                 // External campaign/email deep link
  | 'manage'                   // Manage/edit existing setup

export type EntryMode = 'first_time' | 'manage'

export type ComplianceVariant = 'modal' | 'banner' | 'fullscreen'

export interface EntryContext {
  /** Where the user came from */
  source: EntrySource
  
  /** Whether this is first-time onboarding or managing existing setup */
  mode: EntryMode
  
  /** Compliance disclosure variant to show */
  complianceVariant?: ComplianceVariant
  
  /** Optional return path after onboarding completes */
  returnTo?: string
  
  /** Optional organization ID (for deep links that switch orgs) */
  orgId?: string
  
  /** Additional metadata for tracking/analytics */
  metadata?: {
    campaignId?: string
    taskId?: string
    invoiceId?: string
    [key: string]: any
  }
}

/**
 * Parse entry context from URL search params
 */
export function parseEntryContext(searchParams: URLSearchParams): EntryContext {
  const source = (searchParams.get('source') || 'settings') as EntrySource
  const mode = (searchParams.get('mode') || 'first_time') as EntryMode
  const complianceVariant = (searchParams.get('complianceVariant') || undefined) as ComplianceVariant | undefined
  const returnTo = searchParams.get('returnTo') || undefined
  const orgId = searchParams.get('orgId') || undefined
  
  // Parse metadata from any other params
  const metadata: Record<string, any> = {}
  searchParams.forEach((value, key) => {
    if (!['source', 'mode', 'complianceVariant', 'returnTo', 'orgId'].includes(key)) {
      metadata[key] = value
    }
  })
  
  return {
    source,
    mode,
    complianceVariant,
    returnTo,
    orgId,
    metadata: Object.keys(metadata).length > 0 ? metadata : undefined
  }
}

/**
 * Serialize entry context to URL search params
 */
export function serializeEntryContext(context: EntryContext): URLSearchParams {
  const params = new URLSearchParams()
  
  params.set('source', context.source)
  params.set('mode', context.mode)
  
  if (context.complianceVariant) params.set('complianceVariant', context.complianceVariant)
  if (context.returnTo) params.set('returnTo', context.returnTo)
  if (context.orgId) params.set('orgId', context.orgId)
  
  if (context.metadata) {
    Object.entries(context.metadata).forEach(([key, value]) => {
      params.set(key, String(value))
    })
  }
  
  return params
}

/**
 * Get user-friendly entry source label (for analytics/debugging)
 */
export function getEntrySourceLabel(source: EntrySource): string {
  const labels: Record<EntrySource, string> = {
    'settings': 'Online Payments Settings',
    'invoice.inline': 'Invoice - Inline Prompt',
    'invoice.modal': 'Invoice - Payment Method Modal',
    'banner': 'Online Payments Banner',
    'one_onboarding': 'One Onboarding',
    'campaign': 'External Campaign',
    'manage': 'Manage Existing Setup'
  }
  return labels[source]
}
