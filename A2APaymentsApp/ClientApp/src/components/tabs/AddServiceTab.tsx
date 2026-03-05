import XUIButton from '@xero/xui/react/button'
import type { PaymentServiceConfig} from '../../types/PaymentServiceTypes.ts'
import './AddServiceTab.css'

interface AddServiceTabProps {
  services: PaymentServiceConfig[]
  onStartSetup: (serviceId: string) => void
  onResumeSetup: (serviceId: string) => void
}

export function AddServiceTab({ services, onStartSetup, onResumeSetup }: AddServiceTabProps) {
  const availableServices = services.filter(s => 
    s.status === 'NOT_CONFIGURED' || s.status === 'SETUP_STARTED'
  )
  
  if (availableServices.length === 0) {
    return (
      <div className="x-add-service-tab">
        <div className="x-empty-state">
          <svg width="48" height="48" viewBox="0 0 24 24" fill="none" className="x-empty-icon">
            <path d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
          </svg>
          <h3 className="x-heading-md">All available services connected</h3>
          <p className="x-text-md x-text-muted">
            You've connected all available payment services for your region
          </p>
        </div>
      </div>
    )
  }
  
  return (
    <div className="x-add-service-tab">
      <div className="x-add-service-section">
        <h2 className="x-heading-md">Add a payment service</h2>
        
        <div className="x-service-cards">
          {availableServices.map((service) => (
            <div key={service.id} className={`x-add-service-card ${service.status === 'SETUP_STARTED' ? 'x-add-service-card--started' : ''}`}>
              {service.status === 'SETUP_STARTED' && (
                <div className="x-card-banner">
                  Setup started
                </div>
              )}
              
              <div className="x-card-header">
                <h3 className="x-heading-md">{service.name}</h3>
                <p className="x-text-sm x-provider-attribution">
                  Powered by {' '}
                  <a href="#" className="x-text-link">
                    {service.provider.replace('Powered by ', '')}
                  </a>
                </p>
              </div>
              
              <p className="x-card-description">
                {service.longDescription}
              </p>
              
              <div className="x-card-actions">
                {service.status === 'NOT_CONFIGURED' ? (
                  <XUIButton
                    variant="main"
                    onClick={() => onStartSetup(service.id)}
                  >
                    Set up {service.name}
                  </XUIButton>
                ) : (
                  <XUIButton
                    variant="main"
                    onClick={() => onResumeSetup(service.id)}
                  >
                    Resume setup
                  </XUIButton>
                )}
              </div>
            </div>
          ))}
        </div>
        
        <div className="x-add-service-footer">
          <p className="x-text-md">
            If you're wanting a payment service that isn't listed,{' '}
            <a href="#" className="x-text-link">add another online payment option</a>.
          </p>
        </div>
      </div>
      
      {/* History and notes section */}
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
