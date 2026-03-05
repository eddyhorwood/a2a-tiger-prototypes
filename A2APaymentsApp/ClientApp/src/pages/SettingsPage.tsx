import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import XUIButton from '@xero/xui/react/button'
import { api, A2AConfig, BankAccount } from '../services/api'
import './SettingsPage.css'

function SettingsPage() {
  const navigate = useNavigate()
  const [config, setConfig] = useState<A2AConfig | null>(null)
  const [accounts, setAccounts] = useState<BankAccount[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    loadData()
  }, [])

  const loadData = async () => {
    setLoading(true)
    try {
      const [configData, accountsData] = await Promise.all([
        api.getConfig(),
        api.getEligibleAccounts()
      ])
      setConfig(configData)
      setAccounts(accountsData)
    } catch (error) {
      console.error('Failed to load settings:', error)
    } finally {
      setLoading(false)
    }
  }

  const handleEnableClick = () => {
    navigate('/merchant-onboarding')
  }

  const handleDisable = async () => {
    if (!confirm('Are you sure you want to disable Pay by bank? This will remove it as an option on new online invoices.')) {
      return
    }

    try {
      await api.updateConfig({
        enabled: false,
        settlement_account_id: null
      })
      await loadData()
    } catch (error) {
      console.error('Failed to disable:', error)
    }
  }

  if (loading) {
    return (
      <div className="settings-page">
        <div className="xui-page-width-large">
          <p>Loading...</p>
        </div>
      </div>
    )
  }

  const settlementAccount = accounts.find(a => a.accountId === config?.settlement_account_id)

  return (
    <div className="settings-page">
      <div className="xui-page-width-large">
        <h1 className="xui-heading-xlarge xui-margin-bottom-large">Online payments</h1>
        
        <section className="xui-margin-bottom-xlarge">
          <h2 className="xui-heading-large xui-margin-bottom">Bank payments</h2>
          
          <article className="payment-service-tile">
            <div className="tile-header">
              <h3 className="xui-heading-medium">Pay by bank (Safer A2A)</h3>
              {config?.enabled && (
                <span className="status-pill status-pill--active">Enabled</span>
              )}
            </div>

            {!config?.enabled ? (
              <>
                <p className="tile-description xui-margin-vertical">
                  Let customers pay invoices by secure direct bank transfer, initiated 
                  via Akahu. Funds move directly between bank accounts; Xero and Akahu 
                  don't hold your money.
                </p>
                <XUIButton variant="main" onClick={handleEnableClick}>
                  Enable Pay by bank
                </XUIButton>
              </>
            ) : (
              <>
                <dl className="tile-details xui-margin-vertical">
                  <dt className="xui-text-deemphasis">Deposits to:</dt>
                  <dd className="xui-heading-small">{settlementAccount?.name || 'Unknown account'}</dd>
                  {settlementAccount?.accountNumber && (
                    <dd className="xui-text-deemphasis xui-font-size-small account-number">
                      {settlementAccount.accountNumber}
                    </dd>
                  )}
                </dl>
                <div className="tile-actions">
                  <XUIButton variant="borderless-main" onClick={handleEnableClick}>
                    Change settlement account
                  </XUIButton>
                  <XUIButton variant="borderless-negative" onClick={handleDisable}>
                    Disable Pay by bank
                  </XUIButton>
                </div>
              </>
            )}
          </article>
        </section>
      </div>
    </div>
  )
}

export default SettingsPage
