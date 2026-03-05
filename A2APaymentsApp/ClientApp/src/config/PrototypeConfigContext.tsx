import { createContext, useContext, useState, useEffect, ReactNode } from 'react'
import { PrototypeConfig, DEFAULT_CONFIG } from '../types/PrototypeConfig'

/**
 * Context for managing prototype configuration state
 * 
 * Stores configuration in localStorage so it persists across page refreshes
 * during prototype testing sessions.
 */

interface PrototypeConfigContextValue {
  config: PrototypeConfig
  updateConfig: (updates: Partial<PrototypeConfig>) => void
  resetConfig: () => void
  loadPreset: (preset: PrototypeConfig) => void
}

const PrototypeConfigContext = createContext<PrototypeConfigContextValue | undefined>(undefined)

const STORAGE_KEY = 'a2a-prototype-config'

interface PrototypeConfigProviderProps {
  children: ReactNode
}

export function PrototypeConfigProvider({ children }: PrototypeConfigProviderProps) {
  const [config, setConfig] = useState<PrototypeConfig>(() => {
    // Load from localStorage on mount
    try {
      const stored = localStorage.getItem(STORAGE_KEY)
      if (stored) {
        return JSON.parse(stored) as PrototypeConfig
      }
    } catch (error) {
      console.warn('Failed to load prototype config from localStorage:', error)
    }
    return DEFAULT_CONFIG
  })

  // Persist to localStorage whenever config changes
  useEffect(() => {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(config))
    } catch (error) {
      console.warn('Failed to save prototype config to localStorage:', error)
    }
  }, [config])

  const updateConfig = (updates: Partial<PrototypeConfig>) => {
    setConfig(prev => ({ ...prev, ...updates }))
  }

  const resetConfig = () => {
    setConfig(DEFAULT_CONFIG)
  }

  const loadPreset = (preset: PrototypeConfig) => {
    setConfig(preset)
  }

  const value: PrototypeConfigContextValue = {
    config,
    updateConfig,
    resetConfig,
    loadPreset
  }

  return (
    <PrototypeConfigContext.Provider value={value}>
      {children}
    </PrototypeConfigContext.Provider>
  )
}

/**
 * Hook to access prototype configuration
 */
export function usePrototypeConfig(): PrototypeConfigContextValue {
  const context = useContext(PrototypeConfigContext)
  if (!context) {
    throw new Error('usePrototypeConfig must be used within PrototypeConfigProvider')
  }
  return context
}

/**
 * Hook to get derived state based on configuration
 * 
 * Use this to make decisions in components based on the active config
 */
export function useConfigState() {
  const { config } = usePrototypeConfig()
  
  return {
    // Eligibility checks
    isEligible: config.regionEligibility === 'eligible' && config.bankAccounts !== 'none',
    hasStripe: config.stripe !== 'none',
    isStripeActive: config.stripe === 'active',
    
    // Bank account state
    hasBankAccounts: config.bankAccounts !== 'none',
    hasMultipleBankAccounts: config.bankAccounts === 'multiple',
    
    // A2A state
    isA2AStarted: config.a2aStatus !== 'not_started',
    isA2AComplete: config.a2aStatus === 'complete_enabled' || config.a2aStatus === 'complete_disabled',
    isA2AEnabled: config.a2aStatus === 'complete_enabled',
    needsCompletion: config.a2aStatus === 'partially_complete',
    
    // Flow
    isAggressiveFlow: config.flowVariant === 'aggressive',
    
    // Business
    isTargetCohort: config.businessType === 'b2c_low', // Low Stripe win-rate = target for A2A
    
    // Full config
    config
  }
}
