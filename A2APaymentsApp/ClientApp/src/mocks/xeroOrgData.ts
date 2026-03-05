// Mock Xero organization data for prototype

export interface BankAccount {
  id: string
  name: string
  code: string
  type: 'BANK' | 'CREDITCARD' | 'CURRENT'
  isEnablePayments: boolean
  bankAccountNumberMasked: string
}

export interface Director {
  id: string
  name: string
  email: string
  role: string
  ownership: string
}

export interface XeroOrg {
  organisationID: string
  organisationName: string
  contactName: string
  emailAddress: string
  address: string
  businessStructure: 'Sole trader' | 'Partnership' | 'Company' | 'Trust'
  registrationNumber: string
  country: string
}

// Mock bank accounts from chart of accounts
export const mockBankAccounts: BankAccount[] = [
  {
    id: "acc-001",
    name: "ANZ Business Account",
    code: "090",
    type: "BANK",
    isEnablePayments: true,
    bankAccountNumberMasked: "•••• 1234"
  },
  {
    id: "acc-002",
    name: "Westpac Operating Account",
    code: "545",
    type: "BANK",
    isEnablePayments: true,
    bankAccountNumberMasked: "•••• 5678"
  },
  {
    id: "acc-003",
    name: "Savings Account",
    code: "212",
    type: "BANK",
    isEnablePayments: false, // Edge case: not enabled for payments
    bankAccountNumberMasked: "•••• 9012"
  }
]

// Mock Xero organization details
export const mockXeroOrg: XeroOrg = {
  organisationID: "org-12345",
  organisationName: "Acme Consulting Ltd",
  contactName: "Jordan Smith",
  emailAddress: "jordan@acmeconsulting.co.nz",
  address: "123 Queen Street, Auckland 1010",
  businessStructure: "Sole trader",
  registrationNumber: "123456789",
  country: "New Zealand"
}

// Get eligible settlement accounts (BANK type + enabled for payments)
export const getEligibleSettlementAccounts = (): BankAccount[] => {
  return mockBankAccounts.filter(acc => 
    acc.type === 'BANK' && acc.isEnablePayments
  )
}

// Format account for display in dropdown
export const formatAccountDisplay = (account: BankAccount): string => {
  return `${account.name} (${account.code}) ${account.bankAccountNumberMasked}`
}

// Mock Akahu banks
export const mockAkahuBanks = [
  { id: "anz", name: "ANZ" },
  { id: "westpac", name: "Westpac" },
  { id: "bnz", name: "BNZ" },
  { id: "asb", name: "ASB" },
  { id: "kiwibank", name: "Kiwibank" }
]

// Mock directors/owners
export const mockDirectors: Director[] = [
  {
    id: "dir-001",
    name: "Jordan Smith",
    email: "jordan@acmeconsulting.co.nz",
    role: "Owner and director",
    ownership: "100%"
  }
]
