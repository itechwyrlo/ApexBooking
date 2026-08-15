// Mirrors ApexBooking.Core.Application.Features.PublicBookings.Queries.GetPublicServicesByBranch.PublicServiceSummary
// Deliberately narrower than the authenticated dashboard's IService — this is the public wizard's
// own contract, not shared with the dashboard.
export interface IPublicService {
  serviceId: string
  name: string
  description: string | null
  durationMinutes: number
  price: number
  currencyCode: string
}
