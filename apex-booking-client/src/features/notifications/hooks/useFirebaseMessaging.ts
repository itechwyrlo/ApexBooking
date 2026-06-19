import { useState } from 'react';
import { getToken, onMessage } from 'firebase/messaging';
import { messaging } from '../../../config/firebase';
import axiosInstance from '../../../services/axiosInstance';
import type { NotificationDto } from '../types';

interface UseFirebaseMessagingParams {
  appendNotification: (notification: NotificationDto) => void;
}

export const useFirebaseMessaging = ({ appendNotification }: UseFirebaseMessagingParams) => {
  const [currentFcmToken, setCurrentFcmToken] = useState<string | null>(null);

  const requestPermissionAndSetup = async () => {
    const permission = await Notification.requestPermission();
    if (permission !== 'granted') return;

    const registration = await navigator.serviceWorker.register('/firebase-messaging-sw.js');

    const token = await getToken(messaging, {
      vapidKey: import.meta.env.VITE_FIREBASE_VAPID_KEY,
      serviceWorkerRegistration: registration,
    });

    await axiosInstance.post('/notification/register-token', { token });
    setCurrentFcmToken(token);

    onMessage(messaging, payload => {
      appendNotification({
        notificationId: crypto.randomUUID(),
        eventType: payload.data?.eventType ?? '',
        title: payload.notification?.title ?? '',
        message: payload.notification?.body ?? '',
        isRead: false,
        createdAt: new Date().toISOString(),
      });
    });
  };

  return { requestPermissionAndSetup, currentFcmToken };
};
