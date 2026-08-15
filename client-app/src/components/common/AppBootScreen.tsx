import { LoadingSpinner } from './LoadingSpinner'

interface IAppBootScreenProps {
  label?: string
}

// Full-screen ApexBooking-branded loading state — reuses the same favicon mark AuthLayout
// already renders as the brand logo, and the existing LoadingSpinner rather than a new spinner
// implementation. Shared by PwaInit (installed-PWA cold boot) and both route guards so a session
// check never flashes a bare, unbranded spinner before redirecting.
export function AppBootScreen({ label = 'Loading...' }: IAppBootScreenProps) {
  return (
    <div className="d-flex flex-column justify-content-center align-items-center min-vh-100 gap-3">
      <img src="/favicon.svg" alt="ApexBooking" width={48} height={48} />
      <LoadingSpinner size="md" label={label} />
    </div>
  )
}
