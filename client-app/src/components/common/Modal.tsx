import { useEffect, useRef, useState, type ReactNode } from 'react'

type ModalSize = 'sm' | 'md' | 'lg'

const SIZE_CLASSES: Record<ModalSize, string> = {
  sm: 'modal-sm',
  md: '',
  lg: 'modal-lg',
}

const CLOSE_ANIMATION_MS = 200

// Every standard interactive element a keyboard user could reach — deliberately broader than the
// initial-focus effect's own selector below (that one skips .btn-close on purpose, so opening a
// modal doesn't land focus on the X before any real field); the trap needs every focusable element
// including .btn-close and the footer's buttons, since Tab should reach all of them.
const FOCUSABLE_SELECTOR =
  'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'

interface IModalProps {
  isOpen: boolean
  title: string
  description?: string
  size?: ModalSize
  footer?: ReactNode
  closeOnBackdrop?: boolean
  onClose: () => void
  children: ReactNode
}

export function Modal({
  isOpen,
  title,
  description,
  size = 'md',
  footer,
  closeOnBackdrop = true,
  onClose,
  children,
}: IModalProps) {
  const [isMounted, setIsMounted] = useState(isOpen)
  const [isVisible, setIsVisible] = useState(false)
  const [isBodyScrolled, setIsBodyScrolled] = useState(false)
  const dialogRef = useRef<HTMLDivElement>(null)
  const triggerRef = useRef<HTMLElement | null>(null)

  useEffect(() => {
    if (isOpen) {
      triggerRef.current = document.activeElement as HTMLElement
      setIsMounted(true)
      setIsBodyScrolled(false)
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
      const focusTarget = dialogRef.current?.querySelector<HTMLElement>(
        'input, select, textarea, button:not(.btn-close), [tabindex]:not([tabindex="-1"])',
      )
      ;(focusTarget ?? dialogRef.current)?.focus()
    } else {
      triggerRef.current?.focus()
    }
  }, [isVisible, isMounted])

  useEffect(() => {
    if (!isOpen) return

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        onClose()
        return
      }

      // Focus trap: wrap Tab/Shift+Tab at the dialog's own first/last focusable element instead
      // of letting focus escape into the (still visible, backdrop-obscured) page behind it.
      if (event.key === 'Tab') {
        const focusable = dialogRef.current?.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR)
        if (!focusable || focusable.length === 0) return

        const first = focusable[0]
        const last = focusable[focusable.length - 1]

        if (event.shiftKey && document.activeElement === first) {
          event.preventDefault()
          last.focus()
        } else if (!event.shiftKey && document.activeElement === last) {
          event.preventDefault()
          first.focus()
        }
      }
    }

    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [isOpen, onClose])

  if (!isMounted) return null

  return (
    <>
      <div className="modal d-block" tabIndex={-1} role="dialog" aria-modal="true" aria-label={title}>
        <div
          ref={dialogRef}
          className={`modal-dialog modal-dialog-centered modal-dialog-scrollable ${SIZE_CLASSES[size]} modal-dialog-transition ${isVisible ? 'is-visible' : ''}`.trim()}
        >
          <div className="modal-content">
            <div className={`modal-header ${isBodyScrolled ? 'modal-header-scrolled' : ''}`.trim()}>
              <div>
                <h2 className="modal-title fs-5">{title}</h2>
                {description && <p className="text-muted small mb-0 mt-1">{description}</p>}
              </div>
              <button type="button" className="btn-close" aria-label="Close" onClick={onClose} />
            </div>
            <div className="modal-body" onScroll={(event) => setIsBodyScrolled(event.currentTarget.scrollTop > 0)}>
              {children}
            </div>
            {footer && <div className="modal-footer">{footer}</div>}
          </div>
        </div>
      </div>
      <div
        className={`modal-backdrop modal-backdrop-transition ${isVisible ? 'is-visible' : ''}`.trim()}
        onClick={closeOnBackdrop ? onClose : undefined}
      />
    </>
  )
}
