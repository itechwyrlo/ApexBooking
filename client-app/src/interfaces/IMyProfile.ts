export interface IMyProfile {
  id: string
  email: string
  firstName: string
  lastName: string
  fullName: string
  phoneNumber: string | null
  photoUrl: string | null
  isPlatformAdmin: boolean
}

export interface IUpdateMyProfileValues {
  firstName: string
  lastName: string
  phoneNumber: string
}

export interface IChangePasswordValues {
  currentPassword: string
  newPassword: string
  confirmPassword: string
}
