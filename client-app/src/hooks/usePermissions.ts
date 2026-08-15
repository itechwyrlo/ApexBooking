import { useAuth } from './useAuth'
import type { ISidebarNavItem } from '../interfaces/ISidebarNavItem'

interface IUsePermissionsResult {
  hasAccess: (item: ISidebarNavItem) => boolean
}

export function usePermissions(): IUsePermissionsResult {
  const { user } = useAuth()

  function hasAccess(item: ISidebarNavItem): boolean {
    if (!user) return false
    return item.roles.some((role) => user.roles.includes(role))
  }

  return { hasAccess }
}
