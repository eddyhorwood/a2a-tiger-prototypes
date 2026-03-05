import { Routes, Route, Navigate } from 'react-router-dom'
import PrototypeSetup from './pages/PrototypeSetup'
import DemoLanding from './pages/DemoLanding'
import OnlinePaymentsSettings from './pages/OnlinePaymentsSettings'
import InvoiceView from './pages/InvoiceView'
import OnboardingRouter from './pages/OnboardingRouter'
import OnboardingWizardAggressiveFast from './pages/OnboardingWizardAggressiveFast'

function App() {
  return (
    <Routes>
      {/* Prototype configuration landing page */}
      <Route path="/" element={<PrototypeSetup />} />
      
      {/* Demo landing page - shows all entry points */}
      <Route path="/demo" element={<DemoLanding />} />
      
      {/* Entry Point 1: Online Payments Settings */}
      <Route path="/settings/online-payments" element={<OnlinePaymentsSettings />} />
      
      {/* Entry Point 2: Invoice - unified page with banner + OPMM modal */}
      <Route path="/invoice/:invoiceId" element={<InvoiceView />} />
      
      {/* Onboarding wizard - smart router that respects config flowVariant */}
      <Route path="/merchant-onboarding" element={<OnboardingRouter />} />
      
      {/* Onboarding wizard - aggressive flow (direct route for testing) */}
      <Route path="/merchant-onboarding-aggressive" element={<OnboardingWizardAggressiveFast />} />
      
      {/* Catch all */}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

export default App
