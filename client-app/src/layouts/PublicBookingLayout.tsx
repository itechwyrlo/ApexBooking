import type { ReactNode } from 'react'
import { BookingProgressSteps } from '../components/publicBooking/BookingProgressSteps'

interface IPublicBookingLayoutProps {
  businessName?: string
  /** Reserved for a future tenant-uploaded banner image; falls back to the flat header background when absent. */
  bannerImageUrl?: string | null
  currentIndex: number
  total: number
  stepLabels: string[]
  showProgress: boolean
  contentMaxWidth?: number
  onStepClick?: (index: number) => void
  /** Persistent desktop-only summary panel shown beside the main content (hidden below `lg`). */
  sidePanel?: ReactNode
  children: ReactNode
}

export function PublicBookingLayout({
  businessName,
  bannerImageUrl,
  currentIndex,
  total,
  stepLabels,
  showProgress,
  contentMaxWidth = 560,
  onStepClick,
  sidePanel,
  children,
}: IPublicBookingLayoutProps) {
  return (
    <div className="pb-root d-flex flex-column">
      <header
        className="pb-header pb-header-banner px-3 px-md-4 py-3"
        style={bannerImageUrl ? { backgroundImage: `url(${bannerImageUrl})` } : undefined}
      >
        <div className="mx-auto" style={{ maxWidth: contentMaxWidth }}>
          <div className="d-flex align-items-center gap-2 mb-3">
            <img src="/favicon.svg" alt="" width={24} height={24} className="rounded-1 pb-brand-dot" aria-hidden="true" />
            <span className="fw-semibold small pb-brand">{businessName ?? 'Book an appointment'}</span>
          </div>
          {showProgress && (
            <BookingProgressSteps currentIndex={currentIndex} total={total} stepLabels={stepLabels} onStepClick={onStepClick} />
          )}
        </div>
      </header>

      <main className="flex-fill px-3 px-md-4 py-4 py-md-5">
        {sidePanel ? (
          <div className="mx-auto" style={{ maxWidth: 1100 }}>
            <div className="row g-4 g-lg-5">
              <div className="col-12 col-lg-7">{children}</div>
              <div className="col-lg-5 d-none d-lg-block">
                <div className="pb-summary-panel-sticky">{sidePanel}</div>
              </div>
            </div>
          </div>
        ) : (
          <div className="mx-auto" style={{ maxWidth: contentMaxWidth }}>
            {children}
          </div>
        )}
      </main>
    </div>
  )
}
