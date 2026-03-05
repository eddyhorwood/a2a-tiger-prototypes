import { useState, useEffect } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import XUIStepper from '@xero/xui/react/stepper'
import { XUICompositionDetail } from '@xero/xui/react/compositions'
import IntroStep from '../components/onboarding/IntroStep'
import SettlementAccountStep from '../components/onboarding/SettlementAccountStep'
import GuardrailsStep from '../components/onboarding/GuardrailsStep'
import ConfirmationStep from '../components/onboarding/ConfirmationStep'
import { api, BankAccount } from '../services/api'
import { parseEntryContext, getEntrySourceLabel, type EntryContext } from '../types/EntryContext'
import './OnboardingWizard.css'

enum WizardPage {
  Intro = 0,
  SelectAccount = 1,
  Guardrails = 2,
  Confirmation = 3
}

interface WizardStep {
  name: string
  isDisabled: boolean
  wizardPage: WizardPage
}

function OnboardingWizard() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  
  // Parse entry context from URL params
  const entryContext: EntryContext = parseEntryContext(searchParams)
  
  const [currentStep, setCurrentStep] = useState<WizardPage>(WizardPage.Intro)
  const [selectedAccountId, setSelectedAccountId] = useState<string>('')
  const [selectedAccount, setSelectedAccount] = useState<BankAccount | null>(null)
  const [accounts, setAccounts] = useState<BankAccount[]>([])
  const [acknowledged, setAcknowledged] = useState(false)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    // Load bank accounts when component mounts
    loadAccounts()
    
    // Log entry context for debugging
    console.log('Entry context:', {
      source: getEntrySourceLabel(entryContext.source),
      mode: entryContext.mode,
      returnTo: entryContext.returnTo,
      metadata: entryContext.metadata
    })
  }, [])

  const loadAccounts = async () => {
    try {
      const data = await api.getEligibleAccounts()
      setAccounts(data)
    } catch (error) {
      console.error('Failed to load accounts:', error)
    }
  }

  const tabs: WizardStep[] = [
    {
      name: 'Introduction',
      isDisabled: currentStep < WizardPage.Intro,
      wizardPage: WizardPage.Intro
    },
    {
      name: 'Settlement account',
      isDisabled: currentStep < WizardPage.SelectAccount,
      wizardPage: WizardPage.SelectAccount
    },
    {
      name: 'Terms & conditions',
      isDisabled: currentStep < WizardPage.Guardrails,
      wizardPage: WizardPage.Guardrails
    }
  ]

  // Convert to XUIStepper format
  const steps = tabs.map((tab, index) => ({
    name: tab.name,
    isComplete: currentStep > index,
    isDisabled: tab.isDisabled
  }))

  const handleNext = () => {
    if (currentStep < WizardPage.Confirmation) {
      setCurrentStep(currentStep + 1)
    }
  }

  const handleBack = () => {
    if (currentStep > WizardPage.Intro) {
      setCurrentStep(currentStep - 1)
    }
  }

  const handleAccountSelect = (accountId: string) => {
    setSelectedAccountId(accountId)
    const account = accounts.find(a => a.accountId === accountId)
    setSelectedAccount(account || null)
  }

  const handleEnablePayByBank = async () => {
    if (!selectedAccountId || !acknowledged) {
      return
    }

    setLoading(true)
    try {
      await api.updateConfig({
        enabled: true,
        settlement_account_id: selectedAccountId
      })
      setCurrentStep(WizardPage.Confirmation)
    } catch (error) {
      console.error('Failed to enable Pay by bank:', error)
    } finally {
      setLoading(false)
    }
  }

  const handleComplete = () => {
    // Navigate to return location if specified, otherwise default to settings
    const returnPath = entryContext.returnTo || '/settings/online-payments'
    navigate(returnPath)
  }

  const renderStep = () => {
    switch (currentStep) {
      case WizardPage.Intro:
        return <IntroStep onNext={handleNext} />
      
      case WizardPage.SelectAccount:
        return (
          <SettlementAccountStep
            accounts={accounts}
            selectedAccountId={selectedAccountId}
            onAccountSelect={handleAccountSelect}
            onNext={handleNext}
            onBack={handleBack}
          />
        )
      
      case WizardPage.Guardrails:
        return (
          <GuardrailsStep
            acknowledged={acknowledged}
            onAcknowledgeChange={setAcknowledged}
            onSubmit={handleEnablePayByBank}
            onBack={handleBack}
            loading={loading}
          />
        )
      
      case WizardPage.Confirmation:
        return (
          <ConfirmationStep
            account={selectedAccount}
            onComplete={handleComplete}
          />
        )
      
      default:
        return null
    }
  }

  const showStepper = currentStep !== WizardPage.Confirmation

  return (
    <div className="onboarding-wizard">
      {/* Entry context indicator (for demo/debugging) */}
      {entryContext.source !== 'settings' && (
        <div className="entry-context-banner">
          <span className="context-label">Entry:</span> {getEntrySourceLabel(entryContext.source)}
          {entryContext.mode === 'manage' && <span className="mode-badge">Manage</span>}
        </div>
      )}
      
      {showStepper && (
        <div className="onboarding-wizard-stepper">
          <XUIStepper
            ariaLabel="Pay by bank onboarding progress"
            currentStep={currentStep}
            id="pay-by-bank-stepper"
            stepTitle="Step"
            steps={steps}
            updateCurrentStep={(index: number) => {
              if (index <= currentStep) {
                setCurrentStep(tabs[index].wizardPage)
              }
            }}
          />
        </div>
      )}
      <div className="onboarding-wizard-content">
        <XUICompositionDetail detail={renderStep()} hasAutoSpaceAround={false} />
      </div>
    </div>
  )
}

export default OnboardingWizard
