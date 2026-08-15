import { BusinessType } from '../types/BusinessType'

interface IBusinessTypeOption {
  value: BusinessType
  label: string
}

export const BUSINESS_TYPE_OPTIONS: IBusinessTypeOption[] = [
  { value: BusinessType.BarberShop, label: 'Barbershop' },
  { value: BusinessType.Salon, label: 'Salon' },
  { value: BusinessType.Spa, label: 'Spa' },
  { value: BusinessType.Clinic, label: 'Clinic' },
  { value: BusinessType.FitnessStudio, label: 'Fitness Studio' },
  { value: BusinessType.AutoRepair, label: 'Auto Repair' },
  { value: BusinessType.RetailService, label: 'Retail / Service' },
  { value: BusinessType.Other, label: 'Other' },
]
