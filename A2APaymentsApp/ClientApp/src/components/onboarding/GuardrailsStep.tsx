import XUIButton from '@xero/xui/react/button'
import XUILoader from '@xero/xui/react/loader'
import './Steps.css'

interface GuardrailsStepProps {
  acknowledged: boolean
  onAcknowledgeChange: (value: boolean) => void
  onSubmit: () => void
  onBack: () => void
  loading: boolean
}

function GuardrailsStep({
  acknowledged,
  onAcknowledgeChange,
  onSubmit,
  onBack,
  loading
}: GuardrailsStepProps) {
  return (
    <div className="step-container xui-page-width-standard">
      <h2 className="xui-heading-large xui-margin-bottom-large">
        Understand how Pay by bank works
      </h2>

      <div className="legal-content xui-margin-bottom-xlarge">
        <p className="xui-margin-bottom">
          Pay by bank uses Akahu to initiate direct bank transfers between your 
          customer's bank account and your bank account. Funds move directly 
          between banks. Neither Xero nor Akahu hold or pool funds at any time.
        </p>

        <ul className="xui-list xui-margin-bottom-large">
          <li className="xui-margin-bottom-small">
            <strong>Akahu initiates the payment</strong>; the bank executes the transfer.
          </li>
          <li className="xui-margin-bottom-small">
            <strong>Xero collects payment details</strong> and passes them to Akahu; 
            Xero does not transfer or hold customer funds.
          </li>
        </ul>

        <div className="acknowledgement-checkbox">
          <label className="checkbox-label">
            <input
              type="checkbox"
              checked={acknowledged}
              onChange={(e) => onAcknowledgeChange(e.target.checked)}
              className="checkbox-input"
            />
            <span className="checkbox-text">
              I understand that Pay by bank is a direct bank-to-bank transfer 
              initiated via Akahu, and that neither Xero nor Akahu hold or 
              transfer funds on my behalf.
            </span>
          </label>
        </div>
      </div>

      <div className="step-actions">
        <XUIButton variant="borderless-main" onClick={onBack} isDisabled={loading}>
          Back
        </XUIButton>
        <XUIButton
          variant="main"
          isDisabled={!acknowledged || loading}
          onClick={onSubmit}
        >
          {loading ? (
            <>
              <XUILoader ariaLabel="Enabling" size="small" />
              <span style={{ marginLeft: '0.5rem' }}>Enabling...</span>
            </>
          ) : (
            'Enable Pay by bank'
          )}
        </XUIButton>
      </div>
    </div>
  )
}

export default GuardrailsStep
