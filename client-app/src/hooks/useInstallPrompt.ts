import { useSyncExternalStore } from 'react'
import { subscribeToInstallPrompt, getInstallPromptSnapshot, promptInstall } from '../utils/pwaInstallPrompt'

interface IUseInstallPromptResult {
  isInstallable: boolean
  isInstalled: boolean
  promptInstall: () => Promise<void>
}

// Thin React binding over the pwaInstallPrompt.ts singleton — the beforeinstallprompt/
// appinstalled listeners live there (registered once, globally) so this hook never attaches its
// own. Mounting/unmounting InstallAppButton only subscribes/unsubscribes this component to that
// shared state; it does not affect whether the deferred prompt was captured.
export function useInstallPrompt(): IUseInstallPromptResult {
  const { deferredPrompt, isInstalled } = useSyncExternalStore(subscribeToInstallPrompt, getInstallPromptSnapshot)

  return {
    isInstallable: deferredPrompt !== null && !isInstalled,
    isInstalled,
    promptInstall,
  }
}
