import type { IModule } from '../interfaces/IModule'

// basePath isn't consumed for navigation anywhere today (ModuleSwitcher only renders label/id) —
// kept relative for consistency with the rest of the dashboard-route convention regardless.
export const MODULES: IModule[] = [{ id: 'booking', label: 'Booking', basePath: 'dashboard' }]
