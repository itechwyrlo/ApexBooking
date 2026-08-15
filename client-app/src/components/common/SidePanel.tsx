import { useEffect, useRef, useState, type ReactNode } from 'react'

const CLOSE_ANIMATION_MS = 220

interface ISidePanelProps {
  isOpen: boolean
  title: string
  description?: string
  onClose: () => void
  children: ReactNode
}

export function SidePanel({ isOpen, title, description, onClose, children }: ISidePanelProps) {
  const [isMounted, setIsMounted] = useState(isOpen)
  const [isVisible, setIsVisible] = useState(false)
  const panelRef = useRef<HTMLDivElement>(null)
  const triggerRef = useRef<HTMLElement | null>(null)

  useEffect(() => {
    if (isOpen) {
      triggerRef.current = document.activeElement as HTMLElement
      setIsMounted(true)
      const frame = requestAnimationFrame(() => setIsVisible(true))
      return () => cancelAnimationFrame(frame)
    }

    setIsVisible(false)
    const timeout = setTimeout(() => setIsMounted(false), CLOSE_ANIMATION_MS)
    return () => clearTimeout(timeout)
  }, [isOpen])

  useEffect(() => {
    if (!isMounted) return

    if (isVisible) {
      const focusTarget = panelRef.current?.querySelector<HTMLElement>('button:not(.btn-close), a, [tabindex]:not([tabindex="-1"])')
      ;(focusTarget ?? panelRef.current)?.focus()
    } else {
      triggerRef.current?.focus()
    }
  }, [isVisible, isMounted])

  useEffect(() => {
    if (!isOpen) return

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        onClose()
      }
    }

    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [isOpen, onClose])

  if (!isMounted) return null

  return (
    <>
      <div
        ref={panelRef}
        className={`position-fixed top-0 end-0 h-100 bg-white shadow-lg d-flex flex-column side-panel-transition ${isVisible ? 'is-visible' : ''}`.trim()}
        style={{ width: 'min(420px, 100vw)', zIndex: 1050 }}
        role="dialog"
        aria-modal="true"
        aria-label={title}
      >
        <div className="d-flex align-items-start justify-content-between p-3 border-bottom">
          <div>
            <h2 className="fs-5 fw-bold mb-0">{title}</h2>
            {description && <p className="text-muted small mb-0 mt-1">{description}</p>}
          </div>
          <button type="button" className="btn-close" aria-label="Close" onClick={onClose} />
        </div>
        <div className="p-3 overflow-auto flex-grow-1">{children}</div>
      </div>
      <div
        className={`side-panel-backdrop-transition ${isVisible ? 'is-visible' : ''}`.trim()}
        style={{ position: 'fixed', inset: 0, backgroundColor: '#000', zIndex: 1040 }}
        onClick={onClose}
      />
    </>
  )
}
