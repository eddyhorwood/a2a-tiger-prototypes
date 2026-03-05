import { ReactNode } from 'react'
import XUIButton from '@xero/xui/react/button'
import './SetupBanner.css'

type BannerVariant = 'info' | 'promotional' | 'high-contrast'

interface SetupBannerProps {
  variant?: BannerVariant
  title: string
  description?: string
  icon?: ReactNode
  primaryAction: {
    label: string
    onClick: () => void
  }
  secondaryAction?: {
    label: string
    onClick: () => void
  }
  onDismiss?: () => void
}

/**
 * SetupBanner - Contextual banner for prompting payment setup
 * 
 * Used across multiple entry points:
 * - Invoice view/edit: "Set up new payment options"
 * - Invoice list: contextual nudges
 * - Repeating invoices: "Add online payments"
 * 
 * Variants:
 * - info: Blue, standard informational banner
 * - promotional: Purple/gradient, promotional campaigns
 * - high-contrast: Yellow/orange, urgent attention (overdue, high-value)
 */
export function SetupBanner({
  variant = 'info',
  title,
  description,
  icon,
  primaryAction,
  secondaryAction,
  onDismiss
}: SetupBannerProps) {
  return (
    <div className={`x-setup-banner x-setup-banner--${variant}`}>
      <div className="x-setup-banner__content">
        {icon && (
          <div className="x-setup-banner__icon">
            {icon}
          </div>
        )}
        
        <div className="x-setup-banner__text">
          <h3 className="x-setup-banner__title">{title}</h3>
          {description && (
            <p className="x-setup-banner__description">{description}</p>
          )}
        </div>
      </div>
      
      <div className="x-setup-banner__actions">
        <XUIButton
          variant={variant === 'high-contrast' ? 'main' : 'standard'}
          onClick={primaryAction.onClick}
        >
          {primaryAction.label}
        </XUIButton>
        
        {secondaryAction && (
          <XUIButton
            variant="borderless-standard"
            onClick={secondaryAction.onClick}
          >
            {secondaryAction.label}
          </XUIButton>
        )}
        
        {onDismiss && (
          <button
            className="x-setup-banner__dismiss"
            onClick={onDismiss}
            aria-label="Dismiss banner"
          >
            ×
          </button>
        )}
      </div>
    </div>
  )
}
