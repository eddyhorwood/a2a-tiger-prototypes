import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import XUIButton from '@xero/xui/react/button'
import XUIModal, { XUIModalBody } from '@xero/xui/react/modal'
import { serializeEntryContext, type EntryContext, type ComplianceVariant } from '../types/EntryContext'
import './InvoiceDetail.css'

interface Invoice {
  id: string
  number: string
  contact: string
  amount: number
  currency: string
  dueDate: string
  status: 'draft' | 'sent' | 'paid'
  items: Array<{
    description: string
    quantity: number
    unitPrice: number
    total: number
  }>
}

function InvoiceDetail() {
  const navigate = useNavigate()
  const [showPaymentMethodModal, setShowPaymentMethodModal] = useState(false)
  const [flowVariant, setFlowVariant] = useState<'balanced' | 'aggressive'>('balanced')
  const [complianceVariant, setComplianceVariant] = useState<ComplianceVariant>('modal')
  
  // Mock invoice data - realistic Xero invoice
  const invoice: Invoice = {
    id: 'INV-001',
    number: 'INV-2026-001',
    contact: 'Acme Corporation',
    amount: 2500.00,
    currency: 'NZD',
    dueDate: '2026-03-25',
    status: 'draft',
    items: [
      { description: 'Consulting Services', quantity: 20, unitPrice: 125, total: 2500 }
    ]
  }
  
  const handleConnectPaymentService = () => {
    // Entry point: Invoice → OPMM Modal → Connect payment service
    const context: EntryContext = {
      source: 'invoice.modal',
      mode: 'first_time',
      returnTo: `/invoice/${invoice.id}`,
      metadata: { invoiceId: invoice.id }
    }
    const params = serializeEntryContext(context)
    setShowPaymentMethodModal(false)
    
    // Route to appropriate flow variant
    const path = flowVariant === 'aggressive' 
      ? `/merchant-onboarding-aggressive?${params.toString()}`
      : `/merchant-onboarding?${params.toString()}`
    navigate(path)
  }
  
  return (
    <div className="x-invoice-page">
      {/* Xero-style invoice header */}
      <div className="x-invoice-header">
        <div className="x-invoice-header__title">
          <button className="x-back-link" onClick={() => navigate('/')}>
            ← Back
          </button>
          <h1 className="x-heading-xl">Invoice {invoice.number}</h1>
        </div>
        <div className="x-invoice-header__actions">
          <XUIButton variant="standard">Preview</XUIButton>
          <XUIButton variant="main">Save & continue</XUIButton>
        </div>
      </div>

      {/* Invoice form section */}
      <div className="x-invoice-form">
        {/* Top row: Contact, Issue date, Due date, Invoice number, Reference */}
        <div className="x-invoice-header-row">
          <div className="x-invoice-form__field">
            <label className="x-text-sm x-text-muted">Contact</label>
            <div className="x-contact-badge">
              <span className="x-contact-initials">AT</span>
              <span className="x-contact-name">{invoice.contact}</span>
              <button className="x-contact-remove" aria-label="Remove contact">×</button>
            </div>
          </div>
          <div className="x-invoice-form__field">
            <label className="x-text-sm x-text-muted">Issue date</label>
            <div className="x-form-input">
              <span className="x-input-icon">📅</span>
              <span className="x-text-md">5 Mar 2026</span>
            </div>
          </div>
          <div className="x-invoice-form__field">
            <label className="x-text-sm x-text-muted">Due date</label>
            <div className="x-form-input">
              <span className="x-input-icon">📅</span>
              <span className="x-text-md">{new Date(invoice.dueDate).toLocaleDateString()}</span>
            </div>
          </div>
          <div className="x-invoice-form__field">
            <label className="x-text-sm x-text-muted">Invoice number</label>
            <div className="x-form-input">
              <span className="x-input-icon">#</span>
              <span className="x-text-md">{invoice.number}</span>
            </div>
          </div>
          <div className="x-invoice-form__field">
            <label className="x-text-sm x-text-muted">Reference</label>
            <div className="x-form-input">
              <span className="x-input-icon">🏷️</span>
              <span className="x-text-md x-text-muted">-</span>
            </div>
          </div>
        </div>

        {/* Second row: Online payments, Currency, Amounts are */}
        <div className="x-invoice-header-row" style={{ marginTop: '16px' }}>
          <div className="x-invoice-form__field">
            <label className="x-text-sm x-text-muted">Online payments</label>
            <div style={{ marginTop: '4px' }}>
              <XUIButton
                variant="borderless-main"
                onClick={() => setShowPaymentMethodModal(true)}
                style={{ padding: '6px 12px', fontSize: '14px' }}
              >
                Set up online payments
              </XUIButton>
              <div className="x-payment-card-logos" style={{ display: 'flex', gap: '4px', marginTop: '8px' }}>
                <img src="data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='32' height='20' viewBox='0 0 32 20'%3E%3Crect fill='%231A1F71' width='32' height='20' rx='2'/%3E%3Ctext x='50%25' y='50%25' fill='white' font-size='8' font-weight='bold' text-anchor='middle' dy='.3em'%3EVISA%3C/text%3E%3C/svg%3E" alt="Visa" className="x-card-logo" />
                <img src="data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='32' height='20' viewBox='0 0 32 20'%3E%3Crect fill='%23EB001B' width='32' height='20' rx='2'/%3E%3Ccircle cx='12' cy='10' r='6' fill='%23EB001B'/%3E%3Ccircle cx='20' cy='10' r='6' fill='%23F79E1B'/%3E%3C/svg%3E" alt="Mastercard" className="x-card-logo" />
                <img src="data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='32' height='20' viewBox='0 0 32 20'%3E%3Crect fill='%23006FCF' width='32' height='20' rx='2'/%3E%3Ctext x='50%25' y='50%25' fill='white' font-size='6' font-weight='bold' text-anchor='middle' dy='.3em'%3EAMEX%3C/text%3E%3C/svg%3E" alt="Amex" className="x-card-logo" />
              </div>
            </div>
          </div>
          <div className="x-invoice-form__field">
            <label className="x-text-sm x-text-muted">Currency</label>
            <div className="x-form-input x-form-select">
              <span className="x-text-md">Malaysian Ringgit</span>
              <span className="x-dropdown-icon">▼</span>
            </div>
          </div>
          <div className="x-invoice-form__field">
            <label className="x-text-sm x-text-muted">Amounts are</label>
            <div className="x-form-input x-form-select">
              <span className="x-text-md">Tax exclusive</span>
              <span className="x-dropdown-icon">▼</span>
            </div>
          </div>
          <div className="x-invoice-form__field">
            {/* Empty spacer to maintain grid */}
          </div>
          <div className="x-invoice-form__field">
            <a href="#" className="x-text-sm" style={{ color: '#3B82F6', textDecoration: 'none', marginTop: '24px', display: 'inline-block' }}>
              Add contact's last items
            </a>
          </div>
        </div>

        {/* Invoice items table */}
        <div className="x-invoice-items">
          <table className="x-invoice-items__table">
            <thead>
              <tr>
                <th>Description</th>
                <th className="x-text-right">Qty</th>
                <th className="x-text-right">Unit price</th>
                <th className="x-text-right">Amount</th>
              </tr>
            </thead>
            <tbody>
              {invoice.items.map((item, index) => (
                <tr key={index}>
                  <td>{item.description}</td>
                  <td className="x-text-right">{item.quantity}</td>
                  <td className="x-text-right">{invoice.currency} {item.unitPrice.toFixed(2)}</td>
                  <td className="x-text-right">{invoice.currency} {item.total.toFixed(2)}</td>
                </tr>
              ))}
            </tbody>
          </table>
          <div className="x-invoice-items__total">
            <span className="x-text-md x-text-muted">Total</span>
            <span className="x-heading-md">{invoice.currency} {invoice.amount.toFixed(2)}</span>
          </div>
        </div>
      </div>

      {/* OPMM Modal - Entry point to onboarding wizard */}
      <XUIModal
        id="payment-method-management-modal"
        isOpen={showPaymentMethodModal}
        size="large"
        closeButtonLabel="Close modal"
        onClose={() => setShowPaymentMethodModal(false)}
      >
        <XUIModalBody>
          {/* Main heading and subheading - matches screenshot */}
          <div style={{ textAlign: 'center', marginBottom: '32px' }}>
            <h2 className="x-heading-xl" style={{ marginBottom: '12px', fontSize: '28px', fontWeight: 600 }}>
              Give your customers more ways to pay
            </h2>
            <p className="x-text-md" style={{ color: '#6B7280' }}>
              Join businesses like you who are getting paid 2x faster with online payments
            </p>
          </div>

          {/* Payment option card - matches Stripe card style from screenshot */}
          <div className="x-payment-option-card">
            <div className="x-payment-option-content">
              <h3 className="x-heading-md" style={{ marginBottom: '4px', fontSize: '18px', fontWeight: 600 }}>
                Account to account bank payments
              </h3>
              <p className="x-text-sm" style={{ color: '#6B7280', marginBottom: '16px' }}>
                Powered by Akahu
              </p>

              {/* Bank logos placeholder - simulating payment method icons */}
              <div style={{ display: 'flex', gap: '8px', marginBottom: '20px', flexWrap: 'wrap' }}>
                <div className="x-bank-logo">ANZ</div>
                <div className="x-bank-logo">ASB</div>
                <div className="x-bank-logo">BNZ</div>
                <div className="x-bank-logo">Westpac</div>
                <div className="x-bank-logo">Kiwibank</div>
              </div>

              {/* Feature list with checkmarks - matches screenshot */}
              <ul className="x-feature-list">
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
            </div>
          </div>

          {/* Demo configuration - styled to be less prominent */}
          <details className="x-demo-config" style={{ marginTop: '24px' }}>
            <summary className="x-text-sm" style={{ cursor: 'pointer', fontWeight: 500, color: '#6B7280', marginBottom: '12px' }}>
              Prototype settings
            </summary>
            <div style={{ padding: '16px', background: '#F9FAFB', borderRadius: '6px', border: '1px solid #E5E7EB' }}>
              {/* Flow length dimension */}
              <div style={{ marginBottom: '12px' }}>
                <p className="x-text-sm" style={{ marginBottom: '6px', fontWeight: 500 }}>Flow length</p>
                <div style={{ display: 'flex', gap: '12px' }}>
                  <label style={{ display: 'flex', alignItems: 'center', gap: '6px', cursor: 'pointer' }}>
                    <input
                      type="radio"
                      name="flow"
                      value="balanced"
                      checked={flowVariant === 'balanced'}
                      onChange={(e) => setFlowVariant(e.target.value as 'balanced' | 'aggressive')}
                    />
                    <span className="x-text-sm">Long (3-5 min)</span>
                  </label>
                  <label style={{ display: 'flex', alignItems: 'center', gap: '6px', cursor: 'pointer' }}>
                    <input
                      type="radio"
                      name="flow"
                      value="aggressive"
                      checked={flowVariant === 'aggressive'}
                      onChange={(e) => setFlowVariant(e.target.value as 'balanced' | 'aggressive')}
                    />
                    <span className="x-text-sm">Short (30 sec)</span>
                  </label>
                </div>
              </div>
              
              {/* Compliance disclosure dimension */}
              <div>
                <p className="x-text-sm" style={{ marginBottom: '6px', fontWeight: 500 }}>Compliance disclosure</p>
                <div style={{ display: 'flex', gap: '12px' }}>
                  <label style={{ display: 'flex', alignItems: 'center', gap: '6px', cursor: 'pointer' }}>
                    <input
                      type="radio"
                      name="compliance"
                      value="modal"
                      checked={complianceVariant === 'modal'}
                      onChange={(e) => setComplianceVariant(e.target.value as ComplianceVariant)}
                    />
                    <span className="x-text-sm">Modal</span>
                  </label>
                  <label style={{ display: 'flex', alignItems: 'center', gap: '6px', cursor: 'pointer' }}>
                    <input
                      type="radio"
                      name="compliance"
                      value="banner"
                      checked={complianceVariant === 'banner'}
                      onChange={(e) => setComplianceVariant(e.target.value as ComplianceVariant)}
                    />
                    <span className="x-text-sm">Banner</span>
                  </label>
                  <label style={{ display: 'flex', alignItems: 'center', gap: '6px', cursor: 'pointer' }}>
                    <input
                      type="radio"
                      name="compliance"
                      value="fullscreen"
                      checked={complianceVariant === 'fullscreen'}
                      onChange={(e) => setComplianceVariant(e.target.value as ComplianceVariant)}
                    />
                    <span className="x-text-sm">Fullscreen</span>
                  </label>
                </div>
              </div>
            </div>
          </details>

          {/* CTA button - large and prominent like screenshot */}
          <div style={{ marginTop: '32px', textAlign: 'center' }}>
            <XUIButton
              variant="main"
              onClick={handleConnectPaymentService}
              style={{ width: '100%', maxWidth: '400px', height: '48px', fontSize: '16px', fontWeight: 600 }}
            >
              Get set up with Akahu
            </XUIButton>
            
            {/* Explore all options link - matches screenshot */}
            <a 
              href="#" 
              className="x-text-sm"
              onClick={(e) => {
                e.preventDefault()
                // Could navigate to settings or show more options
              }}
              style={{ 
                display: 'inline-block',
                marginTop: '16px', 
                color: '#3B82F6',
                textDecoration: 'none',
                fontWeight: 500
              }}
            >
              Explore all online payment options →
            </a>
          </div>
        </XUIModalBody>
      </XUIModal>
    </div>
  )
}

export default InvoiceDetail
