// Module-level singleton — registers its beforeinstallprompt/appinstalled listeners exactly
// once, at import time, regardless of which route triggered the import. This is what lets us
// call preventDefault() on every page (suppressing the browser's automatic install UI on public
// marketing pages) while still handing the deferred prompt to the dashboard-only InstallAppButton
// when the user is on an authenticated tenant route. useInstallPrompt.ts is a thin React
// subscriber over this store; it must never add its own window listeners, or the prompt would
// have two owners racing to call .prompt() on the same event.
export interface IBeforeInstallPromptEvent extends Event {
  prompt: () => Promise<void>
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed' }>
}

function isRunningStandalone(): boolean {
  return window.matchMedia('(display-mode: standalone)').matches
}

interface IInstallPromptState {
  deferredPrompt: IBeforeInstallPromptEvent | null
  isInstalled: boolean
}

let state: IInstallPromptState = {
  deferredPrompt: null,
  isInstalled: isRunningStandalone(),
}

const listeners = new Set<() => void>()

function setState(next: IInstallPromptState): void {
  state = next
  listeners.forEach((listener) => listener())
}

window.addEventListener('beforeinstallprompt', (event) => {
  event.preventDefault()
  setState({ ...state, deferredPrompt: event as IBeforeInstallPromptEvent })
})

window.addEventListener('appinstalled', () => {
  setState({ deferredPrompt: null, isInstalled: true })
})

export function subscribeToInstallPrompt(onChange: () => void): () => void {
  listeners.add(onChange)
  return () => listeners.delete(onChange)
}

export function getInstallPromptSnapshot(): IInstallPromptState {
  return state
}

export async function promptInstall(): Promise<void> {
  const { deferredPrompt } = state
  if (!deferredPrompt) {
    return
  }
  await deferredPrompt.prompt()
  await deferredPrompt.userChoice
  setState({ ...state, deferredPrompt: null })
}
