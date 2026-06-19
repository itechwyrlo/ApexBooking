import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    strictPort: true,
  },
  build: {
    // sourcemap: true, 
    // This reaches up one level and then into the WebApi project
    outDir: '../ApexBooking.WebApi/wwwroot',
    emptyOutDir: true, // Cleans the folder before every build
  },
})