import { useNavigate } from 'react-router-dom'
import XUIButton from '@xero/xui/react/button'
import './PaymentServiceSelector.css'

interface PaymentServiceOption {
  id: string
  title: string
  provider: string
  description: string
  buttonText: string
  available: boolean
  route?: string
}

function PaymentServiceSelector() {
  const navigate = useNavigate()

  const paymentServices: PaymentServiceOption[] = [
    {
      id: 'cards-wallets',
      title: 'Cards and digital wallets',
      provider: 'Powered by Stripe',
      description: 'Make receiving invoices easy by accepting credit, debit cards, or digital wallets like Apple Pay and Google Pay.',
      buttonText: 'Set up cards & digital wallets',
      available: false
    },
    {
      id: 'direct-debit',
      title: 'Direct Debit',
      provider: 'Powered by GoCardless',
      description: 'Automate payment collection and reconciliation for any Xero invoice.',
      buttonText: 'Set up Direct Debit',
      available: false
    },
    {
      id: 'pay-by-bank',
      title: 'Pay by bank (A2A)',
      provider: 'Powered by Akahu',
      description: 'Let your customers pay invoices instantly with their bank account using account-to-account payments.',
      buttonText: 'Set up Pay by bank',
      available: true,
      route: '/merchant-onboarding'
    },
    {
      id: 'paypal',
      title: 'PayPal',
      provider: '',
      description: 'Let your customers pay invoices instantly with their PayPal account.',
      buttonText: 'Add another account',
      available: false
    }
  ]

  const getCardBorderColor = (serviceId: string) => {
    switch (serviceId) {
      case 'cards-wallets': return '#5AC8FA' // Stripe blue/cyan
      case 'direct-debit': return '#FF9F0A' // GoCardless orange
      case 'pay-by-bank': return '#13B5EA' // Xero blue
      case 'paypal': return '#CCCCCC' // PayPal gray
      default: return '#CCCCCC'
    }
  }

  const handleServiceSelect = (service: PaymentServiceOption) => {
    if (service.available && service.route) {
      navigate(service.route)
    }
  }

  return (
    <div className="payment-service-selector">
      <div className="selector-content xui-page-width-standard">
        <h1 className="xui-heading-xlarge selector-title">Add a payment service</h1>

        <div className="service-cards">
          {paymentServices.map((service) => (
            <div 
              key={service.id} 
              className={`service-card ${!service.available ? 'service-card--disabled' : ''}`}
              style={{ borderTopColor: getCardBorderColor(service.id) }}
            >
              <div className="service-card-header">
                <h2 className="xui-heading-small service-card-title">{service.title}</h2>
                {service.provider && (
                  <p className="service-card-provider xui-text-deemphasis">{service.provider}</p>
                )}
              </div>
              <p className="service-card-description xui-text-deemphasis">{service.description}</p>
              <div className="service-card-footer">
                <XUIButton
                  variant={service.available ? 'main' : 'standard'}
                  isDisabled={!service.available}
                  onClick={() => handleServiceSelect(service)}
                  className="service-card-button"
                >
                  {service.buttonText}
                </XUIButton>
              </div>
            </div>
          ))}
        </div>

        <div className="additional-options xui-u-text-align-center">
          <p className="xui-text-deemphasis">
            If you're wanting a payment service that isn't listed,{' '}
            <a href="#" className="xui-link">add another online payment option</a>.
          </p>
        </div>

        <div className="history-notes">
          <XUIButton variant="borderless-standard">
            History and notes
          </XUIButton>
        </div>
      </div>
    </div>
  )
}

export default PaymentServiceSelector
