import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import XUIButton from '@xero/xui/react/button'
import { serializeEntryContext, type EntryContext } from '../types/EntryContext'
import './OnlinePaymentsSettings.css'

interface PaymentService {
  id: string
  name: string
  provider: string
  description: string
  enabled: boolean
  settlementAccount?: {
    name: string
    maskedNumber: string
  }
}

function OnlinePaymentsSettings() {
  const navigate = useNavigate()
  
  // Mock state - in real app this would come from API
  const [payByBankEnabled, setPayByBankEnabled] = useState(false)
  
  const paymentServices: PaymentService[] = [
    {
      id: 'pay-by-bank',
      name: 'Pay by bank',
      provider: 'Powered by Akahu',
      description: 'Accept direct bank transfers from your customers. Fast, secure, and automatic reconciliation.',
      enabled: payByBankEnabled,
      settlementAccount: payByBankEnabled ? {
        name: 'Business Cheque Account',
        maskedNumber: '03-XXXX-XXXXXX78-00'
      } : undefined
    },
    {
      id: 'stripe',
      name: 'Cards and digital wallets',
      provider: 'Powered by Stripe',
      description: 'Accept credit and debit card payments, plus Apple Pay and Google Pay.',
      enabled: false
    },
    {
      id: 'gocardless',
      name: 'Direct Debit',
      provider: 'Powered by GoCardless',
      description: 'Collect recurring payments directly from your customers\' bank accounts.',
      enabled: false
    }
  ]
  
  const handleEnablePayByBank = () => {
    const context: EntryContext = {
      source: 'settings',
      mode: 'first_time',
      returnTo: '/settings/online-payments'
    }
    const params = serializeEntryContext(context)
    navigate(`/merchant-onboarding?${params.toString()}`)
  }
  
  const handleManagePayByBank = () => {
    const context: EntryContext = {
      source: 'manage',
      mode: 'manage',
      returnTo: '/settings/online-payments'
    }
    const params = serializeEntryContext(context)
    navigate(`/merchant-onboarding?${params.toString()}`)
  }
  
  const handleDisable = () => {
    if (confirm('Are you sure you want to disable Pay by bank? Your customers will no longer see this payment option on invoices.')) {
      setPayByBankEnabled(false)
    }
  }
  
  return (
    <div className="x-page">
      {/* Page header */}
      <div className="x-page-header">
        <div className="x-page-header__title">
          <h1 className="x-heading-xl">Online payments</h1>
          <p className="x-text-muted">Accept online payments from your customers and get paid faster</p>
        </div>
      </div>
      
      {/* Payment services section */}
      <div className="x-section">
        <h2 className="x-heading-md">Payment services</h2>
        <p className="x-text-md x-text-muted" style={{ marginTop: '8px', marginBottom: '24px' }}>
          Choose which payment methods you want to offer your customers
        </p>
        
        <div className="x-payment-services-grid">
          {paymentServices.map((service) => (
            <div
              key={service.id}
              className={`x-payment-card ${!service.enabled ? 'x-payment-card--available' : ''}`}
            >
              {/* Card content */}
              <div className="x-payment-card__header">
                <div className="x-payment-card__title">
                  <h3 className="x-heading-md">{service.name}</h3>
                  {service.enabled && (
                    <span className="x-status-pill x-status-pill--success">Connected</span>
                  )}
                </div>
                <p className="x-text-sm x-text-muted">{service.provider}</p>
              </div>
              
              <p className="x-payment-card__description x-text-md">
                {service.description}
              </p>
              
              {/* Settlement account info (when enabled) */}
              {service.enabled && service.settlementAccount && (
                <div className="x-payment-card__account">
                  <div className="x-text-sm">
                    <span className="x-text-muted">Settlement account</span>
                    <div style={{ marginTop: '4px' }}>
                      <strong>{service.settlementAccount.name}</strong>
                      <span className="x-text-muted" style={{ marginLeft: '8px' }}>
                        {service.settlementAccount.maskedNumber}
                      </span>
                    </div>
                  </div>
                </div>
              )}
              
              {/* Actions */}
              <div className="x-payment-card__actions">
                {service.id === 'pay-by-bank' && (
                  !service.enabled ? (
                    <XUIButton
                      variant="main"
                      onClick={handleEnablePayByBank}
                    >
                      Connect
                    </XUIButton>
                  ) : (
                    <>
                      <XUIButton
                        variant="standard"
                        onClick={handleManagePayByBank}
                      >
                        Manage
                      </XUIButton>
                      <XUIButton
                        variant="borderless-standard"
                        onClick={handleDisable}
                      >
                        Disconnect
                      </XUIButton>
                    </>
                  )
                )}
                
                {service.id !== 'pay-by-bank' && (
                  <XUIButton
                    variant="standard"
                    disabled
                  >
                    Coming soon
                  </XUIButton>
                )}
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}

export default OnlinePaymentsSettings
