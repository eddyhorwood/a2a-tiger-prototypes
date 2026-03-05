import XUIButton from '@xero/xui/react/button'
import { BankAccount } from '../../services/api'
import './Steps.css'

interface ConfirmationStepProps {
  account: BankAccount | null
  onComplete: () => void
}

function ConfirmationStep({ account, onComplete }: ConfirmationStepProps) {
  return (
    <div className="step-container xui-page-width-standard confirmation-step">
      <div className="success-icon xui-margin-bottom-large">
        <svg width="80" height="80" viewBox="0 0 80 80" fill="none" xmlns="http://www.w3.org/2000/svg">
          <circle cx="40" cy="40" r="40" fill="#00B347"/>
          <path d="M25 40L35 50L55 30" stroke="white" strokeWidth="4" strokeLinecap="round" strokeLinejoin="round"/>
        </svg>
      </div>

      <h2 className="xui-heading-xlarge xui-margin-bottom xui-u-text-align-center">
        Pay by bank is now enabled
      </h2>

      <div className="confirmation-summary xui-margin-vertical-xlarge">
        <h3 className="xui-heading-medium xui-margin-bottom">Summary</h3>
        <dl className="summary-list">
          <dt className="summary-label xui-text-deemphasis">Settlement account:</dt>
          <dd className="summary-value xui-heading-small">{account?.name || 'Unknown'}</dd>
          <dd className="summary-subvalue xui-text-deemphasis xui-font-size-small">
            {account?.accountNumber || ''}
          </dd>
        </dl>
      </div>

      <p className="xui-u-text-align-center xui-margin-bottom-xlarge">
        Customers will now see a 'Pay by bank' option on eligible online invoices.
      </p>

      <div className="step-actions xui-u-flex-justify-center">
        <XUIButton variant="main" onClick={onComplete}>
          Done
        </XUIButton>
      </div>
    </div>
  )
}

export default ConfirmationStep
