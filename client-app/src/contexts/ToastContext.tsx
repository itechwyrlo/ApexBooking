import { createContext, useCallback, useContext, useState, type ReactNode } from 'react'

export type ToastVariant = 'success' | 'error' | 'warning' | 'info'

interface IToastState {
  id: number
  variant: ToastVariant
  message: string
}

interface IToastContextValue {
  showToast: (variant: ToastVariant, message: string) => void
}

const ToastContext = createContext<IToastContextValue | undefined>(undefined)

const AUTO_DISMISS_MS = 5000

const VARIANT_CLASSES: Record<ToastVariant, string> = {
  success: 'text-bg-success',
  error: 'text-bg-danger',
  warning: 'text-bg-warning',
  info: 'text-bg-info',
}

interface IToastProviderProps {
  children: ReactNode
}

export function ToastProvider({ children }: IToastProviderProps) {
  const [toast, setToast] = useState<IToastState | null>(null)

  const showToast = useCallback((variant: ToastVariant, message: string) => {
    const id = Date.now()
    setToast({ id, variant, message })
    window.setTimeout(() => {
      setToast((current) => (current?.id === id ? null : current))
    }, AUTO_DISMISS_MS)
  }, [])

  return (
    <ToastContext.Provider value={{ showToast }}>
      {children}
      <div className="toast-container position-fixed top-0 end-0 p-3" style={{ zIndex: 1080 }}>
        {toast && (
          <div className={`toast show ${VARIANT_CLASSES[toast.variant]}`} role="alert" aria-live="assertive" aria-atomic="true">
            <div className="d-flex">
              <div className="toast-body">{toast.message}</div>
              <button
                type="button"
                className="btn-close btn-close-white me-2 m-auto"
                aria-label="Close"
                onClick={() => setToast(null)}
              />
            </div>
          </div>
        )}
      </div>
    </ToastContext.Provider>
  )
}

export function useToast(): IToastContextValue {
  const context = useContext(ToastContext)
  if (!context) {
    throw new Error('useToast must be used within a ToastProvider')
  }
  return context
}
