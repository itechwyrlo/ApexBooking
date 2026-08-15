// Mirrors ApexBooking.Core.Application.Dtos.Response.CustomerSummary
export interface ICustomer {
  id: string
  name: string
  email: string | null
  phoneNumber: string | null
  createdAt: string
}
