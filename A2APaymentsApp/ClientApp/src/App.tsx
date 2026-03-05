import { Routes, Route, Navigate } from 'react-router-dom'
import DemoLanding from './pages/DemoLanding'
import OnlinePaymentsSettings from './pages/OnlinePaymentsSettings'
import InvoiceDetail from './pages/InvoiceDetail'
import InvoiceView from './pages/InvoiceView'
import OnboardingWizardBalanced from './pages/OnboardingWizardBalanced'
import OnboardingWizardAggressive from './pages/OnboardingWizardAggressive'

function App() {
  return (
    <Routes>
      {/* Demo landing page - shows all entry points */}
      <Route path="/" element={<DemoLanding />} />
      
      {/* Entry Point 1: Online Payments Settings */}
      <Route path="/settings/online-payments" element={<OnlinePaymentsSettings />} />
      
      {/* Entry Point 2: Invoice detail (with payment options modal) */}
      <Route path="/invoice/:invoiceId" element={<InvoiceDetail />} />
      
      {/* Entry Point 3: Invoice view (with contextual banner) - NEW realistic entry */}
      <Route path="/invoice-view/:invoiceId" element={<InvoiceView />} />
      
      {/* Onboarding wizard - balanced flow (default) */}
      <Route path="/merchant-onboarding" element={<OnboardingWizardBalanced />} />
      
      {/* Onboarding wizard - aggressive flow (fast track) */}
      <Route path="/merchant-onboarding-aggressive" element={<OnboardingWizardAggressive />} />
      
      {/* Catch all */}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

export default App
