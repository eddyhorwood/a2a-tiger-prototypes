import { useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import XUIButton from '@xero/xui/react/button'
import XUISelectBox, { XUISelectBoxOption } from '@xero/xui/react/selectbox'
import XUITextInput from '@xero/xui/react/textinput'
import { parseEntryContext, type EntryContext } from '../types/EntryContext'
import { getEligibleSettlementAccounts, formatAccountDisplay } from '../mocks/xeroOrgData'
import './OnboardingWizardAggressive.css'

// Aggressive flow: Just select bank account → done
enum OnboardingScreen {
  SelectAccount = 0,
  Success = 1
}

function OnboardingWizardAggressive() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const entryContext: EntryContext = parseEntryContext(searchParams)
  
  // State
  const [currentScreen, setCurrentScreen] = useState<OnboardingScreen>(OnboardingScreen.SelectAccount)
  const [selectedAccountId, setSelectedAccountId] = useState<string>('')
  const [isEditingAccount, setIsEditingAccount] = useState(false)
  const [editedAccountNumber, setEditedAccountNumber] = useState('')
  const [editedBsb, setEditedBsb] = useState('')
  
  // Get eligible accounts
  const eligibleAccounts = getEligibleSettlementAccounts()
  const selectedAccount = eligibleAccounts.find(acc => acc.id === selectedAccountId)
  
  const handleAccountSelect = (accountId: string) => {
    setSelectedAccountId(accountId)
    setIsEditingAccount(false)
    // Pre-fill edit fields with selected account data
    const account = eligibleAccounts.find(acc => acc.id === accountId)
    if (account) {
      setEditedAccountNumber(account.bankAccountNumberMasked)
      setEditedBsb(account.code)
    }
  }
  
  const handleEditAccount = () => {
    setIsEditingAccount(true)
  }
  
  const handleCancelEdit = () => {
    setIsEditingAccount(false)
    // Reset to original values
    if (selectedAccount) {
      setEditedAccountNumber(selectedAccount.bankAccountNumberMasked)
      setEditedBsb(selectedAccount.code)
    }
  }
  
  const handleSaveEdit = () => {
    setIsEditingAccount(false)
    // In a real app, this would update the account details
  }
  
  const handleSubmit = () => {
    // Simulate submission delay
    setTimeout(() => {
      setCurrentScreen(OnboardingScreen.Success)
    }, 1000)
  }
  
  const handleComplete = () => {
    const returnPath = entryContext.returnTo || '/settings/online-payments'
    navigate(returnPath)
  }
  
  // Screen 1: Select Account
  const renderSelectAccountScreen = () => (
    <div className="aggressive-screen select-account-screen">
      <div className="screen-header-centered">
        <div className="screen-illustration">🏦</div>
        <h1 className="xui-heading-xlarge xui-margin-top-medium xui-margin-bottom-small">
          Choose your bank account
        </h1>
        <p className="xui-font-size-medium xui-text-color-muted">
          Select the bank account where you'd like to receive customer payments.
        </p>
      </div>
      
      <div className="account-selection-section xui-margin-top-xlarge">
        <XUISelectBox
          label="Settlement account"
          buttonContent={
            selectedAccount 
              ? formatAccountDisplay(selectedAccount)
              : 'Select an account...'
          }
        >
          {eligibleAccounts.map(account => (
            <XUISelectBoxOption 
              key={account.id} 
              id={account.id}
              value={account.id}
              isSelected={account.id === selectedAccountId}
              onSelect={(value: string) => handleAccountSelect(value)}
            >
              {formatAccountDisplay(account)}
            </XUISelectBoxOption>
          ))}
        </XUISelectBox>
        <p className="xui-font-size-small xui-text-color-muted xui-margin-top-xsmall">
          Choose where customer payments will be deposited
        </p>
        
        {selectedAccount && !isEditingAccount && (
          <div className="account-details-card xui-margin-top-medium">
            <div className="account-details-header">
              <h3 className="xui-heading-small">Account details</h3>
              <XUIButton 
                variant="borderless-standard" 
                onClick={handleEditAccount}
              >
                Edit
              </XUIButton>
            </div>
            <div className="account-details-body">
              <div className="detail-row">
                <span className="detail-label xui-font-size-small">Account name</span>
                <span className="detail-value xui-font-size-medium">{selectedAccount.name}</span>
              </div>
              <div className="detail-row">
                <span className="detail-label xui-font-size-small">BSB</span>
                <span className="detail-value xui-font-size-medium">{selectedAccount.code}</span>
              </div>
              <div className="detail-row">
                <span className="detail-label xui-font-size-small">Account number</span>
                <span className="detail-value xui-font-size-medium">{selectedAccount.bankAccountNumberMasked}</span>
              </div>
            </div>
          </div>
        )}
        
        {selectedAccount && isEditingAccount && (
          <div className="account-edit-card xui-margin-top-medium">
            <h3 className="xui-heading-small xui-margin-bottom-medium">Edit account details</h3>
            <div className="edit-form">
              <XUITextInput
                label="Account name"
                value={selectedAccount.name}
                isDisabled={true}
                hintMessage="Account name cannot be changed"
              />
              <div className="xui-margin-top-medium">
                <XUITextInput
                  label="BSB"
                  value={editedBsb}
                  onChange={(e) => setEditedBsb(e.target.value)}
                  placeholder="000-000"
                />
              </div>
              <div className="xui-margin-top-medium">
                <XUITextInput
                  label="Account number"
                  value={editedAccountNumber}
                  onChange={(e) => setEditedAccountNumber(e.target.value)}
                  placeholder="Enter account number"
                />
              </div>
              <div className="edit-actions xui-margin-top-large">
                <XUIButton variant="standard" onClick={handleCancelEdit}>
                  Cancel
                </XUIButton>
                <XUIButton variant="main" onClick={handleSaveEdit}>
                  Save
                </XUIButton>
              </div>
            </div>
          </div>
        )}
      </div>
      
      {selectedAccount && !isEditingAccount && (
        <>
          <div className="provider-disclaimer xui-margin-top-xlarge">
            <div className="disclaimer-icon">ℹ️</div>
            <div className="disclaimer-content">
              <p className="xui-font-size-small xui-text-color-muted">
                <strong>About this service:</strong> Xero is not a payments service provider. 
                Bank payments are powered by Akahu, a secure open banking platform. 
                You'll be redirected to Akahu when your customers pay.
              </p>
            </div>
          </div>
          
          <div className="screen-actions-centered xui-margin-top-large">
            <XUIButton 
              variant="main" 
              onClick={handleSubmit}
            >
              Enable bank payments
            </XUIButton>
          </div>
        </>
      )}
    </div>
  )
  
  // Screen 2: Success
  const renderSuccessScreen = () => (
    <div className="aggressive-screen success-screen">
      <div className="success-icon">✓</div>
      <h1 className="xui-heading-xlarge xui-margin-top-medium">You're all set!</h1>
      <p className="xui-font-size-medium xui-margin-top-small xui-text-color-muted">
        Bank payments are now enabled. Your customers can pay invoices directly from their bank account.
      </p>
      
      <div className="what-next-section xui-margin-top-xlarge">
        <h2 className="xui-heading-medium xui-margin-bottom-medium">What happens next?</h2>
        <ul className="xui-list">
          <li className="xui-font-size-medium xui-margin-bottom-small">
            The "Pay by bank" option will appear on your invoices
          </li>
          <li className="xui-font-size-medium xui-margin-bottom-small">
            Customers will be redirected to Akahu to complete payment
          </li>
          <li className="xui-font-size-medium xui-margin-bottom-small">
            Payments settle directly to your selected bank account
          </li>
          <li className="xui-font-size-medium xui-margin-bottom-small">
            Invoices are automatically marked as paid
          </li>
        </ul>
      </div>
      
      <div className="screen-actions-centered xui-margin-top-xlarge">
        <XUIButton variant="main" onClick={handleComplete}>
          Done
        </XUIButton>
      </div>
    </div>
  )
  
  // Main render
  const renderCurrentScreen = () => {
    switch (currentScreen) {
      case OnboardingScreen.SelectAccount:
        return renderSelectAccountScreen()
      case OnboardingScreen.Success:
        return renderSuccessScreen()
      default:
        return null
    }
  }
  
  return (
    <div className="onboarding-wizard-aggressive">
      <div className="aggressive-main-content">
        {renderCurrentScreen()}
      </div>
    </div>
  )
}

export default OnboardingWizardAggressive
