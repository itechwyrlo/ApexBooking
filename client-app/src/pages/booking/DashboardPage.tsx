import { Role } from '../../types/Role'
import { useAuth } from '../../hooks/useAuth'
import { OwnerDashboardPage } from './OwnerDashboardPage'
import { AdminDashboardPage } from './AdminDashboardPage'
import { StaffDashboardPage } from './StaffDashboardPage'

// A membership can hold more than one role (IUser.roles is an array) — the
// highest-authority role a user holds decides which dashboard they land on.
const ROLE_PRECEDENCE: Role[] = [Role.Owner, Role.Admin, Role.Staff]

export function DashboardPage() {
  const { user } = useAuth()
  const roles = user?.roles ?? []
  const primaryRole = ROLE_PRECEDENCE.find((role) => roles.includes(role))

  if (primaryRole === Role.Admin) {
    return <AdminDashboardPage />
  }
  if (primaryRole === Role.Staff) {
    return <StaffDashboardPage />
  }
  return <OwnerDashboardPage />
}
