import type { ReactNode } from 'react'

interface IMobileNavProps {
  isOpen: boolean
  onClose: () => void
  children: ReactNode
}

export function MobileNav({ isOpen, onClose, children }: IMobileNavProps) {
  return (
    <div className="d-lg-none">
      <div
        className={`offcanvas offcanvas-start${isOpen ? ' show' : ''}`}
        style={{ visibility: isOpen ? 'visible' : 'hidden' }}
        tabIndex={-1}
        aria-hidden={!isOpen}
      >
        <div className="offcanvas-header">
          <h2 className="offcanvas-title fs-5">Menu</h2>
          <button type="button" className="btn-close" aria-label="Close" onClick={onClose} />
        </div>
        <div className="offcanvas-body p-0">{children}</div>
      </div>
      {isOpen && <div className="offcanvas-backdrop show" onClick={onClose} />}
    </div>
  )
}
