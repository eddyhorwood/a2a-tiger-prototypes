import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import XUIButton from '@xero/xui/react/button'
import XUIModal, { XUIModalBody } from '@xero/xui/react/modal'
import XUITextInput from '@xero/xui/react/textinput'
import { type EntryContext } from '../types/EntryContext'
import { getEligibleSettlementAccounts, formatAccountDisplay } from '../mocks/xeroOrgData'
import './OnboardingWizardAggressiveFast.css'

interface OnboardingWizardAggressiveFastProps {
  entryContext: EntryContext
  onClose: () => void
}

/**
 * Onboarding Wizard - Aggressive Flow (< 10 sec)
 * 
 * Ultra-fast modal-based onboarding with bank account dropdown + edit + AML disclaimer.
 * This flow is optimized for merchants who want to enable payments as quickly as possible.
 */
function OnboardingWizardAggressiveFast({ entryContext, onClose }: OnboardingWizardAggressiveFastProps) {
  const navigate = useNavigate()
  
  // State
  const [isEditing, setIsEditing] = useState(false)
  const [isProcessing, setIsProcessing] = useState(false)
  const [showAMLDisclaimer, setShowAMLDisclaimer] = useState(false)
  
  // Get pre-filled account (first eligible account)
  const eligibleAccounts = getEligibleSettlementAccounts()
  const [selectedAccountId, setSelectedAccountId] = useState(eligibleAccounts[0]?.id || '')
  const selectedAccount = eligibleAccounts.find(acc => acc.id === selectedAccountId)
  
  // Editable fields (pre-filled with selected account)
  const [accountNumber, setAccountNumber] = useState(selectedAccount?.bankAccountNumberMasked || '')
  const [bsb, setBsb] = useState(selectedAccount?.code || '')
  
  const handleClose = () => {
    const returnPath = entryContext.returnTo || '/settings/online-payments'
    navigate(returnPath)
  }
  
  const handleEnable = async () => {
    setIsProcessing(true)
    
    // Simulate quick OAuth + setup
    await new Promise(resolve => setTimeout(resolve, 1500))
    
    // Navigate to success or return path
    const returnPath = entryContext.returnTo || '/settings/online-payments'
    navigate(`${returnPath}?setup=complete`)
  }
  
  const handleAccountChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const accountId = e.target.value
    setSelectedAccountId(accountId)
    const account = eligibleAccounts.find(acc => acc.id === accountId)
    if (account) {
      setAccountNumber(account.bankAccountNumberMasked || '')
      setBsb(account.code || '')
    }
  }
  
  if (eligibleAccounts.length === 0) {
    return (
      <XUIModal
        id="aggressive-onboarding-modal"
        isOpen={true}
        size="small"
        closeButtonLabel="Close modal"
        onClose={handleClose}
      >
        <XUIModalBody>
          <div style={{ textAlign: 'center', padding: '24px' }}>
            <h2 className="x-heading-lg" style={{ marginBottom: '12px' }}>No eligible bank accounts</h2>
            <p className="x-text-md" style={{ color: '#6B7280', marginBottom: '24px' }}>
              You need to add a New Zealand bank account to Xero before setting up Pay by bank.
            </p>
            <XUIButton variant="standard" onClick={handleClose}>
              Go back
            </XUIButton>
          </div>
        </XUIModalBody>
      </XUIModal>
    )
  }
  
  return (
    <>
      {/* Main Onboarding Modal */}
      <XUIModal
        id="aggressive-onboarding-modal"
        isOpen={true}
        size="medium"
        closeButtonLabel="Close modal"
        onClose={handleClose}
      >
        <XUIModalBody>
          {/* Header */}
          <div style={{ textAlign: 'center', marginBottom: '32px' }}>
            <h2 className="x-heading-xl" style={{ marginBottom: '12px', fontSize: '28px', fontWeight: 600 }}>
              Get set up with Akahu
            </h2>
            <p className="x-text-md" style={{ color: '#6B7280' }}>
              Get paid faster with instant bank-to-bank transfers
            </p>
          </div>

          {/* Settlement Account Section */}
          <div style={{ marginBottom: '24px' }}>
            <label htmlFor="settlement-account" className="x-field-label" style={{ display: 'block', marginBottom: '8px', fontWeight: 500 }}>
              Settlement account
            </label>
            <p className="x-text-sm" style={{ color: '#6B7280', marginBottom: '12px' }}>
              Customer payments will be transferred directly to this account
            </p>
            
            {!isEditing ? (
              <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                <select
                  id="settlement-account"
                  value={selectedAccountId}
                  onChange={handleAccountChange}
                  style={{
                    flex: 1,
                    padding: '10px 12px',
                    border: '1px solid #D1D5DB',
                    borderRadius: '4px',
                    fontSize: '14px',
                    backgroundColor: 'white'
                  }}
                >
                  {eligibleAccounts.map(account => (
                    <option key={account.id} value={account.id}>
                      {formatAccountDisplay(account)}
                    </option>
                  ))}
                </select>
                <XUIButton 
                  variant="borderless-standard" 
                  onClick={() => setIsEditing(true)}
                >
                  Edit
                </XUIButton>
              </div>
            ) : (
              <div style={{ border: '1px solid #D1D5DB', borderRadius: '4px', padding: '16px', backgroundColor: '#F9FAFB' }}>
                <div style={{ marginBottom: '12px' }}>
                  <XUITextInput
                    id="account-number"
                    label="Account number"
                    value={accountNumber}
                    onChange={(e) => setAccountNumber(e.target.value)}
                  />
                </div>
                <div style={{ marginBottom: '16px' }}>
                  <XUITextInput
                    id="bsb"
                    label="BSB / Sort code"
                    value={bsb}
                    onChange={(e) => setBsb(e.target.value)}
                  />
                </div>
                <div style={{ display: 'flex', gap: '8px', justifyContent: 'flex-end' }}>
                  <XUIButton 
                    variant="borderless-standard" 
                    onClick={() => {
                      // Reset to selected account values
                      setAccountNumber(selectedAccount?.bankAccountNumberMasked || '')
                      setBsb(selectedAccount?.code || '')
                      setIsEditing(false)
                    }}
                  >
                    Cancel
                  </XUIButton>
                  <XUIButton 
                    variant="standard" 
                    onClick={() => {
                      // Save edited values
                      setIsEditing(false)
                    }}
                  >
                    Save changes
                  </XUIButton>
                </div>
              </div>
            )}
          </div>

          {/* Feature list */}
          <ul className="x-feature-list" style={{ marginBottom: '24px' }}>
            <li className="x-feature-item">
              <svg className="x-checkmark" width="20" height="20" viewBox="0 0 20 20" fill="none">
                <path d="M7 10l2 2 4-4" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                <circle cx="10" cy="10" r="9" stroke="currentColor" strokeWidth="1.5"/>
              </svg>
              <span>Instant bank-to-bank transfers from major NZ banks</span>
            </li>
            <li className="x-feature-item">
              <svg className="x-checkmark" width="20" height="20" viewBox="0 0 20 20" fill="none">
                <path d="M7 10l2 2 4-4" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                <circle cx="10" cy="10" r="9" stroke="currentColor" strokeWidth="1.5"/>
              </svg>
              <span>Lower fees than traditional card payments</span>
            </li>
            <li className="x-feature-item">
              <svg className="x-checkmark" width="20" height="20" viewBox="0 0 20 20" fill="none">
                <path d="M7 10l2 2 4-4" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                <circle cx="10" cy="10" r="9" stroke="currentColor" strokeWidth="1.5"/>
              </svg>
              <span>No setup fees or monthly costs</span>
            </li>
            <li className="x-feature-item">
              <svg className="x-checkmark" width="20" height="20" viewBox="0 0 20 20" fill="none">
                <path d="M7 10l2 2 4-4" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                <circle cx="10" cy="10" r="9" stroke="currentColor" strokeWidth="1.5"/>
              </svg>
              <span>Automatic reconciliation in Xero</span>
            </li>
          </ul>

          {/* AML Disclaimer */}
          <div style={{ 
            backgroundColor: '#F3F4F6', 
            border: '1px solid #D1D5DB', 
            borderRadius: '4px', 
            padding: '12px 16px',
            marginBottom: '24px'
          }}>
            <p className="x-text-sm" style={{ color: '#374151', margin: 0 }}>
              Neither Xero nor Akahu holds your customer funds — payments go directly from your customer's bank to your account.{' '}
              <button 
                onClick={() => setShowAMLDisclaimer(true)}
                style={{
                  color: '#3B82F6',
                  textDecoration: 'underline',
                  background: 'none',
                  border: 'none',
                  padding: 0,
                  cursor: 'pointer',
                  font: 'inherit'
                }}
              >
                Learn more
              </button>
            </p>
          </div>

          {/* CTA button */}
          <div style={{ textAlign: 'center' }}>
            <XUIButton
              variant="main"
              onClick={handleEnable}
              isDisabled={isProcessing}
              style={{ width: '100%', maxWidth: '400px', height: '48px', fontSize: '16px', fontWeight: 600 }}
            >
              {isProcessing ? 'Enabling...' : 'Enable Pay by bank'}
            </XUIButton>
          </div>
        </XUIModalBody>
      </XUIModal>

      {/* AML Disclaimer Modal */}
      <XUIModal
        id="aml-disclaimer-modal"
        isOpen={showAMLDisclaimer}
        size="small"
        closeButtonLabel="Close"
        onClose={() => setShowAMLDisclaimer(false)}
      >
        <XUIModalBody>
          <h3 className="x-heading-lg" style={{ marginBottom: '16px' }}>How Pay by bank works</h3>
          <div className="x-text-md" style={{ color: '#374151', lineHeight: '1.6' }}>
            <p style={{ marginBottom: '12px' }}>
              Akahu uses Open Banking APIs to initiate payments on your behalf. When your customer pays an invoice:
            </p>
            <ol style={{ paddingLeft: '20px', marginBottom: '12px' }}>
              <li style={{ marginBottom: '8px' }}>They click "Pay now" on their invoice</li>
              <li style={{ marginBottom: '8px' }}>They authorize the payment with their bank</li>
              <li style={{ marginBottom: '8px' }}>Funds are transferred <strong>directly from their bank to your settlement account</strong></li>
              <li style={{ marginBottom: '8px' }}>The payment syncs automatically in Xero</li>
            </ol>
            <p style={{ marginBottom: '12px' }}>
              <strong>Important:</strong> Neither Xero nor Akahu ever takes custody of customer funds. We simply facilitate the authorization and initiation of the bank transfer.
            </p>
            <p style={{ marginBottom: 0 }}>
              This means you benefit from lower regulatory requirements, faster settlement, and reduced risk.
            </p>
          </div>
        </XUIModalBody>
      </XUIModal>
    </>
  )
}

export default OnboardingWizardAggressiveFast
