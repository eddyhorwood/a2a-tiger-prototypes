// Onboarding flow content - easily editable for workshop experiments

export interface ChecklistItem {
  icon: string
  title: string
  description: string
}

export interface FlowContent {
  screen1: {
    modalTitle: string
    heading: string
    subheading?: string
    timeEstimate: string
    checklist: ChecklistItem[]
    dataProtectionText: string
    dataProtectionLink: string
    cta: string
  }
  screen2: {
    stepLabel: string
    heading: string
    label: string
    helpText: string
    noAccountsMessage: string
    addNewAccountText: string
    legalFooter: string
    legalFooterLink: string
    cta: string
  }
  screen3: {
    stepLabel: string
    heading: string
    summaryLabel: string
    explainerHeading: string
    explainerSteps: string[]
    explainerEmphasis: string
    consentCheckbox: string
    termsLink: string
    cta: string
  }
  screen5: {
    stepLabel: string
    heading: string
    confirmation: string
    whatNextHeading: string
    whatNext: string[]
    cta: string
    ctaSecondary: string
  }
}

export const balancedFlowContent: FlowContent = {
  screen1: {
    modalTitle: "Set up online payments",
    heading: "A checklist before you start",
    subheading: "Select your business structure to see what documents you'll need",
    timeEstimate: "It will take about 2-3 minutes to complete if you have this information ready",
    checklist: [
      {
        icon: "📊",
        title: "Settlement account",
        description: "Choose where customer payments will be deposited (from your Xero chart of accounts)"
      },
      {
        icon: "🔐",
        title: "Connect to Akahu",
        description: "Secure provider for direct bank-to-bank payments"
      },
      {
        icon: "✅",
        title: "Review and enable",
        description: "Understand how Pay by bank works and complete setup"
      }
    ],
    dataProtectionText: "Xero takes a defence-in-depth approach to protecting our systems and your data.",
    dataProtectionLink: "Learn more about security at Xero",
    cta: "Start now"
  },
  screen2: {
    stepLabel: "Business details",
    heading: "Choose settlement account",
    label: "Settlement account",
    helpText: "Where should customer payments be deposited?",
    noAccountsMessage: "You need at least one bank account in your chart of accounts to continue",
    addNewAccountText: "+ Add new bank account...",
    legalFooter: "Xero and Akahu don't hold funds. Payments are direct bank-to-bank transfers.",
    legalFooterLink: "Read more",
    cta: "Continue"
  },
  screen3: {
    stepLabel: "Review & submit",
    heading: "Review and connect to Akahu",
    summaryLabel: "Settlement account",
    explainerHeading: "How Pay by bank works",
    explainerSteps: [
      "Customers choose their bank and approve the payment",
      "Akahu securely initiates the direct bank transfer",
      "Funds go directly to your bank account"
    ],
    explainerEmphasis: "Xero and Akahu never hold your money.",
    consentCheckbox: "I understand that payments are direct bank-to-bank transfers. Xero and Akahu do not hold funds.",
    termsLink: "View full terms and conditions",
    cta: "Connect to Akahu"
  },
  screen5: {
    stepLabel: "Complete",
    heading: "Pay by bank is now enabled",
    confirmation: "You're all set to accept direct bank payments from your customers.",
    whatNextHeading: "What happens next",
    whatNext: [
      "Customers will see 'Pay by bank' as a payment option on invoices you send",
      "Payments typically clear in 1-2 business days",
      "You can manage your settlement account in Online payments settings"
    ],
    cta: "Go to Online payments settings",
    ctaSecondary: "Back to invoice"
  }
}

// Placeholder for future flows
export const aggressiveFlowContent: Partial<FlowContent> = {
  // TODO: Clone from balanced and modify
}

export const safeFlowContent: Partial<FlowContent> = {
  // TODO: Clone from balanced and add more explanation
}
