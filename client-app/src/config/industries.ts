import type { IIndustry } from '../interfaces/IIndustry'

export const INDUSTRIES: IIndustry[] = [
  { id: 'salon', name: 'Salon', icon: '/assets/icons/salon.svg', status: 'live' },
  { id: 'barbershop', name: 'Barbershop', icon: '/assets/icons/barbershop.svg', status: 'live' },
  { id: 'clinic', name: 'Clinic', icon: '/assets/icons/clinic.svg', status: 'coming-soon' },
  { id: 'fitness', name: 'Fitness', icon: '/assets/icons/fitness.svg', status: 'coming-soon' },
]
