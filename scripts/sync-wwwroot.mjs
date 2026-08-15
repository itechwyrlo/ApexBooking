// Copies the frontend's production build (client-app/dist) into the ASP.NET Core
// web root (src/backend-api/ApexBooking.WebApi/wwwroot) so `dotnet publish`/`dotnet run`
// serves the current SPA build.
//
// Deliberately lives at the repo root rather than inside client-app/ — client-app's own
// package.json and vite.config.ts stay unaware of the backend's physical layout, so the
// frontend project remains buildable/testable on its own. This script is the one place
// that knows about both halves of the monorepo.
//
// Usage: node scripts/sync-wwwroot.mjs   (run after `npm run build`, see root package.json)

import { existsSync, rmSync, cpSync, readdirSync, statSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const repoRoot = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const distDir = path.join(repoRoot, 'client-app', 'dist');
const wwwrootDir = path.join(repoRoot, 'src', 'backend-api', 'ApexBooking.WebApi', 'wwwroot');

if (!existsSync(distDir)) {
  console.error(
    `[sync-wwwroot] ${distDir} does not exist.\n` +
    `Run "npm run build" (frontend production build) before syncing.`
  );
  process.exit(1);
}

function countFiles(dir) {
  let count = 0;
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    count += entry.isDirectory() ? countFiles(full) : 1;
  }
  return count;
}

// Clean wwwroot first so a previous build's hashed asset files (which Vite never
// overwrites in place, since every build produces new content-hashed filenames) don't
// linger alongside the new ones.
if (existsSync(wwwrootDir)) {
  rmSync(wwwrootDir, { recursive: true, force: true });
}

cpSync(distDir, wwwrootDir, { recursive: true });

const fileCount = countFiles(wwwrootDir);
console.log(`[sync-wwwroot] copied ${fileCount} files from client-app/dist to src/backend-api/ApexBooking.WebApi/wwwroot`);
