import React from 'react'
import './xui.css'

interface XUIStepperTab {
  name: string
  isDisabled: boolean
  wizardPage: number
}

interface XUIStepperProps {
  currentStep: number
  id: string
  tabs: XUIStepperTab[]
  updateCurrentStep: (index: number) => void
}

export default function XUIStepper({ currentStep, tabs, updateCurrentStep }: XUIStepperProps) {
  return (
    <div className="xui-stepper">
      <div className="xui-stepper-container">
        {tabs.map((tab, index) => (
          <div
            key={index}
            className={`xui-stepper-step ${index === currentStep ? 'xui-stepper-step--active' : ''} ${index < currentStep ? 'xui-stepper-step--completed' : ''} ${tab.isDisabled ? 'xui-stepper-step--disabled' : ''}`}
            onClick={() => !tab.isDisabled && updateCurrentStep(index)}
            style={{ cursor: tab.isDisabled ? 'default' : 'pointer' }}
          >
            <div className="xui-stepper-step-indicator">
              {index < currentStep ? '✓' : index + 1}
            </div>
            <div className="xui-stepper-step-label">{tab.name}</div>
          </div>
        ))}
      </div>
    </div>
  )
}
