# CI/CD Workflow Improvement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give ApexBooking a pre-merge CI gate (build + test + verify) and decouple the production deploy into build-once → verify → deploy stages, so a developer never has to build the production artifact locally before pushing.

**Architecture:** Add a new `ci.yml` workflow that runs build+lint+test on every PR/branch push (fast feedback before merge). Restructure the existing `deploy.yml` (push-to-`main` only) into three sequential jobs — `verify` (build+test, defense in depth even if someone pushes to `main` directly), `build` (produces the publish output once, uploads it as a GitHub Actions artifact), and `deploy` (downloads that exact artifact and runs the existing `msdeploy` step). No new hosting, no staging site, no manual-approval gate — per user decision, production stays fully automatic once verification passes.

**Tech Stack:** GitHub Actions, .NET 10 SDK, xunit, Node 20, npm, Vite, oxlint, MonsterASP.NET Web Deploy (msdeploy).

**Spec:** This plan was scoped directly from the user's CI/CD improvement request plus an inspection of the current repo state (see the "Current State" notes below — there is no separate spec doc for this task).

## Global Constraints

- Do not touch the in-progress, uncommitted monorepo consolidation (`src/backend-api/`, `client-app/` untracked; old flat `ApexBooking.*` folders staged as deleted). Build on top of the new layout; don't resolve or commit that consolidation as part of this work.
- No staging environment — MonsterASP.NET has no deployment-slot equivalent, and the user chose to skip a second hosting site. Verification happens pre-deploy, not on a staging copy.
- No manual-approval gate — the user chose fully automatic deploy once CI passes. Do not add a GitHub Environment with required reviewers.
- Keep the existing `msdeploy` mechanics (site name, secrets, `AppOffline` rule) unchanged — they already work in production. Only their trigger/job structure changes.
- Don't introduce Playwright e2e into the CI gate — it needs a running app + DB that CI doesn't provision, and that's out of scope for this smallest-change pass.

## Current State (from inspection)

- `.github/workflows/deploy.yml` triggers only on `push` to `main`. One job does: checkout → `npm install` → `npm run build` (frontend) → `node scripts/sync-wwwroot.mjs` → `dotnet publish -c Release` → `msdeploy` straight to the single production site (`site70197`).
- No workflow runs on PRs or feature branches — CI never verifies code before it reaches `main`.
- `ApexBooking.Core.Domain.UnitTests` (46 passing xunit tests, confirmed via `dotnet test` against the csproj directly) exists on disk but is **not** referenced in `ApexBooking.sln` (confirmed via `dotnet sln ApexBooking.sln list` — 8 projects, no test project). A solution-wide `dotnet build`/`publish` silently skips it.
- Frontend has no unit-test runner; only Playwright e2e (`npm run test:e2e`), never invoked in CI.
- DB migrations are applied manually outside the pipeline (unchanged by this plan).

---

### Task 1: Wire the backend unit test project into the solution

**Files:**
- Modify: `ApexBooking.sln` (via `dotnet sln add`, not hand-edited)

**Interfaces:**
- Produces: `ApexBooking.sln` now includes `src\backend-api\ApexBooking.Core.Domain.UnitTests\ApexBooking.Core.Domain.UnitTests.csproj`, so `dotnet build ApexBooking.sln` / `dotnet test ApexBooking.sln` picks it up. Tasks 2 and 3 rely on `dotnet test ApexBooking.sln` actually running these 46 tests.

- [ ] **Step 1: Add the test project to the solution**

Run from the repo root:

```bash
dotnet sln ApexBooking.sln add src/backend-api/ApexBooking.Core.Domain.UnitTests/ApexBooking.Core.Domain.UnitTests.csproj
```

- [ ] **Step 2: Verify the solution builds with the test project included**

Run: `dotnet build ApexBooking.sln -c Release`
Expected: Build succeeds, 9 projects listed (including `ApexBooking.Core.Domain.UnitTests`).

- [ ] **Step 3: Verify the tests run through the solution**

Run: `dotnet test ApexBooking.sln -c Release --no-build`
Expected: `Passed! - Failed: 0, Passed: 46, Skipped: 0, Total: 46` (same result as running the test csproj directly).

- [ ] **Step 4: Commit**

```bash
git add ApexBooking.sln
git commit -m "build: wire Core.Domain.UnitTests into the solution"
```

---

### Task 2: Add a pre-merge CI workflow (build + test + verify)

**Files:**
- Create: `.github/workflows/ci.yml`

**Interfaces:**
- Consumes: `ApexBooking.sln` from Task 1 (must include the test project for `dotnet test` to mean anything).
- Produces: A `CI` workflow with two jobs, `backend` and `frontend`, both required to pass. This is the "Build + Test + Verify" stage from the target pipeline, running before merge instead of at deploy time.

- [ ] **Step 1: Write the workflow**

Create `.github/workflows/ci.yml`:

```yaml
name: CI

on:
  pull_request:
    branches:
      - main
  push:
    branches-ignore:
      - main

jobs:
  backend:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET 10
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore
        run: dotnet restore ApexBooking.sln

      - name: Build
        run: dotnet build ApexBooking.sln -c Release --no-restore

      - name: Test
        run: dotnet test ApexBooking.sln -c Release --no-build --no-restore

  frontend:
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: client-app
    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '20'
          cache: 'npm'
          cache-dependency-path: client-app/package-lock.json

      - name: Install dependencies
        run: npm ci

      - name: Lint
        run: npm run lint

      - name: Build (type-check + bundle)
        run: npm run build
```

- [ ] **Step 2: Verify the workflow is syntactically valid**

Run: `dotnet` isn't needed for this check — just eyeball the YAML for indentation errors, then push it on a non-`main` branch (see Step 3) since GitHub Actions is the actual YAML validator here.

- [ ] **Step 3: Push and confirm it runs**

```bash
git add .github/workflows/ci.yml
git commit -m "ci: add pre-merge build+test+verify workflow"
git push
```

Expected: In the GitHub Actions tab, a `CI` run appears for this push with two green jobs, `backend` and `frontend`.

---

### Task 3: Restructure the deploy workflow into verify → build-once → deploy

**Files:**
- Modify: `.github/workflows/deploy.yml`

**Interfaces:**
- Consumes: `ApexBooking.sln` from Task 1 (the `verify` job below runs `dotnet test` against it, same as Task 2's `backend` job).
- Produces: Three sequential jobs — `verify`, `build` (uploads a `webapi-publish` artifact via `actions/upload-artifact@v4`), `deploy` (downloads that artifact via `actions/download-artifact@v4` and runs the existing `msdeploy` step, unchanged). The artifact that reaches production is the exact one that was verified — nothing is rebuilt between test and deploy.

- [ ] **Step 1: Rewrite the workflow**

Replace the full contents of `.github/workflows/deploy.yml` with:

```yaml
name: Deploy to MonsterASP.NET

on:
  push:
    branches:
      - main
  workflow_dispatch:

jobs:
  verify:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET 10
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore
        run: dotnet restore ApexBooking.sln

      - name: Build
        run: dotnet build ApexBooking.sln -c Release --no-restore

      - name: Test
        run: dotnet test ApexBooking.sln -c Release --no-build --no-restore

  build:
    needs: verify
    runs-on: ubuntu-latest
    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '20'
          cache: 'npm'
          cache-dependency-path: client-app/package-lock.json

      - name: Install frontend dependencies
        working-directory: client-app
        run: npm ci

      - name: Build frontend
        working-directory: client-app
        run: npm run build
        env:
          VITE_API_BASE_URL: ${{ secrets.VITE_API_BASE_URL }}

      # Copies client-app/dist into src/backend-api/ApexBooking.WebApi/wwwroot — the same
      # script used locally (scripts/sync-wwwroot.mjs), so CI and local dev share one
      # reproducible path to a wwwroot-ready SPA build instead of two.
      - name: Sync frontend build into wwwroot
        run: node scripts/sync-wwwroot.mjs

      - name: Setup .NET 10
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Publish
        run: dotnet publish src/backend-api/ApexBooking.WebApi/ApexBooking.WebApi.csproj -c Release -o ./publish

      - name: Upload publish artifact
        uses: actions/upload-artifact@v4
        with:
          name: webapi-publish
          path: ./publish
          retention-days: 7

  deploy:
    needs: build
    runs-on: windows-latest
    steps:
      - name: Download publish artifact
        uses: actions/download-artifact@v4
        with:
          name: webapi-publish
          path: ./publish

      - name: Deploy via Web Deploy
        shell: pwsh
        run: |
          $msdeploy = 'C:\Program Files\IIS\Microsoft Web Deploy V3\msdeploy.exe'
          $publishPath = Join-Path $env:GITHUB_WORKSPACE 'publish'
          $password = '${{ secrets.WEBDEPLOY_PASSWORD }}'
          $computerName = 'https://site70197.siteasp.net:8172/msdeploy.axd?site=site70197'

          $sourceArg = "-source:contentPath=$publishPath"
          $destArg = "-dest:contentPath=site70197,computerName=$computerName,userName=site70197,password=$password,authType=Basic"

          & $msdeploy -verb:sync $sourceArg $destArg -allowUntrusted -enableRule:AppOffline
```

Note: `workflow_dispatch` is added so this can be run on-demand from the Actions tab to validate the new job structure without needing an actual push to `main`.

- [ ] **Step 2: Dry-run via manual trigger before merging to main**

Push this branch, then in the GitHub Actions tab select the `Deploy to MonsterASP.NET` workflow and run it manually (`workflow_dispatch`) against this branch.

Expected: `verify` → `build` → `deploy` run in sequence, all green. Confirm the `webapi-publish` artifact appears in the run's Artifacts section, and confirm the live production site reflects the deploy afterward (this manual trigger *does* deploy to production, same as today's every-push-to-main behavior — there's no staging site to dry-run against instead).

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/deploy.yml
git commit -m "ci: split deploy workflow into verify, build-once, and deploy stages"
```

---

## Self-Review Notes

- **Spec coverage:** Pre-merge CI gate → Task 2. Build-once/immutable-artifact → Task 3's `build`/`deploy` split with `upload-artifact`/`download-artifact`. Test execution actually meaning something → Task 1. Staging and manual-approval gate were explicitly declined by the user, so no tasks exist for them.
- **No placeholders:** All workflow YAML is complete and copy-pasteable; no `TODO`/`fill in` markers.
- **Consistency:** `webapi-publish` artifact name matches between the `upload-artifact` step in `build` and the `download-artifact` step in `deploy`. `ApexBooking.sln` is referenced identically in Task 2's `backend` job and Task 3's `verify` job.
