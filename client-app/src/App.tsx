import { AuthProvider } from './contexts/AuthContext'
import { ToastProvider } from './contexts/ToastContext'
import { AppRoutes } from './routes/AppRoutes'
// Side-effect import: registers the global beforeinstallprompt/appinstalled listeners (see
// pwaInstallPrompt.ts) on every route, not just when the dashboard-only InstallAppButton mounts —
// otherwise the browser's automatic install UI would never be suppressed on public pages.
import './utils/pwaInstallPrompt'

function App() {
  return (
    <AuthProvider>
      <ToastProvider>
        <AppRoutes />
      </ToastProvider>
    </AuthProvider>
  )
}

export default App
