import type { PaymentServiceConfig } from '../../types/PaymentServiceTypes.ts'
import './PaymentMethodsTab.css'

interface PaymentMethodsTabProps {
  services: PaymentServiceConfig[]
}

export function PaymentMethodsTab({ services }: PaymentMethodsTabProps) {
  // Only show methods for services that are SETUP_COMPLETE
  const connectedServices = services.filter(s => s.status === 'SETUP_COMPLETE')
  
  // Check which providers are connected
  const hasStripe = connectedServices.some(s => s.id === 'stripe')
  const hasAkahu = connectedServices.some(s => s.id === 'pay-by-bank')
  const hasGoCardless = connectedServices.some(s => s.id === 'gocardless')
  
  return (
    <div className="x-payment-methods-tab">
      {connectedServices.length === 0 ? (
        <div className="x-empty-state">
          <svg width="48" height="48" viewBox="0 0 24 24" fill="none" className="x-empty-icon">
            <rect x="2" y="6" width="20" height="12" rx="2" stroke="currentColor" strokeWidth="2"/>
            <path d="M2 10h20" stroke="currentColor" strokeWidth="2"/>
          </svg>
          <h3 className="x-heading-md">No payment methods configured yet</h3>
          <p className="x-text-md x-text-muted">
            Connect a payment service to start offering payment methods to your customers
          </p>
        </div>
      ) : (
        <>
          {/* Cards Category */}
          {hasStripe && (
            <div className="x-method-category">
              <div className="x-category-header">
                <div className="x-category-title">
                  <svg width="20" height="20" viewBox="0 0 24 24" fill="none" className="x-category-icon">
                    <rect x="2" y="6" width="20" height="12" rx="2" stroke="currentColor" strokeWidth="2"/>
                    <path d="M2 10h20M6 14h4M14 14h2" stroke="currentColor" strokeWidth="2" strokeLinecap="round"/>
                  </svg>
                  <h2 className="x-heading-md">Cards</h2>
                </div>
                <p className="x-text-sm x-text-muted">
                  Popular for consumers and businesses to pay online.
                </p>
              </div>
              
              <div className="x-methods-list">
                {/* Primary card method */}
                <div className="x-method-item">
                  <button className="x-method-expand" aria-label="Expand details">
                    <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
                      <path d="M4 6l4 4 4-4"/>
                    </svg>
                  </button>
                  
                  <div className="x-method-icon x-method-icon--stripe">
                    <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
                      <rect x="2" y="6" width="20" height="12" rx="2" stroke="currentColor" strokeWidth="2"/>
                      <path d="M2 10h20" stroke="currentColor" strokeWidth="2"/>
                    </svg>
                  </div>
                  
                  <div className="x-method-content">
                    <div className="x-method-title">
                      <strong>Card</strong>
                      <span className="x-method-provider">by Stripe</span>
                    </div>
                    <p className="x-method-subtitle">Popular globally</p>
                  </div>
                  
                  <div className="x-method-status">
                    <span className="x-status-badge x-status-badge--on">
                      On
                      <svg width="14" height="14" viewBox="0 0 16 16" fill="currentColor">
                        <path d="M5 8l2 2 4-4" strokeWidth="2"/>
                      </svg>
                    </span>
                  </div>
                </div>
              </div>
            </div>
          )}
          
          {/* Wallets Category */}
          {hasStripe && (
            <div className="x-method-category">
              <div className="x-category-header">
                <div className="x-category-title">
                  <svg width="20" height="20" viewBox="0 0 24 24" fill="none" className="x-category-icon">
                    <rect x="5" y="4" width="14" height="16" rx="2" stroke="currentColor" strokeWidth="2"/>
                    <path d="M9 8h6M9 12h6M9 16h4" stroke="currentColor" strokeWidth="2" strokeLinecap="round"/>
                  </svg>
                  <h2 className="x-heading-md">Wallets</h2>
                </div>
                <p className="x-text-sm x-text-muted">
                  Improve conversion and reduce fraud on mobile. Customers pay with a stored Card or balance.
                </p>
              </div>
              
              <div className="x-methods-list">
                {/* Apple Pay */}
                <div className="x-method-item">
                  <button className="x-method-expand" aria-label="Expand details">
                    <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
                      <path d="M4 6l4 4 4-4"/>
                    </svg>
                  </button>
                  
                  <div className="x-method-icon x-method-icon--applepay">
                    <svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor">
                      <path d="M16.5 3.5c-1.2 0-2.4.7-3 1.5-.5-.8-1.5-1.5-3-1.5-1.7 0-3 1.3-3 3 0 3.2 2.5 5.5 6 8.5 3.5-3 6-5.3 6-8.5 0-1.7-1.3-3-3-3z"/>
                    </svg>
                  </div>
                  
                  <div className="x-method-content">
                    <div className="x-method-title">
                      <strong>Apple Pay</strong>
                      <span className="x-method-provider">by Stripe</span>
                    </div>
                    <p className="x-method-subtitle">Popular globally</p>
                  </div>
                  
                  <div className="x-method-status">
                    <div className="x-method-auto">
                      <span className="x-status-badge x-status-badge--on">
                        On
                        <svg width="14" height="14" viewBox="0 0 16 16" fill="currentColor">
                          <path d="M5 8l2 2 4-4" strokeWidth="2"/>
                        </svg>
                      </span>
                      <span className="x-text-xs x-text-muted">Automatically offered with Card</span>
                    </div>
                  </div>
                </div>
                
                {/* Google Pay */}
                <div className="x-method-item">
                  <button className="x-method-expand" aria-label="Expand details">
                    <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
                      <path d="M4 6l4 4 4-4"/>
                    </svg>
                  </button>
                  
                  <div className="x-method-icon x-method-icon--googlepay">
                    <svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor">
                      <path d="M12 2L2 7v10l10 5 10-5V7L12 2zm0 18l-8-4v-8l8 4v8z"/>
                    </svg>
                  </div>
                  
                  <div className="x-method-content">
                    <div className="x-method-title">
                      <strong>Google Pay</strong>
                      <span className="x-method-provider">by Stripe</span>
                    </div>
                    <p className="x-method-subtitle">Popular globally</p>
                  </div>
                  
                  <div className="x-method-status">
                    <div className="x-method-auto">
                      <span className="x-status-badge x-status-badge--on">
                        On
                        <svg width="14" height="14" viewBox="0 0 16 16" fill="currentColor">
                          <path d="M5 8l2 2 4-4" strokeWidth="2"/>
                        </svg>
                      </span>
                      <span className="x-text-xs x-text-muted">Automatically offered with Card</span>
                    </div>
                  </div>
                </div>
                
                {/* Link */}
                <div className="x-method-item">
                  <button className="x-method-expand" aria-label="Expand details">
                    <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
                      <path d="M4 6l4 4 4-4"/>
                    </svg>
                  </button>
                  
                  <div className="x-method-icon x-method-icon--link">
                    <svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor">
                      <path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71"/>
                      <path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71"/>
                    </svg>
                  </div>
                  
                  <div className="x-method-content">
                    <div className="x-method-title">
                      <strong>Link</strong>
                      <span className="x-method-provider">by Stripe</span>
                    </div>
                    <p className="x-method-subtitle">Popular globally</p>
                  </div>
                  
                  <div className="x-method-status">
                    <div className="x-method-auto">
                      <span className="x-status-badge x-status-badge--on">
                        On
                        <svg width="14" height="14" viewBox="0 0 16 16" fill="currentColor">
                          <path d="M5 8l2 2 4-4" strokeWidth="2"/>
                        </svg>
                      </span>
                      <span className="x-text-xs x-text-muted">Automatically offered with Card</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          )}
          
          {/* Bank Payments Category */}
          {hasAkahu && (
            <div className="x-method-category">
              <div className="x-category-header">
                <div className="x-category-title">
                  <svg width="20" height="20" viewBox="0 0 24 24" fill="none" className="x-category-icon">
                    <path d="M3 21h18M3 10h18M5 6l7-4 7 4M4 10v11M8 10v11M12 10v11M16 10v11M20 10v11" 
                      stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                  </svg>
                  <h2 className="x-heading-md">Bank payments</h2>
                </div>
                <p className="x-text-sm x-text-muted">
                  Direct bank-to-bank transfers with instant settlement.
                </p>
              </div>
              
              <div className="x-methods-list">
                <div className="x-method-item">
                  <button className="x-method-expand" aria-label="Expand details">
                    <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
                      <path d="M4 6l4 4 4-4"/>
                    </svg>
                  </button>
                  
                  <div className="x-method-icon x-method-icon--bank">
                    <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
                      <path d="M3 21h18M3 10h18M5 6l7-4 7 4M4 10v11M8 10v11M12 10v11M16 10v11M20 10v11" 
                        stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                    </svg>
                  </div>
                  
                  <div className="x-method-content">
                    <div className="x-method-title">
                      <strong>Pay by bank</strong>
                      <span className="x-method-provider">by Akahu</span>
                    </div>
                    <p className="x-method-subtitle">Available in New Zealand</p>
                  </div>
                  
                  <div className="x-method-status">
                    <span className="x-status-badge x-status-badge--on">
                      On
                      <svg width="14" height="14" viewBox="0 0 16 16" fill="currentColor">
                        <path d="M5 8l2 2 4-4" strokeWidth="2"/>
                      </svg>
                    </span>
                  </div>
                </div>
              </div>
            </div>
          )}
          
          {/* Direct Debit Category */}
          {hasGoCardless && (
            <div className="x-method-category">
              <div className="x-category-header">
                <div className="x-category-title">
                  <svg width="20" height="20" viewBox="0 0 24 24" fill="none" className="x-category-icon">
                    <path d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z" stroke="currentColor" strokeWidth="2"/>
                    <path d="M9 12l2 2 4-4" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                  </svg>
                  <h2 className="x-heading-md">Direct Debit</h2>
                </div>
                <p className="x-text-sm x-text-muted">
                  Collect recurring payments directly from bank accounts.
                </p>
              </div>
              
              <div className="x-methods-list">
                <div className="x-method-item">
                  <button className="x-method-expand" aria-label="Expand details">
                    <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
                      <path d="M4 6l4 4 4-4"/>
                    </svg>
                  </button>
                  
                  <div className="x-method-icon x-method-icon--debit">
                    <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
                      <path d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z" stroke="currentColor" strokeWidth="2"/>
                      <path d="M9 12l2 2 4-4" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                    </svg>
                  </div>
                  
                  <div className="x-method-content">
                    <div className="x-method-title">
                      <strong>Direct Debit</strong>
                      <span className="x-method-provider">by GoCardless</span>
                    </div>
                    <p className="x-method-subtitle">Available in NZ, AU, UK, EU</p>
                  </div>
                  
                  <div className="x-method-status">
                    <span className="x-status-badge x-status-badge--on">
                      On
                      <svg width="14" height="14" viewBox="0 0 16 16" fill="currentColor">
                        <path d="M5 8l2 2 4-4" strokeWidth="2"/>
                      </svg>
                    </span>
                  </div>
                </div>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  )
}
