import React from 'react'
import './xui.css'

interface XUICompositionDetailProps {
  detail: React.ReactNode
  hasAutoSpaceAround?: boolean
}

export default function XUICompositionDetail({ detail, hasAutoSpaceAround = true }: XUICompositionDetailProps) {
  return (
    <div className={`xui-composition-detail ${hasAutoSpaceAround ? 'xui-composition-detail--auto-space' : ''}`}>
      {detail}
    </div>
  )
}
