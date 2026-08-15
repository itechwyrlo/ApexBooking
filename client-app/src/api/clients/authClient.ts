import axios from 'axios'

const ACCESS_TOKEN_STORAGE_KEY = 'apexbooking.accessToken'

export const authClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  withCredentials: true,
})

let accessToken: string | null = sessionStorage.getItem(ACCESS_TOKEN_STORAGE_KEY)

export function getAccessToken(): string | null {
  return accessToken
}

export function setAccessToken(token: string): void {
  accessToken = token
  sessionStorage.setItem(ACCESS_TOKEN_STORAGE_KEY, token)
}

export function clearAccessToken(): void {
  accessToken = null
  sessionStorage.removeItem(ACCESS_TOKEN_STORAGE_KEY)
}

authClient.interceptors.request.use((config) => {
  if (accessToken) {
    config.headers.set('Authorization', `Bearer ${accessToken}`)
  }
  return config
})
