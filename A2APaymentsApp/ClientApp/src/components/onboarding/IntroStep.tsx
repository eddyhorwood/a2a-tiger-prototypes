import XUIButton from '@xero/xui/react/button'
import './Steps.css'

interface IntroStepProps {
  onNext: () => void
}

function IntroStep({ onNext }: IntroStepProps) {
  return (
    <div className="step-container xui-page-width-standard">
      <h1 className="xui-heading-xlarge xui-margin-bottom-large">Enable Pay by bank</h1>
      
      <div className="step-content">
        <ul className="xui-list xui-font-size-large xui-margin-bottom-xlarge">
          <li className="xui-margin-bottom">
            Customers pay you via direct bank transfer from their own bank.
          </li>
          <li className="xui-margin-bottom">
            Payments are initiated via Akahu and executed by the bank.
          </li>
          <li className="xui-margin-bottom">
            Xero and Akahu do not hold or pool funds at any time.
          </li>
        </ul>
      </div>

      <div className="step-actions">
        <XUIButton variant="main" onClick={onNext}>
          Continue
        </XUIButton>
      </div>
    </div>
  )
}

export default IntroStep
