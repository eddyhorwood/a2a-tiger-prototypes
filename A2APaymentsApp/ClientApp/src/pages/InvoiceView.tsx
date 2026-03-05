import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import XUIButton from '@xero/xui/react/button'
import XUIModal, { XUIModalBody } from '@xero/xui/react/modal'
import { SetupBanner } from '../components/SetupBanner'
import { serializeEntryContext, type EntryContext, type ComplianceVariant } from '../types/EntryContext'
import OnboardingWizardAggressiveFast from './OnboardingWizardAggressiveFast'
import './InvoiceView.css'

interface Invoice {
  id: string
  number: string
  contact: {
    name: string
    email: string
    address: string
  }
  issueDate: string
  dueDate: string
  reference?: string
  currency: string
  items: Array<{
    description: string
    quantity: number
    unitPrice: number
    accountCode: string
    taxRate: string
    total: number
  }>
  subtotal: number
  tax: number
  total: number
  status: 'draft' | 'awaiting-approval' | 'awaiting-payment' | 'paid'
}

/**
 * InvoiceView - Unified invoice view/edit screen with multiple payment setup entry points
 * 
 * Entry points:
 * 1. SetupBanner at top of page (source: 'invoice.banner')
 * 2. "Set up online payments" button → OPMM modal (source: 'invoice.modal')
 * 
 * This mimics the real Xero invoice editor with contextual prompts
 * that encourage merchants to set up online payments.
 */
function InvoiceView() {
  const navigate = useNavigate()
  const [showBanner, setShowBanner] = useState(true)
  const [showPaymentMethodModal, setShowPaymentMethodModal] = useState(false)
  const [showAggressiveOnboarding, setShowAggressiveOnboarding] = useState(false)
  const [paymentServiceConfigured] = useState(false) // In real app, fetch from API
  const [flowVariant, setFlowVariant] = useState<'aggressive' | 'balanced' | 'conservative'>('conservative')
  const [complianceVariant, setComplianceVariant] = useState<ComplianceVariant>('modal')
  const [currentEntryContext, setCurrentEntryContext] = useState<EntryContext | null>(null)
  
  // Mock invoice data - realistic Xero invoice
  const invoice: Invoice = {
    id: 'INV-002',
    number: 'INV-2026-002',
    contact: {
      name: 'Acme Corporation Ltd',
      email: 'accounts@acmecorp.co.nz',
      address: '123 Queen Street, Auckland 1010'
    },
    issueDate: '2026-03-05',
    dueDate: '2026-04-04', // 30 days
    reference: 'PO-12345',
    currency: 'NZD',
    items: [
      {
        description: 'Website Design & Development',
        quantity: 1,
        unitPrice: 4500.00,
        accountCode: '200',
        taxRate: '15% GST on Income',
        total: 4500.00
      },
      {
        description: 'Monthly Hosting & Maintenance',
        quantity: 3,
        unitPrice: 150.00,
        accountCode: '200',
        taxRate: '15% GST on Income',
        total: 450.00
      }
    ],
    subtotal: 4950.00,
    tax: 742.50,
    total: 5692.50,
    status: 'draft'
  }
  
  const handleSetupPayments = () => {
    // Entry point 1: Banner on invoice view/edit
    const context: EntryContext = {
      source: 'invoice.banner',
      mode: 'first_time',
      complianceVariant: complianceVariant,
      returnTo: `/invoice/${invoice.id}`,
      metadata: { 
        invoiceId: invoice.id,
        invoiceAmount: invoice.total,
        contactName: invoice.contact.name
      }
    }
    
    // Aggressive flow opens as modal, others navigate to wizard
    if (flowVariant === 'aggressive') {
      setCurrentEntryContext(context)
      setShowAggressiveOnboarding(true)
    } else {
      const params = serializeEntryContext(context)
      navigate(`/merchant-onboarding?${params.toString()}`)
    }
  }
  
  const handleOpenPaymentMethodModal = () => {
    setShowPaymentMethodModal(true)
  }
  
  const handleConnectPaymentService = () => {
    // Close OPMM modal first
    setShowPaymentMethodModal(false)
    
    // Entry point 2: OPMM Modal from "Set up online payments" button
    const context: EntryContext = {
      source: 'invoice.modal',
      mode: 'first_time',
      complianceVariant: complianceVariant,
      returnTo: `/invoice/${invoice.id}`,
      metadata: { 
        invoiceId: invoice.id,
        invoiceAmount: invoice.total,
        contactName: invoice.contact.name
      }
    }
    
    // Aggressive flow opens as modal, others navigate to wizard
    if (flowVariant === 'aggressive') {
      setCurrentEntryContext(context)
      setShowAggressiveOnboarding(true)
    } else {
      const params = serializeEntryContext(context)
      navigate(`/merchant-onboarding?${params.toString()}`)
    }
  }
  
  const handleLearnMore = () => {
    // Navigate to Payment Services landing page
    navigate('/settings/online-payments')
  }
  
  const handleDismissBanner = () => {
    setShowBanner(false)
    // In real app: record dismissal in user preferences
  }
  
  return (
    <div className="x-page x-invoice-view-page">
      {/* Xero-style navigation breadcrumbs */}
      <div className="x-breadcrumbs">
        <a href="#" onClick={(e) => { e.preventDefault(); navigate('/') }}>Home</a>
        <span className="x-breadcrumbs__separator">›</span>
        <a href="#" onClick={(e) => { e.preventDefault(); navigate('/invoices') }}>Invoices</a>
        <span className="x-breadcrumbs__separator">›</span>
        <span className="x-breadcrumbs__current">{invoice.number}</span>
      </div>
      
      {/* Main content area */}
      <div className="x-invoice-view-container">
        {/* Demo configuration controls - visible at top for both entry points */}
        <details className="x-demo-config" style={{ marginBottom: '20px' }}>
          <summary className="x-text-sm" style={{ cursor: 'pointer', fontWeight: 500, color: '#6B7280', marginBottom: '12px' }}>
            Prototype settings
          </summary>
          <div style={{ padding: '16px', background: '#F9FAFB', borderRadius: '6px', border: '1px solid #E5E7EB' }}>
            {/* Flow length dimension */}
            <div style={{ marginBottom: '12px' }}>
              <p className="x-text-sm" style={{ marginBottom: '6px', fontWeight: 500 }}>Flow length</p>
              <div style={{ display: 'flex', gap: '12px', flexWrap: 'wrap' }}>
                <label style={{ display: 'flex', alignItems: 'center', gap: '6px', cursor: 'pointer' }}>
                  <input
                    type="radio"
                    name="flow"
                    value="aggressive"
                    checked={flowVariant === 'aggressive'}
                    onChange={(e) => setFlowVariant(e.target.value as 'aggressive' | 'balanced' | 'conservative')}
                  />
                  <span className="x-text-sm">Aggressive (< 10 sec)</span>
                </label>
                <label style={{ display: 'flex', alignItems: 'center', gap: '6px', cursor: 'pointer' }}>
                  <input
                    type="radio"
                    name="flow"
                    value="balanced"
                    checked={flowVariant === 'balanced'}
                    onChange={(e) => setFlowVariant(e.target.value as 'aggressive' | 'balanced' | 'conservative')}
                  />
                  <span className="x-text-sm">Balanced (~30 sec)</span>
                </label>
                <label style={{ display: 'flex', alignItems: 'center', gap: '6px', cursor: 'pointer' }}>
                  <input
                    type="radio"
                    name="flow"
                    value="conservative"
                    checked={flowVariant === 'conservative'}
                    onChange={(e) => setFlowVariant(e.target.value as 'aggressive' | 'balanced' | 'conservative')}
                  />
                  <span className="x-text-sm">Conservative (3-5 min)</span>
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
        
        {/* Setup banner - contextual prompt to set up payments */}
        {!paymentServiceConfigured && showBanner && (
          <SetupBanner
            variant="info"
            title="Get paid up to 2× faster with online payments"
            description="Add a 'Pay now' button to your invoices so customers can pay you instantly by bank transfer, card, or direct debit."
            icon={
              <svg width="32" height="32" viewBox="0 0 24 24" fill="none">
                <path 
                  d="M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5" 
                  stroke="currentColor" 
                  strokeWidth="2" 
                  strokeLinecap="round" 
                  strokeLinejoin="round"
                />
              </svg>
            }
            primaryAction={{
              label: 'Set up online payments',
              onClick: handleSetupPayments
            }}
            secondaryAction={{
              label: 'Learn more',
              onClick: handleLearnMore
            }}
            onDismiss={handleDismissBanner}
          />
        )}
        
        {/* Invoice header with actions */}
        <div className="x-invoice-view-header">
          <div className="x-invoice-view-header__left">
            <div className="x-invoice-status-badge x-invoice-status-badge--draft">
              Draft
            </div>
            <h1 className="x-heading-xl">Invoice {invoice.number}</h1>
          </div>
          
          <div className="x-invoice-view-header__actions">
            <XUIButton variant="borderless-standard">
              Delete
            </XUIButton>
            <XUIButton variant="standard">
              Preview
            </XUIButton>
            <XUIButton variant="main">
              Approve & email
            </XUIButton>
          </div>
        </div>
        
        {/* Invoice form card */}
        <div className="x-invoice-card">
          {/* Contact & Date section */}
          <div className="x-invoice-section">
            <div className="x-invoice-field-row">
              <div className="x-invoice-field">
                <label className="x-field-label">To</label>
                <div className="x-contact-display">
                  <div className="x-contact-avatar">{invoice.contact.name.substring(0, 2).toUpperCase()}</div>
                  <div className="x-contact-details">
                    <div className="x-contact-name">{invoice.contact.name}</div>
                    <div className="x-contact-meta">{invoice.contact.email}</div>
                  </div>
                </div>
              </div>
              
              <div className="x-invoice-field x-invoice-field--small">
                <label className="x-field-label">Invoice number</label>
                <div className="x-field-value">{invoice.number}</div>
              </div>
            </div>
            
            <div className="x-invoice-field-row">
              <div className="x-invoice-field x-invoice-field--small">
                <label className="x-field-label">Reference</label>
                <div className="x-field-value">{invoice.reference || '-'}</div>
              </div>
              
              <div className="x-invoice-field x-invoice-field--small">
                <label className="x-field-label">Issue date</label>
                <div className="x-field-value">
                  {new Date(invoice.issueDate).toLocaleDateString('en-NZ', { day: 'numeric', month: 'short', year: 'numeric' })}
                </div>
              </div>
              
              <div className="x-invoice-field x-invoice-field--small">
                <label className="x-field-label">Due date</label>
                <div className="x-field-value">
                  {new Date(invoice.dueDate).toLocaleDateString('en-NZ', { day: 'numeric', month: 'short', year: 'numeric' })}
                </div>
              </div>
              
              <div className="x-invoice-field x-invoice-field--small">
                <label className="x-field-label">Currency</label>
                <div className="x-field-value">{invoice.currency}</div>
              </div>
            </div>
          </div>
          
          {/* Online payments section */}
          {!paymentServiceConfigured && (
            <div className="x-invoice-section" style={{ borderTop: '1px solid var(--xui-color-border-subtle)', paddingTop: '16px' }}>
              <div className="x-invoice-field">
                <label className="x-field-label">Online payments</label>
                <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                  <XUIButton
                    variant="borderless-main"
                    onClick={handleOpenPaymentMethodModal}
                  >
                    Set up online payments
                  </XUIButton>
                  <span className="x-text-sm" style={{ color: 'var(--xui-color-text-secondary)' }}>
                    Add a 'Pay now' button to your invoices
                  </span>
                </div>
              </div>
            </div>
          )}
          
          {/* Line items table */}
          <div className="x-invoice-section">
            <table className="x-invoice-items-table">
              <thead>
                <tr>
                  <th className="x-table-header">Description</th>
                  <th className="x-table-header x-table-header--right">Qty</th>
                  <th className="x-table-header x-table-header--right">Unit price</th>
                  <th className="x-table-header x-table-header--center">Tax rate</th>
                  <th className="x-table-header x-table-header--right">Amount {invoice.currency}</th>
                </tr>
              </thead>
              <tbody>
                {invoice.items.map((item, index) => (
                  <tr key={index} className="x-invoice-item-row">
                    <td className="x-invoice-item-description">{item.description}</td>
                    <td className="x-table-cell--right">{item.quantity}</td>
                    <td className="x-table-cell--right">{item.unitPrice.toFixed(2)}</td>
                    <td className="x-table-cell--center">{item.taxRate}</td>
                    <td className="x-table-cell--right x-table-cell--bold">{item.total.toFixed(2)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            
            {/* Add line button */}
            <div className="x-invoice-add-line">
              <XUIButton variant="borderless-standard">
                + Add a line
              </XUIButton>
            </div>
          </div>
          
          {/* Totals section */}
          <div className="x-invoice-section x-invoice-totals-section">
            <div className="x-invoice-totals">
              <div className="x-invoice-total-row">
                <span className="x-total-label">Subtotal</span>
                <span className="x-total-value">{invoice.subtotal.toFixed(2)}</span>
              </div>
              <div className="x-invoice-total-row">
                <span className="x-total-label">GST</span>
                <span className="x-total-value">{invoice.tax.toFixed(2)}</span>
              </div>
              <div className="x-invoice-total-row x-invoice-total-row--grand">
                <span className="x-total-label">Total</span>
                <span className="x-total-value">{invoice.currency} {invoice.total.toFixed(2)}</span>
              </div>
            </div>
          </div>
        </div>
        
        {/* Notes section */}
        <div className="x-invoice-notes">
          <h3 className="x-heading-sm">Notes</h3>
          <textarea 
            className="x-invoice-notes-textarea"
            placeholder="Add notes for your customer (e.g., payment terms, thank you message)"
            rows={3}
          />
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
          {/* Main heading and subheading */}
          <div style={{ textAlign: 'center', marginBottom: '32px' }}>
            <h2 className="x-heading-xl" style={{ marginBottom: '12px', fontSize: '28px', fontWeight: 600 }}>
              Give your customers more ways to pay
            </h2>
            <p className="x-text-md" style={{ color: '#6B7280' }}>
              Join businesses like you who are getting paid 2x faster with online payments
            </p>
          </div>

          {/* Payment option card */}
          <div className="x-payment-option-card">
            <div className="x-payment-option-content">
              <h3 className="x-heading-md" style={{ marginBottom: '4px', fontSize: '18px', fontWeight: 600 }}>
                Account to account bank payments
              </h3>
              <p className="x-text-sm" style={{ color: '#6B7280', marginBottom: '16px' }}>
                Powered by Akahu
              </p>

              {/* Bank logos */}
              <div style={{ display: 'flex', gap: '8px', marginBottom: '20px', flexWrap: 'wrap' }}>
                <div className="x-bank-logo">ANZ</div>
                <div className="x-bank-logo">ASB</div>
                <div className="x-bank-logo">BNZ</div>
                <div className="x-bank-logo">Westpac</div>
                <div className="x-bank-logo">Kiwibank</div>
              </div>

              {/* Feature list with checkmarks */}
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

          {/* CTA button */}
          <div style={{ marginTop: '32px', textAlign: 'center' }}>
            <XUIButton
              variant="main"
              onClick={handleConnectPaymentService}
              style={{ width: '100%', maxWidth: '400px', height: '48px', fontSize: '16px', fontWeight: 600 }}
            >
              Get set up with Akahu
            </XUIButton>
            
            {/* Explore all options link */}
            <a 
              href="#" 
              className="x-text-sm"
              onClick={(e) => {
                e.preventDefault()
                navigate('/settings/online-payments')
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
      
      {/* Aggressive Onboarding Modal */}
      {showAggressiveOnboarding && currentEntryContext && (
        <OnboardingWizardAggressiveFast
          entryContext={currentEntryContext}
          onClose={() => setShowAggressiveOnboarding(false)}
        />
      )}
    </div>
  )
}

export default InvoiceView
