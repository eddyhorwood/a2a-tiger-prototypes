import { useEffect } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { serializeEntryContext, type EntryContext } from '../types/EntryContext'
import './CampaignEntry.css'

/**
 * Entry point 6: External campaign / email → in-product deep link
 * 
 * Handles deep links from:
 * - Marketing emails
 * - Xero Central CTAs
 * - Campaign landing pages
 * 
 * Example URLs:
 * - /campaign-entry?campaign=email_nz_a2a_launch
 * - /campaign-entry?campaign=central_article&article=pay-by-bank
 * - /campaign-entry?utm_source=campaignX&utm_medium=email
 */
function CampaignEntry() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  
  useEffect(() => {
    // In a real app, this would:
    // 1. Authenticate user (redirect to login if needed)
    // 2. Handle org switching if orgId param present
    // 3. Check eligibility (show explanation if ineligible)
    // 4. Track campaign source for analytics
    
    // For demo: immediately route to onboarding after brief loading
    const timer = setTimeout(() => {
      const context: EntryContext = {
        source: 'campaign',
        mode: 'first_time',
        returnTo: '/settings/online-payments',
        metadata: {
          campaignId: searchParams.get('campaign') || searchParams.get('utm_campaign') || undefined,
          utmSource: searchParams.get('utm_source') || undefined,
          utmMedium: searchParams.get('utm_medium') || undefined,
          utmContent: searchParams.get('utm_content') || undefined
        }
      }
      
      const params = serializeEntryContext(context)
      navigate(`/merchant-onboarding?${params.toString()}`)
    }, 1500)
    
    return () => clearTimeout(timer)
  }, [navigate, searchParams])
  
  return (
    <div className="campaign-entry">
      <div className="loading-container">
        <div className="xero-logo">
          <div className="logo-circle"></div>
        </div>
        <h1>Setting up Pay by bank...</h1>
        <p>We're taking you to your settings</p>
        <div className="loading-spinner"></div>
      </div>
      
      <div className="campaign-info">
        <p className="info-text">
          Campaign: {searchParams.get('campaign') || searchParams.get('utm_campaign') || 'Direct link'}
        </p>
      </div>
    </div>
  )
}

export default CampaignEntry
