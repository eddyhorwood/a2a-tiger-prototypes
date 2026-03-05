// Mock API for stubbed data
export interface BankAccount {
  accountId: string
  name: string
  accountNumber: string
  type: string
  currencyCode: string
}

export interface A2AConfig {
  enabled: boolean
  settlement_account_id: string | null
}

// Mock bank accounts
const mockBankAccounts: BankAccount[] = [
  {
    accountId: '550e8400-e29b-41d4-a716-446655440000',
    name: 'Business Cheque Account',
    accountNumber: '12-3456-7890123-00',
    type: 'BANK',
    currencyCode: 'NZD'
  },
  {
    accountId: '550e8400-e29b-41d4-a716-446655440001',
    name: 'Business Savings',
    accountNumber: '12-3456-7890456-00',
    type: 'BANK',
    currencyCode: 'NZD'
  },
  {
    accountId: '550e8400-e29b-41d4-a716-446655440002',
    name: 'Operating Account',
    accountNumber: '12-3456-7890789-00',
    type: 'BANK',
    currencyCode: 'NZD'
  }
]

// Simulate API delay
const delay = (ms: number) => new Promise(resolve => setTimeout(resolve, ms))

export const api = {
  async getEligibleAccounts(): Promise<BankAccount[]> {
    await delay(500)
    return mockBankAccounts
  },

  async getConfig(): Promise<A2AConfig> {
    await delay(300)
    const stored = localStorage.getItem('a2a_config')
    if (stored) {
      return JSON.parse(stored)
    }
    return {
      enabled: false,
      settlement_account_id: null
    }
  },

  async updateConfig(config: A2AConfig): Promise<{ success: boolean; config: A2AConfig }> {
    await delay(500)
    localStorage.setItem('a2a_config', JSON.stringify(config))
    return {
      success: true,
      config
    }
  }
}
