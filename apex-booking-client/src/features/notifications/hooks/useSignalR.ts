import { useEffect } from 'react';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { useAuth } from '../../../context/AuthContext';
import type { NotificationDto } from '../types';

interface UseSignalRParams {
  appendNotification: (notification: NotificationDto) => void;
}

export const useSignalR = ({ appendNotification }: UseSignalRParams) => {
  const { isAuthenticated, accessToken } = useAuth();

  useEffect(() => {
    if (!isAuthenticated || !accessToken) return;

    const connection = new HubConnectionBuilder()
      .withUrl(import.meta.env.VITE_SIGNALR_HUB_URL, {
        accessTokenFactory: () => accessToken,
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on('ReceiveNotification', (notification: NotificationDto) => {
      appendNotification(notification);
    });

    connection.start().catch(() => {});

    return () => {
      connection.stop();
    };
  }, [isAuthenticated, accessToken, appendNotification]);
};
