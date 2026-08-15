import type { ReactNode } from 'react'

interface IBrowserFrameProps {
  url?: string
  children: ReactNode
  className?: string
}

export function BrowserFrame({ url = 'app.apexbooking.com', children, className = '' }: IBrowserFrameProps) {
  return (
    <div className={`browser-frame ${className}`.trim()}>
      <div className="browser-frame__bar">
        <span className="browser-frame__dot" />
        <span className="browser-frame__dot" />
        <span className="browser-frame__dot" />
        <span className="browser-frame__url">{url}</span>
      </div>
      <div className="browser-frame__body">{children}</div>
    </div>
  )
}
