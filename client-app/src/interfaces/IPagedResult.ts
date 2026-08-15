// Mirrors ApexBooking.SharedKernel.Models.QueryResult<T>
export interface IPagedResult<T> {
  data: T[]
  total: number
}

export interface IPageParams {
  pageNumber?: number
  pageSize?: number
}
