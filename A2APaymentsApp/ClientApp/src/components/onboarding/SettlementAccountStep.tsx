import XUIButton from '@xero/xui/react/button'
import { BankAccount } from '../../services/api'
import './Steps.css'

interface SettlementAccountStepProps {
  accounts: BankAccount[]
  selectedAccountId: string
  onAccountSelect: (accountId: string) => void
  onNext: () => void
  onBack: () => void
}

function SettlementAccountStep({
  accounts,
  selectedAccountId,
  onAccountSelect,
  onNext,
  onBack
}: SettlementAccountStepProps) {
  return (
    <div className="step-container xui-page-width-standard">
      <h2 className="xui-heading-large xui-margin-bottom">Choose settlement account</h2>
      <p className="xui-margin-bottom-large">
        Select the bank account where Pay by bank deposits should go.
      </p>

      <div className="account-list xui-margin-bottom-xlarge">
        {accounts.map((account) => (
          <label
            key={account.accountId}
            className={`account-item ${selectedAccountId === account.accountId ? 'account-item--selected' : ''}`}
          >
            <input
              type="radio"
              name="settlement-account"
              value={account.accountId}
              checked={selectedAccountId === account.accountId}
              onChange={(e) => onAccountSelect(e.target.value)}
              className="account-radio"
            />
            <div className="account-details">
              <div className="account-name xui-heading-small">{account.name}</div>
              <div className="account-number xui-text-deemphasis xui-font-size-small">
                {account.accountNumber}
              </div>
            </div>
            <div className="account-checkmark">
              {selectedAccountId === account.accountId && '✓'}
            </div>
          </label>
        ))}
      </div>

      <div className="step-actions">
        <XUIButton variant="borderless-main" onClick={onBack}>
          Back
        </XUIButton>
        <XUIButton
          variant="main"
          isDisabled={!selectedAccountId}
          onClick={onNext}
        >
          Continue
        </XUIButton>
      </div>
    </div>
  )
}

export default SettlementAccountStep
