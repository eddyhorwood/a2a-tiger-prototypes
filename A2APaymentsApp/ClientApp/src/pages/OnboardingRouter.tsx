import { useConfigState } from '../config/PrototypeConfigContext'
import OnboardingWizardConservative from './OnboardingWizardNew' // Multi-step, 3-5 min
import OnboardingWizardBalanced from './OnboardingWizardAggressive' // Fast, ~30 sec
import OnboardingWizardAggressive from './OnboardingWizardAggressiveFast' // Ultra-fast, < 10 sec

/**
 * OnboardingRouter
 * 
 * Smart component that renders the appropriate onboarding wizard
 * based on the configured flow variant (aggressive / balanced / conservative).
 * 
 * This ensures the flow variant setting is respected regardless of
 * which entry point the user came from.
 */
function OnboardingRouter() {
  const { isAggressiveFlow, isBalancedFlow, isConservativeFlow } = useConfigState()
  
  if (isAggressiveFlow) {
    return <OnboardingWizardAggressive />
  }
  
  if (isBalancedFlow) {
    return <OnboardingWizardBalanced />
  }
  
  return <OnboardingWizardConservative />
}

export default OnboardingRouter
