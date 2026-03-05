import { useConfigState } from '../config/PrototypeConfigContext'
import OnboardingWizardBalanced from './OnboardingWizardBalanced'
import OnboardingWizardAggressive from './OnboardingWizardAggressive'

/**
 * OnboardingRouter
 * 
 * Smart component that renders the appropriate onboarding wizard
 * based on the configured flow variant (balanced vs aggressive).
 * 
 * This ensures the flow variant setting is respected regardless of
 * which entry point the user came from.
 */
function OnboardingRouter() {
  const { isAggressiveFlow } = useConfigState()
  
  if (isAggressiveFlow) {
    return <OnboardingWizardAggressive />
  }
  
  return <OnboardingWizardBalanced />
}

export default OnboardingRouter
