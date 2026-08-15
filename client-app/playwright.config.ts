import { defineConfig, devices } from '@playwright/test'

// Minimal Playwright setup for this UI refactor's own visual/responsive verification —
// not a general test framework. Dev-only: not imported by src/, not part of the Vite build
// (tsc -b only type-checks tsconfig.app.json's "src" and tsconfig.node.json's vite.config.ts,
// neither of which includes this file or e2e/).
export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  reporter: 'list',
  use: {
    baseURL: 'http://localhost:5173',
    screenshot: 'only-on-failure',
  },
  // Two projects cover the "desktop layouts" / "mobile layouts" / "responsive breakpoints"
  // verification the refactor needs; add more device presets later if a specific phase needs them.
  projects: [
    { name: 'Desktop Chrome', use: { ...devices['Desktop Chrome'] } },
    { name: 'Mobile Chrome', use: { ...devices['Pixel 7'] } },
  ],
  // Auto-starts `npm run dev` for the test run and reuses it locally if already running
  // (matches how Step 1's manual verification was done).
  webServer: {
    command: 'npm run dev',
    url: 'http://localhost:5173',
    reuseExistingServer: !process.env.CI,
    timeout: 30_000,
  },
})
