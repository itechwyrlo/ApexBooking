import axios from 'axios'

// Anonymous client for the public booking pages. Tenant identity travels as a path slug (see
// TenantMiddleware on the backend), not a Host header or JWT claim, so this is just a plain
// client — each service call builds the slug into the URL path itself.
export const publicClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
})
