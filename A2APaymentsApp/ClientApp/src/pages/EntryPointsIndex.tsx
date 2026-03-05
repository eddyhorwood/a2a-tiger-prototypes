import { useNavigate } from 'react-router-dom'
import XUIButton from '@xero/xui/react/button'
import './EntryPointsIndex.css'

interface EntryPoint {
  id: string
  number: number
  title: string
  description: string
  surface: string
  route: string
  highlight?: boolean
}

function EntryPointsIndex() {
  const navigate = useNavigate()
  
  const entryPoints: EntryPoint[] = [
    {
      id: 'settings',
      number: 1,
      title: 'Online Payments Settings',
      description: 'Primary entry point with Pay by bank tile in enabled/disabled states',
      surface: 'Settings → Online Payments → Bank payments tile',
      route: '/settings/online-payments',
      highlight: true
    },
    {
      id: 'invoice-inline',
      number: 2,
      title: 'Invoice – Inline Enable Prompt',
      description: 'In-context prompt on invoice detail page when A2A not enabled',
      surface: 'Invoice detail → Payment options section',
      route: '/invoice/INV-001'
    },
    {
      id: 'invoice-modal',
      number: 3,
      title: 'Invoice – Payment Method Modal',
      description: 'Payment method selector modal (OPMM) with Pay by bank option',
      surface: 'Invoice detail → Add payment method modal',
      route: '/invoice/INV-001'
    },
    {
      id: 'banner',
      number: 4,
      title: 'Online Payments Banner',
      description: 'High-contrast promo banner at top of Online Payments settings',
      surface: 'Settings → Online Payments → Banner/promo',
      route: '/settings/online-payments'
    },
    {
      id: 'one-onboarding',
      number: 5,
      title: 'One Onboarding',
      description: 'Guided setup checklist with "Accept direct bank payments" task',
      surface: 'One Onboarding → Setup tasks',
      route: '/onboarding'
    },
    {
      id: 'campaign',
      number: 6,
      title: 'External Campaign / Deep Link',
      description: 'Entry from marketing emails, Xero Central, or campaign landing pages',
      surface: 'External → Deep link handler → Settings',
      route: '/campaign-entry?campaign=email_nz_a2a_launch&utm_source=campaign_demo'
    },
    {
      id: 'manage',
      number: 7,
      title: 'Manage Pay by bank',
      description: 'Edit existing setup (change settlement account, view guardrails)',
      surface: 'Settings → Online Payments → Enabled tile → Manage',
      route: '/settings/online-payments'
    }
  ]
  
  return (
    <div className="entry-points-index">
      <div className="index-header">
        <h1>A2A Payments Prototype</h1>
        <p className="header-subtitle">
          Multiple Entry Points Demo – All flows route to a single canonical onboarding experience
        </p>
      </div>
      
      <div className="index-intro">
        <div className="intro-card">
          <h2>🎯 Demo Focus</h2>
          <p>
            This prototype demonstrates <strong>7 different entry points</strong> into the Pay by bank merchant onboarding flow.
            Each entry preserves context (source, return path, metadata) and routes to the same underlying onboarding wizard.
          </p>
        </div>
        
        <div className="intro-card architecture">
          <h2>🏗️ Architecture</h2>
          <ul>
            <li><strong>Entry Context System:</strong> Tracks source, mode (first-time/manage), return path</li>
            <li><strong>Single Onboarding Flow:</strong> All entries route to one canonical wizard</li>
            <li><strong>Smart Routing:</strong> After completion, user returns to originating surface</li>
            <li><strong>Conditional Messaging:</strong> Content adapts based on entry context</li>
          </ul>
        </div>
      </div>
      
      <div className="entry-points-list">
        <h2>Select an Entry Point</h2>
        
        {entryPoints.map((entry) => (
          <div
            key={entry.id}
            className={`entry-point-card ${entry.highlight ? 'highlight' : ''}`}
          >
            <div className="entry-number">{entry.number}</div>
            <div className="entry-content">
              <h3>{entry.title}</h3>
              <p className="entry-description">{entry.description}</p>
              <p className="entry-surface">
                <span className="surface-label">Surface:</span> {entry.surface}
              </p>
            </div>
            <div className="entry-action">
              <XUIButton
                variant={entry.highlight ? 'primary' : 'secondary'}
                onClick={() => navigate(entry.route)}
              >
                Open
              </XUIButton>
            </div>
          </div>
        ))}
      </div>
      
      <div className="index-footer">
        <p>
          💡 <strong>Tip:</strong> Each entry surface includes a "Back to all entry points" link
          to help you navigate between demos.
        </p>
      </div>
    </div>
  )
}

export default EntryPointsIndex
