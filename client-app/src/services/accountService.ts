import { authClient } from '../api/clients/authClient'
import type { IChangePasswordValues, IMyProfile, IUpdateMyProfileValues } from '../interfaces/IMyProfile'

// Raw wire shape from ApexBooking.Core.Application.Dtos.Response.MyProfileDto
// (camelCase property names, ASP.NET Core's default JSON naming policy).
interface IMyProfileWire {
  id: string
  email: string
  firstName: string
  lastName: string
  fullName: string
  phoneNumber: string | null
  photoUrl: string | null
  isPlatformAdmin: boolean
}

function toMyProfile(wire: IMyProfileWire): IMyProfile {
  return {
    id: wire.id,
    email: wire.email,
    firstName: wire.firstName,
    lastName: wire.lastName,
    fullName: wire.fullName,
    phoneNumber: wire.phoneNumber,
    photoUrl: wire.photoUrl,
    isPlatformAdmin: wire.isPlatformAdmin,
  }
}

export async function getMyProfile(): Promise<IMyProfile> {
  const response = await authClient.get<IMyProfileWire>('/api/account/me')
  return toMyProfile(response.data)
}

export async function updateMyProfile(values: IUpdateMyProfileValues): Promise<void> {
  await authClient.put('/api/account/me', {
    firstName: values.firstName,
    lastName: values.lastName,
    phoneNumber: values.phoneNumber || null,
  })
}

export async function uploadMyProfilePhoto(file: File): Promise<string> {
  const formData = new FormData()
  formData.append('photo', file)
  const response = await authClient.post<{ photoUrl: string }>('/api/account/me/photo', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  })
  return response.data.photoUrl
}

export async function removeMyProfilePhoto(): Promise<void> {
  await authClient.delete('/api/account/me/photo')
}

// Every session is revoked server-side on a successful call, including this one — the caller
// (outside this module's scope) is expected to treat the resolved promise as "log out now,"
// the same handling as any other session-expiry path. No token is returned to keep alive.
export async function changeMyPassword(values: IChangePasswordValues): Promise<void> {
  await authClient.post('/api/account/me/change-password', {
    currentPassword: values.currentPassword,
    newPassword: values.newPassword,
    confirmPassword: values.confirmPassword,
  })
}
