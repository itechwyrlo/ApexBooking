import { useSyncExternalStore } from 'react'
import { subscribeToInstallPrompt, getInstallPromptSnapshot, promptInstall, isIosSafari } from '../utils/pwaInstallPrompt'

export type InstallMethod = 'native' | 'ios' | null

interface IUseInstallPromptResult {
  isInstallable: boolean
  isInstalled: boolean
  installMethod: InstallMethod
  promptInstall: () => Promise<void>
}

// Thin React binding over the pwaInstallPrompt.ts singleton — the beforeinstallprompt/
// appinstalled listeners live there (registered once, globally) so this hook never attaches its
// own. Mounting/unmounting InstallAppButton only subscribes/unsubscribes this component to that
// shared state; it does not affect whether the deferred prompt was captured.
//
// installMethod distinguishes the two install affordances: 'native' hands off to the captured
// beforeinstallprompt event via promptInstall(), 'ios' means there's no such event to defer to
// (Safari/iOS never fires it) and the caller must show its own "Add to Home Screen" instructions.
export function useInstallPrompt(): IUseInstallPromptResult {
  const { deferredPrompt, isInstalled } = useSyncExternalStore(subscribeToInstallPrompt, getInstallPromptSnapshot)

  const installMethod: InstallMethod = isInstalled
    ? null
    : deferredPrompt !== null
      ? 'native'
      : isIosSafari()
        ? 'ios'
        : null

  return {
    isInstallable: installMethod !== null,
    isInstalled,
    installMethod,
    promptInstall,
  }
}
