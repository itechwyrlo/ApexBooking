// Mirrors ApexBooking.Core.Application.Dtos.Response.ServiceCatalogSummary
export interface IService {
  id: string
  name: string
  description: string | null
  durationMinutes: number
  bufferBeforeMinutes: number
  bufferAfterMinutes: number
  price: number
  currencyCode: string
  minAdvanceBookingHoursOverride: number | null
  isActive: boolean
  createdAt: string
}

export interface IServiceFormValues {
  name: string
  description: string
  durationMinutes: number
  price: number
  currencyCode: string
  bufferBeforeMinutes: number
  bufferAfterMinutes: number
  minAdvanceBookingHoursOverride: number | null
}
