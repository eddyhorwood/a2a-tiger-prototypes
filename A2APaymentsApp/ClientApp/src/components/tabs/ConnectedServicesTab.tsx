import XUIButton from '@xero/xui/react/button'
import type { PaymentServiceConfig } from '../../types/PaymentServiceTypes.ts'
import './ConnectedServicesTab.css'

interface ConnectedServicesTabProps {
  services: PaymentServiceConfig[]
  onEdit: (serviceId: string) => void
}

export function ConnectedServicesTab({ services, onEdit }: ConnectedServicesTabProps) {
  const connectedServices = services.filter(s => s.status === 'SETUP_COMPLETE')
  
  if (connectedServices.length === 0) {
    return (
      <div className="x-connected-services-tab">
        <div className="x-empty-state">
          <svg width="48" height="48" viewBox="0 0 24 24" fill="none" className="x-empty-icon">
            <path d="M13 2L3 14h8l-1 8 10-12h-8l1-8z" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
          </svg>
          <h3 className="x-heading-md">No payment services connected</h3>
          <p className="x-text-md x-text-muted">
            Connect a payment service to start accepting online payments
          </p>
        </div>
      </div>
    )
  }
  
  return (
    <div className="x-connected-services-tab">
      <div className="x-services-section">
        <h2 className="x-heading-md">Connected payment services</h2>
        
        <div className="x-services-table-container">
          <table className="x-services-table">
            <thead>
              <tr>
                <th>Account name</th>
                <th>Service</th>
                <th>Branding themes using this service</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {connectedServices.map((service) => (
                <tr key={service.id}>
                  <td className="x-table-account">
                    {service.name}
                  </td>
                  <td className="x-table-service">
                    {service.id === 'stripe' && (
                      <span className="x-provider-logo x-provider-logo--stripe">stripe</span>
                    )}
                    {service.id === 'pay-by-bank' && (
                      <span className="x-provider-logo x-provider-logo--akahu">Akahu</span>
                    )}
                    {service.id === 'gocardless' && (
                      <span className="x-provider-logo x-provider-logo--gocardless">GoCardless</span>
                    )}
                  </td>
                  <td className="x-table-themes">
                    <span className="x-theme-name">Standard</span>
                  </td>
                  <td className="x-table-actions">
                    <XUIButton
                      variant="borderless-standard"
                      onClick={() => onEdit(service.id)}
                    >
                      Edit
                    </XUIButton>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        
        <div className="x-table-footer">
          <XUIButton variant="standard">
            Manage themes
          </XUIButton>
        </div>
      </div>
      
      {/* History and notes section (collapsible) */}
      <details className="x-history-section">
        <summary className="x-history-summary">
          <div className="x-history-title">
            <svg width="20" height="20" viewBox="0 0 16 16" fill="currentColor" className="x-chevron">
              <path d="M4 6l4 4 4-4"/>
            </svg>
            <span>History and notes</span>
          </div>
          <XUIButton variant="borderless-standard">
            Add note
          </XUIButton>
        </summary>
        <div className="x-history-content">
          <p className="x-text-sm x-text-muted">No notes yet</p>
        </div>
      </details>
    </div>
  )
}
