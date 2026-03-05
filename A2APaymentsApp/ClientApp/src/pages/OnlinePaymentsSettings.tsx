import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import XUIButton from '@xero/xui/react/button'
import { SetupBanner } from '../components/SetupBanner'
import { serializeEntryContext, type EntryContext } from '../types/EntryContext'
import './OnlinePaymentsSettings.css'

type ProviderStatus = 'NOT_CONFIGURED' | 'SETUP_STARTED' | 'SETUP_COMPLETE' | 'ERROR'

interface PaymentServiceConfig {
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

function OnlinePaymentsSettings() {
  const navigate = useNavigate()
  
  // Mock state - in real app this would come from API/CP Provisioning
  const [services, setServices] = useState<PaymentServiceConfig[]>([
    {
      id: 'pay-by-bank',
      name: 'Pay by bank',
      provider: 'Powered by Akahu',
      description: 'Accept instant bank-to-bank transfers',
      longDescription: 'Let your customers pay you directly from their bank account with instant settlement. No card fees, faster cash flow.',
      status: 'NOT_CONFIGURED', // Change to SETUP_STARTED or SETUP_COMPLETE to demo those states
      methods: ['Bank transfer', 'Open Banking'],
      features: [
        'Instant bank-to-bank transfers',
        'Lower fees than card payments (0.95% vs 2.9%)',
        'No setup fees or monthly costs',
        'Automatic reconciliation in Xero',
        'Support for all major NZ banks'
      ],
      pricing: '0.95% per transaction',
      setupTime: '5 minutes',
      region: ['NZ'],
      icon: 'bank',
      // When SETUP_COMPLETE:
      // settlementAccount: {
      //   name: 'Business Cheque Account',
      //   maskedNumber: '03-XXXX-XXXXXX78-00'
      // },
      // feeAccount: 'Payment Processing Fees'
    },
    {
      id: 'stripe',
      name: 'Cards and digital wallets',
      provider: 'Powered by Stripe',
      description: 'Accept credit cards, debit cards, and wallets',
      longDescription: 'Accept Visa, Mastercard, American Express, Apple Pay, and Google Pay. Get paid in 2-3 business days.',
      status: 'NOT_CONFIGURED',
      methods: ['Credit card', 'Debit card', 'Apple Pay', 'Google Pay'],
      features: [
        'Accept Visa, Mastercard, Amex',
        'Digital wallets (Apple Pay, Google Pay)',
        '2-3 business day settlements',
        'Automatic reconciliation',
        'Chargeback protection available'
      ],
      pricing: '2.9% + 30¢ per transaction',
      setupTime: '10 minutes',
      region: ['NZ', 'AU', 'UK', 'US', 'CA'],
      icon: 'card'
    },
    {
      id: 'gocardless',
      name: 'Direct Debit',
      provider: 'Powered by GoCardless',
      description: 'Collect recurring payments from bank accounts',
      longDescription: 'Set up recurring payments directly from your customers\' bank accounts. Perfect for subscriptions and regular billing.',
      status: 'NOT_CONFIGURED',
      methods: ['Direct Debit', 'Recurring payments'],
      features: [
        'Automatic recurring collections',
        'Lower failure rates than cards',
        'Free setup, pay as you go',
        'Mandate management',
        'Failed payment retries'
      ],
      pricing: '1% per transaction (min 20¢, max $4)',
      setupTime: '15 minutes',
      region: ['NZ', 'AU', 'UK', 'EU'],
      icon: 'debit'
    }
  ])
  
  const [showSuccessBanner, setShowSuccessBanner] = useState(false)
  
  // Determine if any services are configured
  const hasConfiguredServices = services.some(s => s.status === 'SETUP_COMPLETE')
  const hasStartedServices = services.some(s => s.status === 'SETUP_STARTED')
  
  const handleStartSetup = (serviceId: string) => {
    const service = services.find(s => s.id === serviceId)
    if (!service) return
    
    const context: EntryContext = {
      source: 'settings',
      mode: 'first_time',
      returnTo: '/settings/online-payments',
      metadata: { serviceId }
    }
    const params = serializeEntryContext(context)
    
    // Route to appropriate flow based on service
    if (serviceId === 'pay-by-bank') {
      navigate(`/merchant-onboarding?${params.toString()}`)
    } else {
      // For other providers, would integrate with their onboarding
      alert(`Setup for ${service.name} would launch ${service.provider} onboarding flow`)
    }
  }
  
  const handleResumeSetup = (serviceId: string) => {
    const service = services.find(s => s.id === serviceId)
    if (!service) return
    
    const context: EntryContext = {
      source: 'settings',
      mode: 'resume',
      returnTo: '/settings/online-payments',
      metadata: { serviceId }
    }
    const params = serializeEntryContext(context)
    navigate(`/merchant-onboarding?${params.toString()}`)
  }
  
  const handleManage = (serviceId: string) => {
    const context: EntryContext = {
      source: 'manage',
      mode: 'manage',
      returnTo: '/settings/online-payments',
      metadata: { serviceId }
    }
    const params = serializeEntryContext(context)
    navigate(`/merchant-onboarding?${params.toString()}`)
  }
  
  const handleDisconnect = (serviceId: string) => {
    const service = services.find(s => s.id === serviceId)
    if (!service) return
    
    if (confirm(`Disconnect ${service.name}? Customers will no longer see this payment option on invoices.`)) {
      setServices(services.map(s => 
        s.id === serviceId ? { ...s, status: 'NOT_CONFIGURED' as ProviderStatus, settlementAccount: undefined } : s
      ))
    }
  }
  
  const getStatusBadge = (status: ProviderStatus) => {
    switch (status) {
      case 'SETUP_COMPLETE':
        return <span className="x-status-badge x-status-badge--success">Connected</span>
      case 'SETUP_STARTED':
        return <span className="x-status-badge x-status-badge--warning">Setup started</span>
      case 'ERROR':
        return <span className="x-status-badge x-status-badge--error">Action required</span>
      default:
        return null
    }
  }
  
  const getActionButtons = (service: PaymentServiceConfig) => {
    switch (service.status) {
      case 'NOT_CONFIGURED':
        return (
          <XUIButton
            variant="main"
            onClick={() => handleStartSetup(service.id)}
          >
            Get set up
          </XUIButton>
        )
      
      case 'SETUP_STARTED':
        return (
          <>
            <XUIButton
              variant="main"
              onClick={() => handleResumeSetup(service.id)}
            >
              Resume setup
            </XUIButton>
            <XUIButton
              variant="borderless-standard"
              onClick={() => handleDisconnect(service.id)}
            >
              Cancel
            </XUIButton>
          </>
        )
      
      case 'SETUP_COMPLETE':
        return (
          <>
            <XUIButton
              variant="standard"
              onClick={() => handleManage(service.id)}
            >
              Manage
            </XUIButton>
            <XUIButton
              variant="borderless-standard"
              onClick={() => handleDisconnect(service.id)}
            >
              Disconnect
            </XUIButton>
          </>
        )
      
      case 'ERROR':
        return (
          <>
            <XUIButton
              variant="main"
              onClick={() => handleResumeSetup(service.id)}
            >
              Fix issues
            </XUIButton>
            <XUIButton
              variant="borderless-standard"
              onClick={() => handleDisconnect(service.id)}
            >
              Disconnect
            </XUIButton>
          </>
        )
    }
  }
  
  const renderIcon = (icon: 'bank' | 'card' | 'debit') => {
    switch (icon) {
      case 'bank':
        return (
          <svg width="40" height="40" viewBox="0 0 24 24" fill="none">
            <path d="M3 21h18M3 10h18M5 6l7-4 7 4M4 10v11M8 10v11M12 10v11M16 10v11M20 10v11" 
              stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
          </svg>
        )
      case 'card':
        return (
          <svg width="40" height="40" viewBox="0 0 24 24" fill="none">
            <rect x="2" y="6" width="20" height="12" rx="2" stroke="currentColor" strokeWidth="2"/>
            <path d="M2 10h20M6 14h4M14 14h2" stroke="currentColor" strokeWidth="2" strokeLinecap="round"/>
          </svg>
        )
      case 'debit':
        return (
          <svg width="40" height="40" viewBox="0 0 24 24" fill="none">
            <path d="M21 12a9 9 0 11-18 0 9 9 0 0118 0zM9 12l2 2 4-4" 
              stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
          </svg>
        )
    }
  }
  
  return (
    <div className="x-page x-payment-services-page">
      {/* Xero-style breadcrumbs */}
      <div className="x-breadcrumbs">
        <a href="#" onClick={(e) => { e.preventDefault(); navigate('/') }}>Home</a>
        <span className="x-breadcrumbs__separator">›</span>
        <a href="#" onClick={(e) => { e.preventDefault(); navigate('/settings') }}>Settings</a>
        <span className="x-breadcrumbs__separator">›</span>
        <span className="x-breadcrumbs__current">Online payments</span>
      </div>
      
      {/* Success banner (shown after completing setup) */}
      {showSuccessBanner && (
        <SetupBanner
          variant="info"
          title="Payment service connected successfully"
          description="Your customers can now pay you using this method on invoices."
          primaryAction={{
            label: 'View settings',
            onClick: () => setShowSuccessBanner(false)
          }}
          onDismiss={() => setShowSuccessBanner(false)}
        />
      )}
      
      {/* "Almost there" banner for incomplete setups */}
      {hasStartedServices && (
        <SetupBanner
          variant="high-contrast"
          title="Almost there!"
          description="You started setting up a payment service. Complete setup to start accepting payments."
          primaryAction={{
            label: 'Resume setup',
            onClick: () => {
              const startedService = services.find(s => s.status === 'SETUP_STARTED')
              if (startedService) handleResumeSetup(startedService.id)
            }
          }}
        />
      )}
      
      {/* Page header */}
      <div className="x-page-header">
        <div className="x-page-header__title">
          <h1 className="x-heading-xl">Online payments</h1>
          <p className="x-text-md x-text-muted">
            Choose which payment methods you want to offer your customers. {' '}
            <a href="#" className="x-text-link">Learn more about online payments</a>
          </p>
        </div>
      </div>
      
      {/* Payment services grid */}
      <div className="x-payment-services-section">
        <h2 className="x-heading-md" style={{ marginBottom: '8px' }}>Available payment services</h2>
        <p className="x-text-md x-text-muted" style={{ marginBottom: '24px' }}>
          Connect payment services to give customers more ways to pay and get paid faster
        </p>
        
        <div className="x-services-grid">
          {services.map((service) => (
            <div 
              key={service.id} 
              className={`x-service-card ${service.status === 'SETUP_COMPLETE' ? 'x-service-card--connected' : ''}`}
            >
              {/* Card header */}
              <div className="x-service-card__header">
                <div className="x-service-card__icon">
                  {renderIcon(service.icon)}
                </div>
                
                <div className="x-service-card__title">
                  <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                    <h3 className="x-heading-md">{service.name}</h3>
                    {getStatusBadge(service.status)}
                  </div>
                  <p className="x-text-sm x-text-muted">{service.provider}</p>
                </div>
              </div>
              
              {/* Card description */}
              <p className="x-service-card__description">
                {service.longDescription}
              </p>
              
              {/* Payment method badges */}
              <div className="x-service-card__methods">
                {service.methods.map((method, idx) => (
                  <span key={idx} className="x-method-badge">{method}</span>
                ))}
              </div>
              
              {/* Features list (expandable for NOT_CONFIGURED state) */}
              {service.status === 'NOT_CONFIGURED' && (
                <details className="x-service-card__details">
                  <summary className="x-details-summary">
                    <span>Key features</span>
                    <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
                      <path d="M4 6l4 4 4-4"/>
                    </svg>
                  </summary>
                  <ul className="x-features-list">
                    {service.features.map((feature, idx) => (
                      <li key={idx} className="x-feature-item">
                        <svg className="x-checkmark" width="16" height="16" viewBox="0 0 16 16" fill="none">
                          <path d="M5 8l2 2 4-4" stroke="currentColor" strokeWidth="2" strokeLinecap="round"/>
                        </svg>
                        <span>{feature}</span>
                      </li>
                    ))}
                  </ul>
                  <div className="x-service-meta">
                    <div className="x-meta-item">
                      <span className="x-meta-label">Pricing:</span>
                      <span className="x-meta-value">{service.pricing}</span>
                    </div>
                    <div className="x-meta-item">
                      <span className="x-meta-label">Setup time:</span>
                      <span className="x-meta-value">{service.setupTime}</span>
                    </div>
                  </div>
                </details>
              )}
              
              {/* Settlement account (shown when SETUP_COMPLETE) */}
              {service.status === 'SETUP_COMPLETE' && service.settlementAccount && (
                <div className="x-service-card__account">
                  <div className="x-account-info">
                    <span className="x-text-sm x-text-muted">Settlement account</span>
                    <div style={{ marginTop: '4px' }}>
                      <strong className="x-text-md">{service.settlementAccount.name}</strong>
                      <span className="x-text-sm x-text-muted" style={{ marginLeft: '8px' }}>
                        {service.settlementAccount.maskedNumber}
                      </span>
                    </div>
                  </div>
                  {service.feeAccount && (
                    <div className="x-account-info" style={{ marginTop: '12px' }}>
                      <span className="x-text-sm x-text-muted">Fee account</span>
                      <div style={{ marginTop: '4px' }}>
                        <strong className="x-text-md">{service.feeAccount}</strong>
                      </div>
                    </div>
                  )}
                </div>
              )}
              
              {/* Actions */}
              <div className="x-service-card__actions">
                {getActionButtons(service)}
              </div>
            </div>
          ))}
        </div>
      </div>
      
      {/* Help section */}
      <div className="x-help-section">
        <h3 className="x-heading-sm">Need help?</h3>
        <p className="x-text-md x-text-muted">
          Learn more about <a href="#" className="x-text-link">setting up online payments</a>, {' '}
          <a href="#" className="x-text-link">pricing and fees</a>, or {' '}
          <a href="#" className="x-text-link">which payment methods are right for you</a>.
        </p>
      </div>
    </div>
  )
}

export default OnlinePaymentsSettings
