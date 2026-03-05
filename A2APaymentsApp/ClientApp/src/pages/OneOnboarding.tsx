import { useNavigate } from 'react-router-dom'
import XUIButton from '@xero/xui/react/button'
import { serializeEntryContext, type EntryContext } from '../types/EntryContext'
import './OneOnboarding.css'

interface OnboardingTask {
  id: string
  title: string
  description: string
  status: 'not-started' | 'in-progress' | 'completed'
  entryConfig?: EntryContext
}

function OneOnboarding() {
  const navigate = useNavigate()
  
  const tasks: OnboardingTask[] = [
    {
      id: 'add-contacts',
      title: 'Add your customers',
      description: 'Import or add customer contacts to get started',
      status: 'completed'
    },
    {
      id: 'create-invoice',
      title: 'Create your first invoice',
      description: 'Send professional invoices to your customers',
      status: 'completed'
    },
    {
      id: 'setup-payment',
      title: 'Accept direct bank payments',
      description: 'Turn on Pay by bank to let customers pay by direct bank transfer',
      status: 'not-started',
      entryConfig: {
        source: 'one_onboarding',
        mode: 'first_time',
        returnTo: '/onboarding',
        metadata: { taskId: 'setup-payment' }
      }
    },
    {
      id: 'connect-bank',
      title: 'Connect your bank',
      description: 'Automatically import bank transactions',
      status: 'not-started'
    }
  ]
  
  const handleTaskClick = (task: OnboardingTask) => {
    if (task.entryConfig) {
      const params = serializeEntryContext(task.entryConfig)
      navigate(`/merchant-onboarding?${params.toString()}`)
    }
  }
  
  const completedCount = tasks.filter(t => t.status === 'completed').length
  const progressPercentage = (completedCount / tasks.length) * 100
  
  return (
    <div className="one-onboarding">
      <div className="onboarding-header">
        <h1>Get started with Xero</h1>
        <p className="header-subtitle">
          Complete these steps to get the most out of Xero
        </p>
      </div>
      
      {/* Progress bar */}
      <div className="progress-section">
        <div className="progress-bar">
          <div
            className="progress-fill"
            style={{ width: `${progressPercentage}%` }}
          />
        </div>
        <p className="progress-text">
          {completedCount} of {tasks.length} completed
        </p>
      </div>
      
      {/* Tasks list */}
      <div className="tasks-list">
        {tasks.map((task) => (
          <div
            key={task.id}
            className={`task-item ${task.status}`}
          >
            <div className="task-icon">
              {task.status === 'completed' && <span className="icon-checkmark">✓</span>}
              {task.status === 'in-progress' && <span className="icon-progress">◐</span>}
              {task.status === 'not-started' && <span className="icon-todo">○</span>}
            </div>
            
            <div className="task-info">
              <h3>{task.title}</h3>
              <p>{task.description}</p>
            </div>
            
            <div className="task-action">
              {task.status === 'completed' && (
                <span className="completed-label">Complete</span>
              )}
              {task.status === 'not-started' && task.entryConfig && (
                <XUIButton
                  variant="main"
                  size="small"
                  onClick={() => handleTaskClick(task)}
                >
                  Set up
                </XUIButton>
              )}
              {task.status === 'not-started' && !task.entryConfig && (
                <XUIButton
                  variant="standard"
                  size="small"
                  disabled
                >
                  Coming soon
                </XUIButton>
              )}
            </div>
          </div>
        ))}
      </div>
      
      {/* Tips section */}
      <div className="tips-section">
        <h2>💡 Tip: Get paid faster</h2>
        <p>
          Customers who can pay online pay 2x faster than those who can't.
          Set up Pay by bank to give customers a secure, instant payment option.
        </p>
      </div>
      
      {/* Navigation hint */}
      <div className="nav-hint">
        <p>
          <a href="/" onClick={(e) => { e.preventDefault(); navigate('/') }}>
            ← Back to all entry points
          </a>
        </p>
      </div>
    </div>
  )
}

export default OneOnboarding
