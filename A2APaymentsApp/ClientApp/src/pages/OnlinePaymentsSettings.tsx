import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { SetupBanner } from '../components/SetupBanner'
import { PaymentMethodsTab } from '../components/tabs/PaymentMethodsTab'
import { DefaultSettingsTab } from '../components/tabs/DefaultSettingsTab'
import { ConnectedServicesTab } from '../components/tabs/ConnectedServicesTab'
import { AddServiceTab } from '../components/tabs/AddServiceTab'
import { serializeEntryContext, type EntryContext } from '../types/EntryContext'
import type { PaymentServiceConfig, ProviderStatus } from '../types/PaymentServiceTypes'
import './OnlinePaymentsSettings.css'

type TabId = 'methods' | 'settings' | 'connected' | 'add'

function OnlinePaymentsSettings() {
  const navigate = useNavigate()
  const [activeTab, setActiveTab] = useState<TabId>('methods')
  
  // Mock state - in real app this would come from API/CP Provisioning
  // Customize the initial state to demo different flows:
  // - All NOT_CONFIGURED: pristine org without any setup
  // - Some SETUP_STARTED: user has incomplete onboarding(s)
  // - Some SETUP_COMPLETE: user has connected services
  const [services, setServices] = useState<PaymentServiceConfig[]>([
    {
      id: 'pay-by-bank',
      name: 'Pay by bank',
      provider: 'Powered by Akahu',
      description: 'Accept instant bank-to-bank transfers',
      longDescription: 'Let your customers pay you directly from their bank account with instant settlement. No card fees, faster cash flow.',
      status: 'SETUP_COMPLETE', // Change to test different states
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
      settlementAccount: {
        name: 'Business Cheque Account',
        maskedNumber: '03-XXXX-XXXXXX78-00'
      },
      feeAccount: 'Payment Processing Fees'
    },
    {
      id: 'stripe',
      name: 'Cards and digital wallets',
      provider: 'Powered by Stripe',
      description: 'Accept credit cards, debit cards, and wallets',
      longDescription: 'Accept Visa, Mastercard, American Express, Apple Pay, and Google Pay. Get paid in 2-3 business days.',
      status: 'SETUP_COMPLETE',
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
      icon: 'card',
      settlementAccount: {
        name: 'Business Cheque Account',
        maskedNumber: '03-XXXX-XXXXXX78-00'
      },
      feeAccount: 'Payment Processing Fees'
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
  
  // Determine if any services are configured or started
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
  
  const handleEditService = (serviceId: string) => {
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
        <h1 className="x-heading-xl">Online payments</h1>
      </div>
      
      {/* Tab Navigation */}
      <div className="x-tabs">
        <div className="x-tabs-nav" role="tablist">
          <button
            role="tab"
            aria-selected={activeTab === 'methods'}
            className={`x-tab ${activeTab === 'methods' ? 'x-tab--active' : ''}`}
            onClick={() => setActiveTab('methods')}
          >
            Payment methods
          </button>
          <button
            role="tab"
            aria-selected={activeTab === 'settings'}
            className={`x-tab ${activeTab === 'settings' ? 'x-tab--active' : ''}`}
            onClick={() => setActiveTab('settings')}
          >
            Default settings
          </button>
          <button
            role="tab"
            aria-selected={activeTab === 'connected'}
            className={`x-tab ${activeTab === 'connected' ? 'x-tab--active' : ''}`}
            onClick={() => setActiveTab('connected')}
          >
            Connected services
          </button>
          <button
            role="tab"
            aria-selected={activeTab === 'add'}
            className={`x-tab ${activeTab === 'add' ? 'x-tab--active' : ''}`}
            onClick={() => setActiveTab('add')}
          >
            Add a new service
          </button>
        </div>
        
        {/* Tab Content */}
        <div className="x-tabs-content">
          {activeTab === 'methods' && (
            <PaymentMethodsTab services={services} />
          )}
          {activeTab === 'settings' && (
            <DefaultSettingsTab />
          )}
          {activeTab === 'connected' && (
            <ConnectedServicesTab 
              services={services}
              onEdit={handleEditService}
            />
          )}
          {activeTab === 'add' && (
            <AddServiceTab 
              services={services}
              onStartSetup={handleStartSetup}
              onResumeSetup={handleResumeSetup}
            />
          )}
        </div>
      </div>
    </div>
  )
}

export default OnlinePaymentsSettings
