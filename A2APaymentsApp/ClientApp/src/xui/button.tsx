import React from 'react'
import './xui.css'

interface XUIButtonProps {
  variant?: 'primary' | 'secondary' | 'borderless-main' | 'borderless-destructive'
  onClick?: () => void
  disabled?: boolean
  children: React.ReactNode
  className?: string
}

export default function XUIButton({ 
  variant = 'primary', 
  onClick, 
  disabled = false, 
  children,
  className = ''
}: XUIButtonProps) {
  return (
    <button
      className={`xui-button xui-button--${variant} ${className}`}
      onClick={onClick}
      disabled={disabled}
    >
      {children}
    </button>
  )
}
