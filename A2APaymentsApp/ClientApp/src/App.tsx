import { Routes, Route, Navigate } from 'react-router-dom'
import OnlinePaymentsSettings from './pages/OnlinePaymentsSettings'
import InvoiceDetail from './pages/InvoiceDetail'
import OnboardingWizardBalanced from './pages/OnboardingWizardBalanced'
import OnboardingWizardAggressive from './pages/OnboardingWizardAggressive'

function App() {
  return (
    <Routes>
      {/* Main demo entry - settings page */}
      <Route path="/" element={<Navigate to="/settings/online-payments" replace />} />
      
      {/* Entry Point 1: Online Payments Settings */}
      <Route path="/settings/online-payments" element={<OnlinePaymentsSettings />} />
      
      {/* Entry Point 2: Invoice detail (with inline payment prompt) */}
      <Route path="/invoice/:invoiceId" element={<InvoiceDetail />} />
      
      {/* Onboarding wizard - balanced flow (default) */}
      <Route path="/merchant-onboarding" element={<OnboardingWizardBalanced />} />
      
      {/* Onboarding wizard - aggressive flow (fast track) */}
      <Route path="/merchant-onboarding-aggressive" element={<OnboardingWizardAggressive />} />
      
      {/* Catch all */}
      <Route path="*" element={<Navigate to="/settings/online-payments" replace />} />
    </Routes>
  )
}

export default App
