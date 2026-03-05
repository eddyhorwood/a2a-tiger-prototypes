import React from 'react'
import './xui.css'

interface XUILoaderProps {
  ariaLabel?: string
  size?: 'small' | 'medium' | 'large'
}

export default function XUILoader({ ariaLabel = 'Loading', size = 'medium' }: XUILoaderProps) {
  return (
    <div className={`xui-loader xui-loader--${size}`} aria-label={ariaLabel}>
      <div className="xui-loader-spinner"></div>
    </div>
  )
}
