import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { VitePWA } from 'vite-plugin-pwa'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      registerType: 'autoUpdate',  // Automatically update Service Worker
      includeAssets: ['favicon.ico', 'robots.txt', 'apple-touch-icon-180x180.png'],  // Files to cache
      workbox: {
        // The SW's navigation handler otherwise serves the cached app shell (index.html) for
        // every navigation on the origin, which is what let public marketing/booking routes be
        // presented as part of the installable app. These patterns mirror the public routes
        // registered in src/routes/AppRoutes.tsx (landing, request-access, tenant login/reset,
        // public booking) — everything NOT listed here (dashboard, /pwa-init, /admin) keeps
        // being served the cached shell as before.
        navigateFallbackDenylist: [
          /^\/$/,
          /^\/find-workspace$/,
          /^\/request-access(\/pending)?$/,
          /^\/superadmin\/login$/,
          /^\/[^/]+\/login$/,
          /^\/[^/]+\/reset-password$/,
          /^\/[^/]+\/book(\/.*)?$/,
          /^\/[^/]+\/cancel-booking$/,
          /^\/[^/]+\/refund-status$/,
        ],
      },
      manifest: {
        name: 'ApexBooking',
        short_name: 'ApexBooking',
        description: 'Online booking and appointment scheduling for local businesses.',
        theme_color: '#4f46e5',
        background_color: '#ffffff',
        display: 'standalone',
        // Every install launches through the boot gateway (src/pages/PwaInit.tsx) instead of
        // straight into the last-viewed URL — sessionStorage never survives a fresh app launch,
        // so PwaInit is what resolves the session and routes to the right dashboard.
        start_url: '/pwa-init?source=pwa',
        scope: '/',
        icons: [
          {
            src: 'pwa-64x64.png',
            sizes: '64x64',
            type: 'image/png',
          },
          {
            src: 'pwa-192x192.png',
            sizes: '192x192',
            type: 'image/png',
          },
          {
            src: 'pwa-512x512.png',
            sizes: '512x512',
            type: 'image/png',
            purpose: 'any',
          },
          {
            src: 'maskable-icon-512x512.png',
            sizes: '512x512',
            type: 'image/png',
            purpose: 'maskable',
          },
        ],
      }
    })
  ]
})
