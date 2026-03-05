// Payment Service Types for Online Payments Settings

export type ProviderStatus = 'NOT_CONFIGURED' | 'SETUP_STARTED' | 'SETUP_COMPLETE' | 'ERROR'

export interface PaymentServiceConfig {
  id: string
  name: string
  provider: string
  description: string
  longDescription: string
  status: ProviderStatus
  methods: string[]
  features: string[]
  settlementAccount?: {
    name: string
    maskedNumber: string
  }
  feeAccount?: string
  pricing: string
  setupTime: string
  region: string[]
  icon: 'bank' | 'card' | 'debit'
}
