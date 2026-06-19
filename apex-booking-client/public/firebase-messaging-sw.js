importScripts('https://www.gstatic.com/firebasejs/12.14.0/firebase-app-compat.js');
importScripts('https://www.gstatic.com/firebasejs/12.14.0/firebase-messaging-compat.js');

firebase.initializeApp({
  apiKey: 'AIzaSyCgXkIw09KnWxfooYwLo8tGUIa6cjWUuh0',
  authDomain: 'apexbooking-80a1e.firebaseapp.com',
  projectId: 'apexbooking-80a1e',
  storageBucket: 'apexbooking-80a1e.firebasestorage.app',
  messagingSenderId: '497481753958',
  appId: '1:497481753958:web:ac0fe5786a20d4b6b7d4a7',
});

const messaging = firebase.messaging();

messaging.onBackgroundMessage(payload => {
  const title = payload.notification?.title ?? 'New Notification';
  const body = payload.notification?.body ?? '';

  self.registration.showNotification(title, {
    body,
    icon: '/favicon.svg',
  });
});
