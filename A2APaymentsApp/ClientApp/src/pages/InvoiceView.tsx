import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import XUIButton from '@xero/xui/react/button'
import { SetupBanner } from '../components/SetupBanner'
import { serializeEntryContext, type EntryContext } from '../types/EntryContext'
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
 * InvoiceView - Realistic Xero invoice view/edit screen
 * 
 * Entry point: Banner prompting payment setup
 * This mimics the real Xero invoice editor with contextual banners
 * that encourage merchants to set up online payments.
 * 
 * Banner appears when:
 * - Invoice is draft or sent
 * - Merchant has no payment services configured
 * - Banner hasn't been dismissed recently
 */
function InvoiceView() {
  const navigate = useNavigate()
  const [showBanner, setShowBanner] = useState(true)
  const [paymentServiceConfigured] = useState(false) // In real app, fetch from API
  
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
    // Entry point: Banner on invoice view/edit
    const context: EntryContext = {
      source: 'banner',
      mode: 'first_time',
      returnTo: `/invoice/${invoice.id}`,
      metadata: { 
        invoiceId: invoice.id,
        invoiceAmount: invoice.total,
        contactName: invoice.contact.name
      }
    }
    const params = serializeEntryContext(context)
    navigate(`/merchant-onboarding?${params.toString()}`)
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
    </div>
  )
}

export default InvoiceView
