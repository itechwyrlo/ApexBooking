import { NavLink } from 'react-router-dom'
import type { IShellNavItem } from '../../interfaces/IShellNavItem'

interface ISidebarNavItemProps {
  item: IShellNavItem
  isCollapsed?: boolean
}

export function SidebarNavItem({ item, isCollapsed }: ISidebarNavItemProps) {
  return (
    <li className="nav-item">
      <NavLink
        to={item.href}
        end={item.end}
        className={({ isActive }) => `sidebar-link${isActive ? ' active' : ''}${isCollapsed ? ' justify-content-center' : ''}`}
        title={isCollapsed ? item.label : undefined}
      >
        <img src={item.icon} alt="" width={18} height={18} aria-hidden="true" className="sidebar-icon" />
        <span className={isCollapsed ? 'visually-hidden' : undefined}>{item.label}</span>
      </NavLink>
    </li>
  )
}
