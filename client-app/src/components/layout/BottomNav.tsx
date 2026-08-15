import { NavLink } from 'react-router-dom'
import { Icon } from '../common/Icon'
import type { IShellNavItem } from '../../interfaces/IShellNavItem'

interface IBottomNavProps {
  /** Already capped to the role's primary destinations — see utils/shellNav.ts. */
  items: IShellNavItem[]
  /** Whether anything was omitted from `items` and needs the "More" trigger. */
  showMore: boolean
  isMoreOpen: boolean
  onMoreClick: () => void
}

// Persistent mobile primary navigation (<992px), replacing the offcanvas-only pattern there.
// NavLink applies aria-current="page" to the active item automatically — no extra ARIA needed
// for that, matching how SidebarNavItem relies on the same built-in behavior.
export function BottomNav({ items, showMore, isMoreOpen, onMoreClick }: IBottomNavProps) {
  return (
    <nav className="bottom-nav d-lg-none" aria-label="Primary">
      {items.map((item) => (
        <NavLink key={item.key} to={item.href} end={item.end} className={({ isActive }) => `bottom-nav-item${isActive ? ' active' : ''}`}>
          <span className="bottom-nav-item-icon-wrap">
            <img src={item.icon} alt="" width={18} height={18} aria-hidden="true" className="sidebar-icon" />
          </span>
          <span>{item.label}</span>
        </NavLink>
      ))}
      {showMore && (
        <button
          type="button"
          className="bottom-nav-item"
          onClick={onMoreClick}
          aria-haspopup="true"
          aria-expanded={isMoreOpen}
        >
          <span className="bottom-nav-item-icon-wrap">
            <Icon name="more-horizontal" size={18} className="sidebar-icon" />
          </span>
          <span>More</span>
        </button>
      )}
    </nav>
  )
}
