import { authClient } from '../api/clients/authClient'
import type { IAppearance, IAppearanceValues } from '../interfaces/IAppearance'

export async function getAppearance(): Promise<IAppearance> {
  const response = await authClient.get<IAppearance>('/api/Tenant/appearance')
  return response.data
}

export async function updateAppearance(values: IAppearanceValues): Promise<void> {
  await authClient.put('/api/Tenant/appearance', values)
}
