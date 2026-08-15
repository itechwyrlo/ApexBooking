import { useState } from 'react'
import { Button } from '../common/Button'
import { Modal } from '../common/Modal'
import { useInstallPrompt } from '../../hooks/useInstallPrompt'

export function InstallAppButton() {
  const { isInstallable, installMethod, promptInstall } = useInstallPrompt()
  const [showIosInstructions, setShowIosInstructions] = useState(false)

  if (!isInstallable) {
    return null
  }

  const handleClick = installMethod === 'ios' ? () => setShowIosInstructions(true) : promptInstall

  return (
    <>
      <Button variant="outline-primary" onClick={handleClick}>
        Install App
      </Button>
      <Modal
        isOpen={showIosInstructions}
        title="Install ApexBooking"
        description="Add ApexBooking to your Home Screen for quick, full-screen access."
        onClose={() => setShowIosInstructions(false)}
      >
        <ol className="ps-3 mb-0">
          <li className="mb-2">Tap the Share button in Safari&rsquo;s toolbar.</li>
          <li className="mb-2">Scroll down and tap &ldquo;Add to Home Screen&rdquo;.</li>
          <li>Tap &ldquo;Add&rdquo; in the top-right corner to confirm.</li>
        </ol>
      </Modal>
    </>
  )
}
