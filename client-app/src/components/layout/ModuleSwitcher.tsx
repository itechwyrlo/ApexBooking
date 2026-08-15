import { MODULES } from '../../config/modules.config'

interface IModuleSwitcherProps {
  isCollapsed?: boolean
}

export function ModuleSwitcher({ isCollapsed }: IModuleSwitcherProps) {
  if (MODULES.length <= 1) {
    const activeModule = MODULES[0]

    return (
      <div className={`d-flex align-items-center gap-2 px-1${isCollapsed ? ' justify-content-center' : ''}`}>
        <img
          src="/favicon.svg"
          alt={isCollapsed ? 'ApexBooking' : ''}
          aria-hidden={isCollapsed ? undefined : true}
          width={28}
          height={28}
          className="rounded-2 flex-shrink-0"
        />
        {!isCollapsed && (
          <div>
            <p className="fw-bold mb-0 lh-sm">ApexBooking</p>
            {activeModule && <p className="text-muted small mb-0 lh-sm">{activeModule.label}</p>}
          </div>
        )}
      </div>
    )
  }

  if (isCollapsed) {
    return (
      <div className="d-flex justify-content-center px-1">
        <img src="/favicon.svg" alt="ApexBooking" width={28} height={28} className="rounded-2" />
      </div>
    )
  }

  return (
    <select className="form-select" defaultValue={MODULES[0]?.id} aria-label="Switch module">
      {MODULES.map((module) => (
        <option key={module.id} value={module.id}>
          {module.label}
        </option>
      ))}
    </select>
  )
}
