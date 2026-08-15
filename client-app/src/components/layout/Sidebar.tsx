import type { ReactNode } from 'react'
import type { IShellNavSection } from '../../interfaces/IShellNavItem'
import { SidebarNavItem } from './SidebarNavItem'

interface ISidebarProps {
  /** The brand/switcher block at the top of the nav — role-specific (ModuleSwitcher for tenant, a static favicon+label block for Super Admin). */
  brand: ReactNode
  sections: IShellNavSection[]
  isCollapsed?: boolean
}

// Shared, config-driven nav-list renderer for both the tenant dashboard shell and the Super
// Admin shell — same markup either side always rendered (previously duplicated verbatim between
// this file and components/admin/AdminSidebar.tsx), driven entirely by `sections`/`brand` so
// role-specific navigation stays configuration-driven rather than forking the component.
export function Sidebar({ brand, sections, isCollapsed }: ISidebarProps) {
  return (
    <nav className="d-flex flex-column h-100 p-3" aria-label="Primary">
      {brand}
      <div className="d-flex flex-column gap-3 mt-4">
        {sections.map((section) => (
          <div key={section.key}>
            {section.showDividerBefore && <hr className="my-1 opacity-25" />}
            {!isCollapsed && section.label && <p className="sidebar-section-label mb-2">{section.label}</p>}
            <ul className="nav flex-column gap-1 list-unstyled">
              {section.items.map((item) => (
                <SidebarNavItem key={item.key} item={item} isCollapsed={isCollapsed} />
              ))}
            </ul>
          </div>
        ))}
      </div>
    </nav>
  )
}
