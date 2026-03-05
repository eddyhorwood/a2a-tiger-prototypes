import './DefaultSettingsTab.css'

export function DefaultSettingsTab() {
  return (
    <div className="x-default-settings-tab">
      <div className="x-placeholder-state">
        <svg width="48" height="48" viewBox="0 0 24 24" fill="none" className="x-placeholder-icon">
          <path d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
        </svg>
        <h3 className="x-heading-md">Default settings configuration</h3>
        <p className="x-text-md x-text-muted">
          Global payment preferences and default behaviors will be configured here in a future iteration.
        </p>
      </div>
    </div>
  )
}
