// Mirrors ApexBooking.Core.Application.Common.ReferenceData.Psgc records
export interface IPsgcProvince {
  provCode: string
  provName: string
  cityClass: string | null
}

export interface IPsgcCity {
  provCode: string
  munCityCode: string
  munCityName: string
}

export interface IPsgcBarangay {
  munCityCode: string
  brgyCode: string
  brgyName: string
}
