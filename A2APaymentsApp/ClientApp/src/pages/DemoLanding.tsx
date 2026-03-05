import { useNavigate } from 'react-router-dom'
import XUIButton from '@xero/xui/react/button'
import { usePrototypeConfig } from '../config/PrototypeConfigContext'
import { CONFIG_LABELS } from '../types/PrototypeConfig'
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
  const { config } = usePrototypeConfig()
  
  return (
    <div className="x-demo-landing">
      <div className="x-demo-container">
        {/* Configuration Status Banner */}
        <div className="x-demo-config-banner">
          <div className="x-demo-config-banner__content">
            <div className="x-demo-config-banner__title">Current Configuration</div>
            <div className="x-demo-config-banner__details">
              {CONFIG_LABELS.stripe[config.stripe]} • {CONFIG_LABELS.bankAccounts[config.bankAccounts]} • 
              {' '}{CONFIG_LABELS.a2aStatus[config.a2aStatus]} • {CONFIG_LABELS.flowVariant[config.flowVariant]}
            </div>
          </div>
          <XUIButton
            variant="borderless-standard"
            size="small"
            onClick={() => navigate('/')}
          >
            Change Configuration
          </XUIButton>
        </div>

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
            <h2 className="x-heading-lg">Invoice Entry Points</h2>
            <p className="x-text-md x-demo-card__description">
              View a realistic invoice editor with TWO ways to trigger payment setup onboarding:
              (1) Prominent banner at top of page, and (2) "Set up online payments" button that opens OPMM modal.
              Both entry points tracked separately for testing conversion approaches.
            </p>
            
            <div className="x-demo-features">
              <h3 className="x-text-sm x-text-muted">Features demonstrated:</h3>
              <ul className="x-demo-features-list">
                <li>High-contrast informational banner (entry: invoice.banner)</li>
                <li>OPMM modal from "Set up online payments" button (entry: invoice.modal)</li>
                <li>Realistic Xero invoice layout with breadcrumbs</li>
                <li>Demo configuration controls for flow variant</li>
                <li>Direct flow to onboarding from invoice context</li>
              </ul>
            </div>
            
            <div className="x-demo-card__actions">
              <XUIButton
                variant="main"
                onClick={() => navigate('/invoice/INV-001')}
              >
                View Invoice Page
              </XUIButton>
            </div>
          </div>
          
          {/* Entry Point 3: Settings (moved from original) */}
          <div className="x-demo-card x-demo-card--secondary">
            <div className="x-demo-card__badge x-demo-card__badge--existing">Entry Point 3 (Primary)</div>
            <h2 className="x-heading-lg">Online Payments Settings</h2>
            <p className="x-text-md x-demo-card__description">
              Primary settings entry point with Pay by bank tile, settlement account management, 
              and state-based CTAs (first-time vs. manage modes).
            </p>
            
            <div className="x-demo-card__actions">
              <XUIButton
                variant="standard"
                onClick={() => navigate('/settings/online-payments')}
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
