import { useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import XUIButton from '@xero/xui/react/button'
import { parseEntryContext, type EntryContext } from '../types/EntryContext'
import { balancedFlowContent } from '../config/onboardingContent'
import { getEligibleSettlementAccounts, mockXeroOrg, formatAccountDisplay, mockAkahuBanks, mockDirectors } from '../mocks/xeroOrgData'
import './OnboardingWizardNew.css'

// Screen enum for the new flow
enum OnboardingScreen {
  Checklist = 0,
  BusinessDetails = 1,
  SelectAccount = 2,
  ReviewConsent = 3,
  AkahuOAuth = 4,
  Success = 5
}

function OnboardingWizard() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const entryContext: EntryContext = parseEntryContext(searchParams)
  
  // State
  const [currentScreen, setCurrentScreen] = useState<OnboardingScreen>(OnboardingScreen.Checklist)
  const [selectedAccountId, setSelectedAccountId] = useState<string>('')
  const [selectedBank, setSelectedBank] = useState('')
  const [businessStructure, setBusinessStructure] = useState<string>('sole-trader')
  
  // Mock data
  const eligibleAccounts = getEligibleSettlementAccounts()
  const content = balancedFlowContent
  
  // Get initials for org badge
  const getOrgInitials = (name: string): string => {
    return name
      .split(' ')
      .filter(word => word.length > 0)
      .slice(0, 2)
      .map(word => word[0].toUpperCase())
      .join('')
  }
  
  // Auto-select if only one account
  if (eligibleAccounts.length === 1 && !selectedAccountId && currentScreen === OnboardingScreen.SelectAccount) {
    setSelectedAccountId(eligibleAccounts[0].id)
  }
  
  // Navigation handlers
  const handleNext = () => {
    setCurrentScreen(currentScreen + 1)
  }
  
  const handleBack = () => {
    setCurrentScreen(currentScreen - 1)
  }
  
  const handleSaveAndExit = () => {
    // TODO: Save state for resume capability
    const returnPath = entryContext.returnTo || '/settings/online-payments'
    navigate(returnPath)
  }
  
  const handleComplete = () => {
    const returnPath = entryContext.returnTo || '/settings/online-payments'
    navigate(returnPath)
  }
  
  const handleConnectAkahu = () => {
    // Move to OAuth screen
    setCurrentScreen(OnboardingScreen.AkahuOAuth)
  }
  
  const handleAkahuApprove = () => {
    // Simulate OAuth completion after 2 seconds
    setTimeout(() => {
      setCurrentScreen(OnboardingScreen.Success)
    }, 2000)
  }
  
  // Render stepper (left sidebar)
  const renderStepper = () => {
    if (currentScreen === OnboardingScreen.Checklist || currentScreen === OnboardingScreen.AkahuOAuth) {
      return null // No stepper on checklist or OAuth screens
    }
    
    const steps = [
      { label: 'Business details', screen: OnboardingScreen.BusinessDetails },
      { label: 'Personal details', screen: OnboardingScreen.SelectAccount },
      { label: 'Review & submit for verification', screen: OnboardingScreen.ReviewConsent },
    ]
    
    if (currentScreen === OnboardingScreen.Success) {
      steps.push({ label: content.screen5.stepLabel, screen: OnboardingScreen.Success })
    }
    
    return (
      <div className="onboarding-stepper">
        {steps.map((step, index) => {
          const isActive = currentScreen === step.screen
          const isComplete = currentScreen > step.screen
          
          return (
            <div key={index} className={`stepper-step ${isActive ? 'active' : ''} ${isComplete ? 'complete' : ''}`}>
              <div className="stepper-circle">
                {isComplete ? '✓' : index + 1}
              </div>
              <div className="stepper-label">{step.label}</div>
            </div>
          )
        })}
      </div>
    )
  }
  
  // Screen 1: Checklist
  const renderChecklistScreen = () => (
    <div className="onboarding-screen checklist-screen">
      <div className="checklist-header-centered">
        <h1 className="xui-heading-xlarge xui-margin-bottom-small">{content.screen1.heading}</h1>
        <p className="xui-font-size-medium xui-margin-bottom-large">
          Select your business structure to see what documents you'll need
        </p>
      </div>
      
      <div className="form-group">
        <label htmlFor="business-type" className="xui-font-size-small xui-margin-bottom-xsmall">Business type</label>
        <select
          id="business-type"
          value={businessStructure}
          onChange={(e) => setBusinessStructure(e.target.value)}
          className="business-structure-select"
        >
          <option value="sole-trader">Individual or sole trader</option>
          <option value="partnership">Partnership</option>
          <option value="company">Company</option>
          <option value="trust">Trust</option>
        </select>
      </div>
      
      <div className="what-expect-section xui-margin-top-xlarge">
        <h2 className="xui-heading-medium xui-margin-bottom-small">What to expect</h2>
        <p className="xui-font-size-medium xui-margin-bottom-medium">
          It will take about 3-5 minutes to complete if you have this information ready:
        </p>
      </div>
      
      <div className="checklist">
        {content.screen1.checklist.map((item, index) => (
          <div key={index} className="checklist-item xui-margin-bottom-medium">
            <div className="checklist-icon">{item.icon}</div>
            <div className="checklist-content">
              <h3 className="xui-heading-small xui-margin-bottom-xsmall">{item.title}</h3>
              <p className="xui-font-size-medium">{item.description}</p>
            </div>
          </div>
        ))}
      </div>
      
      <div className="data-protection-box xui-margin-top-large">
        <div className="data-protection-header">
          <span className="protection-icon">🔒</span>
          <span className="xui-font-size-medium">We protect your data</span>
        </div>
        <p className="xui-font-size-small xui-margin-top-xsmall">
          {content.screen1.dataProtectionText}{' '}
          <a href="#" className="learn-more-link">{content.screen1.dataProtectionLink}</a>
        </p>
      </div>
      
      <div className="screen-actions-centered xui-margin-top-xlarge">
        <XUIButton variant="main" onClick={handleNext}>
          {content.screen1.cta}
        </XUIButton>
      </div>
      
      <div className="invite-owner-section xui-margin-top-large">
        <h3 className="xui-heading-small xui-margin-bottom-xsmall">Don't have the information?</h3>
        <div className="invite-owner-card">
          <div className="invite-content">
            <span className="invite-icon">✉️</span>
            <div>
              <div className="xui-font-size-medium">Invite owner to complete</div>
              <div className="xui-font-size-small" style={{ color: 'var(--text-muted)' }}>We'll email the owner to complete their details</div>
            </div>
          </div>
          <XUIButton variant="borderless-main" onClick={() => {}}>Invite</XUIButton>
        </div>
      </div>
    </div>
  )
  
  // Screen 2: Select Settlement Account
  const renderSelectAccountScreen = () => (
    <div className="onboarding-screen account-screen">
      <div className="screen-header-with-exit">
        <h1 className="xui-heading-xlarge">{content.screen2.heading}</h1>
        <XUIButton variant="borderless-main" onClick={handleSaveAndExit}>Save and exit</XUIButton>
      </div>
      
      <div className="org-badge xui-margin-top-large xui-font-size-medium">{mockXeroOrg.organisationName}</div>
      
      <div className="form-group xui-margin-top-large">
        <label htmlFor="settlement-account" className="xui-font-size-medium xui-margin-bottom-xsmall">{content.screen2.label}</label>
        <p className="xui-font-size-small xui-margin-bottom-small">{content.screen2.helpText}</p>
        
        {eligibleAccounts.length === 0 ? (
          <div className="error-message xui-font-size-medium">
            {content.screen2.noAccountsMessage}
          </div>
        ) : (
          <select
            id="settlement-account"
            value={selectedAccountId}
            onChange={(e) => setSelectedAccountId(e.target.value)}
            className="account-select"
          >
            <option value="">Select an account...</option>
            {eligibleAccounts.map(account => (
              <option key={account.id} value={account.id}>
                {formatAccountDisplay(account)}
              </option>
            ))}
            <option value="add-new" disabled>{content.screen2.addNewAccountText}</option>
          </select>
        )}
      </div>
      
      <div className="legal-footer xui-margin-top-large xui-font-size-small">
        {content.screen2.legalFooter}{' '}
        <a href="#" className="learn-more-link">{content.screen2.legalFooterLink}</a>
      </div>
      
      <div className="screen-actions xui-margin-top-xlarge">
        <XUIButton variant="standard" onClick={handleBack}>Back</XUIButton>
        <XUIButton 
          variant="main" 
          onClick={handleNext}
          isDisabled={!selectedAccountId || eligibleAccounts.length === 0}
        >
          {content.screen2.cta}
        </XUIButton>
      </div>
    </div>
  )
  
  // Screen 3: Review and Consent
  const renderReviewConsentScreen = () => (
    <div className="onboarding-screen review-screen">
      <div className="review-header-centered">
        <div className="review-illustration">📋</div>
        <h1 className="xui-heading-xlarge xui-margin-top-medium xui-margin-bottom-small">Review and submit</h1>
        <p className="xui-font-size-medium">Take a moment to review your information.</p>
      </div>
      
      <div className="review-section xui-margin-top-xlarge">
        <h2 className="xui-heading-medium xui-margin-bottom-medium">Business details</h2>
        
        <div className="review-card">
          <div className="review-card-header">
            <div className="org-info">
              <div className="org-initials">{getOrgInitials(mockXeroOrg.organisationName)}</div>
              <span className="org-name xui-font-size-large">{mockXeroOrg.organisationName}</span>
            </div>
            <XUIButton variant="borderless-main" onClick={() => setCurrentScreen(OnboardingScreen.BusinessDetails)}>Edit</XUIButton>
          </div>
          
          <div className="info-grid">
            <div className="info-row">
              <span className="info-label xui-font-size-small">Legal business name</span>
              <span className="info-value xui-font-size-medium">{mockXeroOrg.organisationName}</span>
            </div>
            <div className="info-row">
              <span className="info-label xui-font-size-small">Business structure</span>
              <span className="info-value xui-font-size-medium">{mockXeroOrg.businessStructure}</span>
            </div>
            <div className="info-row">
              <span className="info-label xui-font-size-small">Business registration number</span>
              <span className="info-value xui-font-size-medium">{mockXeroOrg.registrationNumber}</span>
            </div>
            <div className="info-row">
              <span className="info-label xui-font-size-small">Business address</span>
              <span className="info-value xui-font-size-medium">{mockXeroOrg.address}</span>
            </div>
            <div className="info-row">
              <span className="info-label xui-font-size-small">Country</span>
              <span className="info-value xui-font-size-medium">{mockXeroOrg.country}</span>
            </div>
          </div>
        </div>
      </div>
      
      <div className="review-section xui-margin-top-large">
        <h2 className="xui-heading-medium xui-margin-bottom-medium">Owners/directors</h2>
        
        {mockDirectors.map((director) => (
          <div key={director.id} className="review-card">
            <div className="review-card-header">
              <div className="org-info">
                <div className="org-initials director-initials">{getOrgInitials(director.name)}</div>
                <span className="org-name xui-font-size-large">{director.name}</span>
              </div>
              <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
                <XUIButton variant="borderless-main" onClick={() => {}}>Edit</XUIButton>
                <button className="menu-button" aria-label="More options">⋮</button>
              </div>
            </div>
            
            <div className="director-info">
              <div className="xui-font-size-small" style={{ color: 'var(--text-muted)' }}>Email: <span className="xui-font-size-medium" style={{ color: 'var(--text-heading)' }}>{director.email}</span></div>
              <div className="xui-font-size-small" style={{ color: 'var(--text-muted)' }}>Role: <span className="xui-font-size-medium" style={{ color: 'var(--text-heading)' }}>{director.role}</span></div>
              <div className="xui-font-size-small" style={{ color: 'var(--text-muted)' }}>Ownership: <span className="xui-font-size-medium" style={{ color: 'var(--text-heading)' }}>{director.ownership}</span></div>
            </div>
          </div>
        ))}
      </div>
      
      <div className="consent-text xui-margin-top-xlarge xui-font-size-small">
        By clicking 'Agree and submit' you give us your consent to share and receive the data set out above. You also agree to Xero's <a href="#" className="learn-more-link">Terms of Use</a> and <a href="#" className="learn-more-link">Privacy Policy</a>
      </div>
      
      <div className="screen-actions-centered xui-margin-top-large">
        <XUIButton 
          variant="main" 
          onClick={handleConnectAkahu}
        >
          Agree and submit
        </XUIButton>
      </div>
    </div>
  )
  
  // Screen 4: Akahu OAuth (mock)
  const renderAkahuOAuthScreen = () => (
    <div className="onboarding-screen oauth-screen">
      <div className="oauth-container">
        <div className="oauth-header">
          <h1 className="xui-heading-large xui-margin-bottom-small">Authorize Xero to initiate bank payments</h1>
          <p className="xui-font-size-medium">You will be redirected to securely connect your bank account</p>
        </div>
        
        <div className="form-group xui-margin-top-large">
          <label htmlFor="bank-select" className="xui-font-size-medium xui-margin-bottom-xsmall">Choose your bank</label>
          <select
            id="bank-select"
            value={selectedBank}
            onChange={(e) => setSelectedBank(e.target.value)}
            className="bank-select"
          >
            <option value="">Select a bank...</option>
            {mockAkahuBanks.map(bank => (
              <option key={bank.id} value={bank.id}>{bank.name}</option>
            ))}
          </select>
        </div>
        
        <div className="permissions-list xui-margin-top-large">
          <h3 className="xui-heading-small xui-margin-bottom-small">Permissions</h3>
          <ul className="xui-list">
            <li className="xui-font-size-medium">Initiate payments on your behalf</li>
          </ul>
        </div>
        
        <div className="screen-actions xui-margin-top-xlarge">
          <XUIButton 
            variant="main" 
            onClick={handleAkahuApprove}
            isDisabled={!selectedBank}
          >
            Approve
          </XUIButton>
        </div>
      </div>
    </div>
  )
  
  // Screen 5: Success
  const renderSuccessScreen = () => (
    <div className="onboarding-screen success-screen">
      <div className="success-icon">✓</div>
      <h1 className="xui-heading-xlarge xui-margin-top-medium">{content.screen5.heading}</h1>
      <p className="xui-font-size-medium xui-margin-top-small">{content.screen5.confirmation}</p>
      
      <div className="what-next-section xui-margin-top-large">
        <h2 className="xui-heading-medium xui-margin-bottom-medium">{content.screen5.whatNextHeading}</h2>
        <ul className="xui-list">
          {content.screen5.whatNext.map((item, index) => (
            <li key={index} className="xui-font-size-medium xui-margin-bottom-small">{item}</li>
          ))}
        </ul>
      </div>
      
      <div className="screen-actions xui-margin-top-xlarge">
        <XUIButton variant="main" onClick={handleComplete}>
          {content.screen5.cta}
        </XUIButton>
      </div>
    </div>
  )
  
  // Screen 2: Business Details Review
  const renderBusinessDetailsScreen = () => (
    <div className="onboarding-screen business-details-screen">
      <div className="screen-header-with-exit">
        <h1 className="xui-heading-xlarge">Tell us about your business</h1>
        <XUIButton variant="borderless-main" onClick={handleSaveAndExit}>Save and exit</XUIButton>
      </div>
      
      <p className="xui-font-size-medium xui-margin-top-small">
        This information is collected to better serve your business and comply with regulators and financial partners.{' '}
        <a href="#" className="learn-more-link">Why we need this?</a>
      </p>
      
      <div className="auto-fill-notice xui-margin-top-large">
        <span className="notice-icon">✓</span>
        <span className="xui-font-size-medium">We've auto-filled the information based on your record in Xero</span>
      </div>
      
      <div className="business-info-card xui-margin-top-large">
        <div className="card-header">
          <div className="org-info">
            <div className="org-initials">{getOrgInitials(mockXeroOrg.organisationName)}</div>
            <span className="org-name xui-font-size-large">{mockXeroOrg.organisationName}</span>
          </div>
          <XUIButton variant="borderless-main" onClick={() => {}}>Edit</XUIButton>
        </div>
        
        <div className="info-grid">
          <div className="info-row">
            <span className="info-label xui-font-size-small">Legal business name</span>
            <span className="info-value xui-font-size-medium">{mockXeroOrg.organisationName}</span>
          </div>
          <div className="info-row">
            <span className="info-label xui-font-size-small">Business structure</span>
            <span className="info-value xui-font-size-medium">{mockXeroOrg.businessStructure}</span>
          </div>
          <div className="info-row">
            <span className="info-label xui-font-size-small">Business registration number</span>
            <span className="info-value xui-font-size-medium">{mockXeroOrg.registrationNumber}</span>
          </div>
          <div className="info-row">
            <span className="info-label xui-font-size-small">Business address</span>
            <span className="info-value xui-font-size-medium">{mockXeroOrg.address}</span>
          </div>
          <div className="info-row">
            <span className="info-label xui-font-size-small">Country</span>
            <span className="info-value xui-font-size-medium">{mockXeroOrg.country}</span>
          </div>
        </div>
      </div>
      
      <div className="security-notice xui-margin-top-large">
        <span className="notice-icon-small">🛡️</span>
        <span className="xui-font-size-small">
          Your information is encrypted and stored securely. We use this data solely to verify your business identity and comply with anti-money laundering laws.
        </span>
      </div>
      
      <div className="screen-actions xui-margin-top-xlarge">
        <XUIButton variant="standard" onClick={handleBack}>Back</XUIButton>
        <XUIButton variant="main" onClick={handleNext}>
          Continue
        </XUIButton>
      </div>
    </div>
  )
  
  // Main render
  const renderCurrentScreen = () => {
    switch (currentScreen) {
      case OnboardingScreen.Checklist:
        return renderChecklistScreen()
      case OnboardingScreen.BusinessDetails:
        return renderBusinessDetailsScreen()
      case OnboardingScreen.SelectAccount:
        return renderSelectAccountScreen()
      case OnboardingScreen.ReviewConsent:
        return renderReviewConsentScreen()
      case OnboardingScreen.AkahuOAuth:
        return renderAkahuOAuthScreen()
      case OnboardingScreen.Success:
        return renderSuccessScreen()
      default:
        return null
    }
  }
  
  const showStepper = currentScreen !== OnboardingScreen.Checklist && 
                      currentScreen !== OnboardingScreen.AkahuOAuth
  
  return (
    <div className="onboarding-wizard-v2">
      {showStepper && renderStepper()}
      <div className="onboarding-main-content">
        {renderCurrentScreen()}
      </div>
    </div>
  )
}

export default OnboardingWizard
