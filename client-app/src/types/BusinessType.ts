export const BusinessType = {
  BarberShop: 'BarberShop',
  Salon: 'Salon',
  Spa: 'Spa',
  Clinic: 'Clinic',
  FitnessStudio: 'FitnessStudio',
  AutoRepair: 'AutoRepair',
  RetailService: 'RetailService',
  Other: 'Other',
} as const

export type BusinessType = (typeof BusinessType)[keyof typeof BusinessType]
