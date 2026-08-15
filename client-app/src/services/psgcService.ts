import { authClient } from '../api/clients/authClient'
import type { IPsgcBarangay, IPsgcCity, IPsgcProvince } from '../interfaces/IPsgc'

export async function getPsgcProvinces(): Promise<IPsgcProvince[]> {
  const response = await authClient.get<IPsgcProvince[]>('/api/reference/psgc/provinces')
  return response.data
}

export async function getPsgcCities(provCode: string): Promise<IPsgcCity[]> {
  const response = await authClient.get<IPsgcCity[]>(`/api/reference/psgc/provinces/${provCode}/cities`)
  return response.data
}

export async function getPsgcBarangays(munCityCode: string): Promise<IPsgcBarangay[]> {
  const response = await authClient.get<IPsgcBarangay[]>(`/api/reference/psgc/cities/${munCityCode}/barangays`)
  return response.data
}
