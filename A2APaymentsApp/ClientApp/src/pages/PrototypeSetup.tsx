import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import XUIButton from '@xero/xui/react/button'
import { usePrototypeConfig } from '../config/PrototypeConfigContext'
import {
  PrototypeConfig,
  StripeStatus,
  BankAccountSetup,
  A2AOnboardingStatus,
  FlowVariant,
  BusinessType,
  RegionEligibility,
  CONFIG_LABELS,
  PRESET_SCENARIOS
} from '../types/PrototypeConfig'
import './PrototypeSetup.css'

/**
 * Prototype Setup Page
 * 
 * Landing page that lets you configure the initial state of the prototype
 * before entering any onboarding flows. Choose merchant state, Stripe status,
 * bank accounts, etc. to test different scenarios.
 */
function PrototypeSetup() {
  const navigate = useNavigate()
  const { config, updateConfig, loadPreset } = usePrototypeConfig()
  const [showPresets, setShowPresets] = useState(false)

  const handlePresetClick = (presetConfig: PrototypeConfig) => {
    loadPreset(presetConfig)
    setShowPresets(false)
  }

  const handleStartPrototype = () => {
    navigate('/demo')
  }

  return (
    <div className="x-prototype-setup">
      <div className="x-prototype-setup__container">
        {/* Header */}
        <div className="x-prototype-setup__header">
          <h1 className="x-heading-xxl">A2A Prototype Configuration</h1>
          <p className="x-text-lg x-text-muted">
            Configure the initial state for your prototype session. Choose merchant characteristics,
            payment provider status, and onboarding stage to test different scenarios.
          </p>
        </div>

        {/* Preset Scenarios */}
        <div className="x-prototype-setup__presets-section">
          <div className="x-prototype-setup__presets-header">
            <h2 className="x-heading-lg">Quick Start Presets</h2>
            <XUIButton
              variant="standard"
              size="small"
              onClick={() => setShowPresets(!showPresets)}
            >
              {showPresets ? 'Hide Presets' : 'Show Presets'}
            </XUIButton>
          </div>

          {showPresets && (
            <div className="x-prototype-setup__presets-grid">
              {Object.entries(PRESET_SCENARIOS).map(([key, preset]) => (
                <div key={key} className="x-preset-card">
                  <div className="x-preset-card__header">
                    <h3 className="x-heading-md">{preset.name}</h3>
                  </div>
                  <p className="x-text-sm x-text-muted x-preset-card__description">
                    {preset.description}
                  </p>
                  <div className="x-preset-card__specs">
                    <div className="x-preset-spec">
                      <span className="x-preset-spec__label">Stripe:</span>
                      <span className="x-preset-spec__value">{CONFIG_LABELS.stripe[preset.config.stripe]}</span>
                    </div>
                    <div className="x-preset-spec">
                      <span className="x-preset-spec__label">Bank Accounts:</span>
                      <span className="x-preset-spec__value">{CONFIG_LABELS.bankAccounts[preset.config.bankAccounts]}</span>
                    </div>
                    <div className="x-preset-spec">
                      <span className="x-preset-spec__label">A2A Status:</span>
                      <span className="x-preset-spec__value">{CONFIG_LABELS.a2aStatus[preset.config.a2aStatus]}</span>
                    </div>
                  </div>
                  <XUIButton
                    variant="standard"
                    size="small"
                    onClick={() => handlePresetClick(preset.config)}
                  >
                    Load Preset
                  </XUIButton>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Manual Configuration */}
        <div className="x-prototype-setup__config-section">
          <h2 className="x-heading-lg">Custom Configuration</h2>
          <p className="x-text-md x-text-muted" style={{ marginBottom: '24px' }}>
            Fine-tune the prototype state to test specific scenarios
          </p>

          <div className="x-prototype-setup__config-grid">
            {/* Stripe Status */}
            <div className="x-config-group">
              <label className="x-config-group__label">Stripe Status</label>
              <p className="x-text-sm x-text-muted x-config-group__hint">
                Whether Stripe is connected and how actively it's used
              </p>
              <div className="x-config-group__options">
                {(Object.keys(CONFIG_LABELS.stripe) as StripeStatus[]).map(option => (
                  <label key={option} className="x-config-option">
                    <input
                      type="radio"
                      name="stripe"
                      value={option}
                      checked={config.stripe === option}
                      onChange={() => updateConfig({ stripe: option })}
                    />
                    <span className="x-config-option__label">{CONFIG_LABELS.stripe[option]}</span>
                  </label>
                ))}
              </div>
            </div>

            {/* Bank Accounts */}
            <div className="x-config-group">
              <label className="x-config-group__label">Bank Accounts in Xero</label>
              <p className="x-text-sm x-text-muted x-config-group__hint">
                Number of eligible NZ bank accounts connected
              </p>
              <div className="x-config-group__options">
                {(Object.keys(CONFIG_LABELS.bankAccounts) as BankAccountSetup[]).map(option => (
                  <label key={option} className="x-config-option">
                    <input
                      type="radio"
                      name="bankAccounts"
                      value={option}
                      checked={config.bankAccounts === option}
                      onChange={() => updateConfig({ bankAccounts: option })}
                    />
                    <span className="x-config-option__label">{CONFIG_LABELS.bankAccounts[option]}</span>
                  </label>
                ))}
              </div>
            </div>

            {/* A2A Onboarding Status */}
            <div className="x-config-group">
              <label className="x-config-group__label">A2A Onboarding Status</label>
              <p className="x-text-sm x-text-muted x-config-group__hint">
                How far the merchant has progressed through A2A setup
              </p>
              <div className="x-config-group__options">
                {(Object.keys(CONFIG_LABELS.a2aStatus) as A2AOnboardingStatus[]).map(option => (
                  <label key={option} className="x-config-option">
                    <input
                      type="radio"
                      name="a2aStatus"
                      value={option}
                      checked={config.a2aStatus === option}
                      onChange={() => updateConfig({ a2aStatus: option })}
                    />
                    <span className="x-config-option__label">{CONFIG_LABELS.a2aStatus[option]}</span>
                  </label>
                ))}
              </div>
            </div>

            {/* Flow Variant */}
            <div className="x-config-group">
              <label className="x-config-group__label">Onboarding Flow Variant</label>
              <p className="x-text-sm x-text-muted x-config-group__hint">
                Test different UX approaches to onboarding
              </p>
              <div className="x-config-group__options">
                {(Object.keys(CONFIG_LABELS.flowVariant) as FlowVariant[]).map(option => (
                  <label key={option} className="x-config-option">
                    <input
                      type="radio"
                      name="flowVariant"
                      value={option}
                      checked={config.flowVariant === option}
                      onChange={() => updateConfig({ flowVariant: option })}
                    />
                    <span className="x-config-option__label">{CONFIG_LABELS.flowVariant[option]}</span>
                  </label>
                ))}
              </div>
            </div>

            {/* Business Type */}
            <div className="x-config-group">
              <label className="x-config-group__label">Business Type</label>
              <p className="x-text-sm x-text-muted x-config-group__hint">
                Customer segment for testing
              </p>
              <div className="x-config-group__options">
                {(Object.keys(CONFIG_LABELS.businessType) as BusinessType[]).map(option => (
                  <label key={option} className="x-config-option">
                    <input
                      type="radio"
                      name="businessType"
                      value={option}
                      checked={config.businessType === option}
                      onChange={() => updateConfig({ businessType: option })}
                    />
                    <span className="x-config-option__label">{CONFIG_LABELS.businessType[option]}</span>
                  </label>
                ))}
              </div>
            </div>

            {/* Region Eligibility */}
            <div className="x-config-group">
              <label className="x-config-group__label">Region Eligibility</label>
              <p className="x-text-sm x-text-muted x-config-group__hint">
                Test error states for ineligible organizations
              </p>
              <div className="x-config-group__options">
                {(Object.keys(CONFIG_LABELS.regionEligibility) as RegionEligibility[]).map(option => (
                  <label key={option} className="x-config-option">
                    <input
                      type="radio"
                      name="regionEligibility"
                      value={option}
                      checked={config.regionEligibility === option}
                      onChange={() => updateConfig({ regionEligibility: option })}
                    />
                    <span className="x-config-option__label">{CONFIG_LABELS.regionEligibility[option]}</span>
                  </label>
                ))}
              </div>
            </div>
          </div>
        </div>

        {/* Actions */}
        <div className="x-prototype-setup__actions">
          <XUIButton
            variant="main"
            size="medium"
            onClick={handleStartPrototype}
          >
            Start Prototype with This Configuration
          </XUIButton>
        </div>
      </div>
    </div>
  )
}

export default PrototypeSetup
