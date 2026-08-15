import { Button } from '../common/Button'
import { useInstallPrompt } from '../../hooks/useInstallPrompt'

export function InstallAppButton() {
  const { isInstallable, promptInstall } = useInstallPrompt()

  if (!isInstallable) {
    return null
  }

  return (
    <Button variant="outline-primary" onClick={promptInstall}>
      Install App
    </Button>
  )
}
