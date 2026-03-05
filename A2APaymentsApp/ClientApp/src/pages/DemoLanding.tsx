import { useNavigate } from 'react-router-dom'
import XUIButton from '@xero/xui/react/button'
import './DemoLanding.css'

/**
 * DemoLanding - Overview page showing all entry points
 * 
 * This page demonstrates the two main entry points for payment setup:
 * 1. Settings-initiated flow (Payment Services landing page)
 * 2. Invoice-initiated flow (Contextual banner on invoice)
 */
function DemoLanding() {
  const navigate = useNavigate()
  
  return (
    <div className="x-demo-landing">
      <div className="x-demo-container">
        <div className="x-demo-header">
          <h1 className="x-heading-xxl">A2A Bank Payments Prototype</h1>
          <p className="x-text-lg x-text-muted">
            Explore realistic entry points for setting up account-to-account bank payments in Xero
          </p>
        </div>
        
        <div className="x-demo-grid">
          {/* Entry Point 1: Settings */}
          <div className="x-demo-card">
            <div className="x-demo-card__badge">Entry Point 1</div>
            <h2 className="x-heading-lg">Payment Services Settings</h2>
            <p className="x-text-md x-demo-card__description">
              Navigate to Settings → Online payments to explore the Payment Services landing page. 
              This is where merchants discover, configure, and manage all payment providers.
            </p>
            
            <div className="x-demo-features">
              <h3 className="x-text-sm x-text-muted">Features demonstrated:</h3>
              <ul className="x-demo-features-list">
                <li>Provider status management (Not configured / Setup started / Setup complete / Error)</li>
                <li>"Get set up", "Resume setup", and "Manage" CTAs</li>
                <li>Contextual banners for incomplete setups</li>
                <li>Settlement account and fee account display</li>
                <li>Multiple payment service providers (Pay by bank, Cards, Direct Debit)</li>
              </ul>
            </div>
            
            <div className="x-demo-card__actions">
              <XUIButton
                variant="main"
                onClick={() => navigate('/settings/online-payments')}
              >
                View Settings Flow
              </XUIButton>
            </div>
          </div>
          
          {/* Entry Point 2: Invoice Banner */}
          <div className="x-demo-card">
            <div className="x-demo-card__badge x-demo-card__badge--new">Entry Point 2 (New)</div>
            <h2 className="x-heading-lg">Invoice with Banner</h2>
            <p className="x-text-md x-demo-card__description">
              View a realistic invoice editor with a contextual banner prompting merchants to set up 
              online payments. This matches the real Xero pattern for in-context payment setup.
            </p>
            
            <div className="x-demo-features">
              <h3 className="x-text-sm x-text-muted">Features demonstrated:</h3>
              <ul className="x-demo-features-list">
                <li>High-contrast informational banner above invoice</li>
                <li>Realistic Xero invoice layout with breadcrumbs</li>
                <li>Primary and secondary CTAs in banner</li>
                <li>Dismissible banner pattern</li>
                <li>Direct flow to onboarding from invoice context</li>
              </ul>
            </div>
            
            <div className="x-demo-card__actions">
              <XUIButton
                variant="main"
                onClick={() => navigate('/invoice-view/INV-002')}
              >
                View Invoice Banner
              </XUIButton>
            </div>
          </div>
          
          {/* Entry Point 3: Invoice with Modal (existing) */}
          <div className="x-demo-card x-demo-card--secondary">
            <div className="x-demo-card__badge x-demo-card__badge--existing">Entry Point 3 (Existing)</div>
            <h2 className="x-heading-lg">Invoice with Payment Options Modal</h2>
            <p className="x-text-md x-demo-card__description">
              Original prototype with inline payment options modal (OPMM pattern). 
              Still functional for comparison with the new banner approach.
            </p>
            
            <div className="x-demo-card__actions">
              <XUIButton
                variant="standard"
                onClick={() => navigate('/invoice/INV-001')}
              >
                View Modal Flow
              </XUIButton>
            </div>
          </div>
        </div>
        
        {/* About Section */}
        <div className="x-demo-about">
          <h2 className="x-heading-md">About this prototype</h2>
          <p className="x-text-md">
            This prototype demonstrates realistic Xero entry points for setting up account-to-account 
            bank payments powered by Akahu. It follows actual Xero patterns for payment service onboarding, 
            including CP Provisioning flows, provider state management, and contextual nudges.
          </p>
          <p className="x-text-md">
            The implementation follows Xero Design Guidelines (CSS variables, 4px grid, XUI components) 
            and AML/CFT compliance requirements (no fund custody language, clear provider roles).
          </p>
        </div>
      </div>
    </div>
  )
}

export default DemoLanding
