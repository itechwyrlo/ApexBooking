import * as signalR from '@microsoft/signalr'
import { getAccessToken } from './authClient'

// Browser transports (WebSockets/SSE) can't set a custom Authorization header, so the token goes
// as an "access_token" query param instead — the backend's JWT bearer config reads it from there
// specifically for /hubs/* paths (see AuthenticationExtensions.cs).
export function createNotificationHubConnection(): signalR.HubConnection {
  return new signalR.HubConnectionBuilder()
    .withUrl(`${import.meta.env.VITE_API_BASE_URL}/hubs/notifications`, {
      accessTokenFactory: () => getAccessToken() ?? '',
    })
    .withAutomaticReconnect()
    .build()
}
