# ApexBooking Deployment Readiness Plan
**Date:** May 7, 2026  
**Status:** Planning Phase (Not Yet Deployed)  
**Target:** Docker-based production deployment while supporting local development

---

## SECTION 1: CURRENT STATE ASSESSMENT

### Current Infrastructure Status
```
Backend:
  ✅ ASP.NET Core 10 Web API (fully built, compiles cleanly)
  ✅ Clean Architecture (Domain, Application, Persistence, Infrastructure)
  ✅ Authentication: JWT (RS256) with User Identity
  ✅ Database: Entity Framework Core with SQL Server support
  ✅ Security: Rate limiting, security headers, CORS configured
  ❌ Docker: Not yet containerized
  ❌ Deployment: No deployment scripts/configs

Frontend:
  ✅ React + TypeScript + Vite (fully built)
  ✅ Environment-based API URL (VITE_API_BASE_URL)
  ❌ Docker: Not yet containerized

Database:
  ❌ Migration: Not automated for deployment
  ❌ Seeding: No seed data strategy defined
```

### Configuration Files Status
```
appsettings.json
  ✅ Base configuration (non-sensitive)
  ❌ JWT keys missing (intentional - secrets only)
  ❌ Connection string empty (intentional - secrets only)
  ✅ Default CORS, rate limiting, security settings

appsettings.Development.json
  ✅ Development-specific overrides
  ❌ JWT keys missing (intentional - from User Secrets)
  ❌ Connection string empty (needs setup)
  ✅ Relaxed security settings (HTTPS disabled)

User Secrets
  ❌ Not yet initialized
  ❌ JWT keys not set
  ❌ Database connection not set
```

---

## SECTION 2: CURRENT ISSUES & BLOCKERS

### Issue 1: Missing JWT Configuration
**Status:** BLOCKING - App won't start
**Root Cause:** `ConfigurationValidationExtensions.cs` requires JWT keys
**Current State:**
- RSA keys exist locally: `C:\Users\Wyrlo\{RSA PRIVATE KEY.pem, PUBLIC KEY.pem}`
- Keys NOT in config files (correct for security)
- Keys NOT in User Secrets (needs to be done)

**Impact:** 
- `dotnet run` fails with InvalidOperationException
- Any deployment will fail without these keys

### Issue 2: Database Connection String Missing
**Status:** BLOCKING - Database access fails
**Current State:**
- `appsettings.json` has empty connection string (correct)
- `appsettings.Development.json` doesn't specify override
- Not configured in User Secrets

**Impact:**
- Database operations will fail in any environment
- Development testing impossible without setup

### Issue 3: No Docker Infrastructure
**Status:** PLANNING - Required for deployment readiness
**Current State:**
- No Dockerfile
- No docker-compose.yml
- No .dockerignore
- No multi-stage build strategy

**Impact:**
- Cannot containerize for production
- Manual deployment only (not scalable)

### Issue 4: No Deployment Documentation
**Status:** PLANNING - Required for DevOps/Operations
**Current State:**
- ENVIRONMENT_VARIABLES.md exists (good)
- No deployment guide for Docker
- No production configuration templates
- No Azure/AWS/Kubernetes configs

---

## SECTION 3: WHAT WE CURRENTLY HAVE

### Code & Configuration
```
✅ Security Framework:
   - Rate limiting policies (global, auth-specific)
   - Security headers middleware (CSP, X-Frame-Options, etc.)
   - CORS configuration (environment-aware)
   - Configuration validation (checks for required secrets)
   - JWT RS256 implementation (public key validation)

✅ Configuration Management:
   - Environment-aware configuration (Dev vs non-Dev)
   - Options pattern for strong-typed config
   - Validation during startup

✅ Documentation:
   - ENVIRONMENT_VARIABLES.md (comprehensive)
   - Architecture documentation (ApexBooking.Architecture.md)
   - Entity relationship diagrams

✅ Assets/Keys:
   - RSA Private Key: C:\Users\Wyrlo\RSA PRIVATE KEY.pem
   - RSA Public Key: C:\Users\Wyrlo\PUBLIC KEY.pem
   - Both in PEM format, ready to use
```

### Project Structure (Ready for Containerization)
```
ApexBooking/
├── ApexBooking.WebApi/           (Entry point - ready to containerize)
├── ApexBooking.Core.Application/ (Business logic)
├── ApexBooking.Core.Domain/      (Domain entities)
├── ApexBooking.Core.Persistence/ (Data access)
├── ApexBooking.Infrastructure/   (External services, configs)
├── apex-booking-client/          (React frontend)
└── Framework.Core/               (Shared utilities)

⚠️ Missing:
   ├── Dockerfile (backend)
   ├── Dockerfile (frontend)
   ├── docker-compose.yml
   ├── .dockerignore
   ├── entrypoint.sh (for migrations)
   └── .env.example (for developers)
```

---

## SECTION 4: DEPLOYMENT READINESS PLAN

### Phase 1: Local Development Setup (IMMEDIATE)
**Objective:** Enable developers to run app locally  
**Duration:** < 30 minutes per developer

**Steps:**
1. Initialize User Secrets
   ```powershell
   cd ApexBooking.WebApi
   dotnet user-secrets init
   ```

2. Set JWT Keys (from existing RSA key files)
   ```powershell
   $privateKey = Get-Content "C:\Users\Wyrlo\RSA PRIVATE KEY.pem" -Raw
   $publicKey = Get-Content "C:\Users\Wyrlo\PUBLIC KEY.pem" -Raw
   dotnet user-secrets set "Jwt:PrivateKeyPem" "$privateKey"
   dotnet user-secrets set "Jwt:PublicKeyPem" "$publicKey"
   ```

3. Configure Local Database
   ```powershell
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=ApexBooking;User Id=sa;Password=YourLocalPassword;Encrypt=false;"
   ```

4. (Optional) Email/External Services
   ```powershell
   dotnet user-secrets set "BrevoSmtp:Key" "dev-key"
   dotnet user-secrets set "EmailSettings:SenderEmail" "dev@localhost"
   ```

5. Create `.env.example` (for git, no secrets)
   ```
   # For developers: Copy to .env.development.local and fill in YOUR secrets
   Jwt__PrivateKeyPem=<your-private-key>
   Jwt__PublicKeyPem=<your-public-key>
   ConnectionStrings__DefaultConnection=<your-db-connection>
   ```

**Deliverables:**
- ✅ Users can run `dotnet run` successfully
- ✅ Database connectivity works
- ✅ JWT authentication functional

---

### Phase 2: Docker Containerization (THIS SPRINT)
**Objective:** Create production-ready containers  
**Duration:** 2-3 hours

**Files to Create:**

**A. Backend Dockerfile** (`ApexBooking.WebApi/Dockerfile`)
```dockerfile
# Multi-stage build: Build and Runtime
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=builder /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

EXPOSE 5000
ENTRYPOINT ["dotnet", "ApexBooking.WebApi.dll"]
```

**B. Frontend Dockerfile** (`apex-booking-client/Dockerfile`)
```dockerfile
FROM node:20-alpine AS builder
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
ARG VITE_API_BASE_URL=http://localhost:5000/api
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
RUN npm run build

FROM nginx:alpine
COPY --from=builder /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/nginx.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

**C. docker-compose.yml** (Local orchestration)
```yaml
version: '3.9'

services:
  # SQL Server Database
  mssql:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: 'Y'
      SA_PASSWORD: 'ApexBooking@123'
    ports:
      - "1433:1433"
    volumes:
      - sqlserver-data:/var/opt/mssql

  # Backend API
  api:
    build: 
      context: .
      dockerfile: ApexBooking.WebApi/Dockerfile
    environment:
      ASPNETCORE_ENVIRONMENT: Docker
      ConnectionStrings__DefaultConnection: "Server=mssql;Database=ApexBooking;User Id=sa;Password=ApexBooking@123;"
      Jwt__PrivateKeyPem: ${JWT_PRIVATE_KEY}
      Jwt__PublicKeyPem: ${JWT_PUBLIC_KEY}
      Jwt__Issuer: https://api.localhost
      Jwt__Audience: https://api.localhost
    ports:
      - "5000:5000"
    depends_on:
      - mssql
    volumes:
      - ./ApexBooking.WebApi/bin/Release/net10.0/publish:/app

  # Frontend SPA
  web:
    build:
      context: ./apex-booking-client
      dockerfile: Dockerfile
      args:
        VITE_API_BASE_URL: http://localhost:5000/api
    ports:
      - "80:80"
    depends_on:
      - api

volumes:
  sqlserver-data:
```

**D. .dockerignore**
```
.git
.gitignore
.github
.vscode
node_modules
.env
.env.local
.env.development
bin
obj
dist
coverage
*.log
```

**E. .gitignore** (Update root)
```
# Environment & Secrets
.env
.env.local
.env.development.local
.env.production.local
secrets.json

# Build outputs
bin/
obj/
dist/
node_modules/

# IDE
.vscode/
.idea/
*.swp
*.swo

# OS
.DS_Store
Thumbs.db

# Logs
*.log
logs/
```

**Deliverables:**
- ✅ Multi-stage Docker builds (optimized size)
- ✅ docker-compose for local orchestration
- ✅ Environment variable injection ready
- ✅ Health checks configured

---

### Phase 3: Configuration for Different Environments (THIS SPRINT)
**Objective:** Support Dev, Test, and Production configs  
**Duration:** 1 hour

**Create Configuration Hierarchy:**

**appsettings.Docker.json** (New file for Docker environment)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "AllowedHosts": "*",
  "Jwt": {
    "Issuer": "https://api.docker.localhost",
    "Audience": "https://api.docker.localhost",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7
  },
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "Security": {
    "RequireHttps": true,
    "CookieDomain": ".docker.localhost"
  },
  "Cors": {
    "AllowedOrigins": "http://*.docker.localhost",
    "AllowedMethods": ["GET", "POST", "PUT", "DELETE"],
    "AllowCredentials": true
  }
}
```

**appsettings.Production.json** (Template for production)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Error"
    }
  },
  "AllowedHosts": "*",
  "Jwt": {
    "Issuer": "https://api.yourdomain.com",
    "Audience": "https://api.yourdomain.com",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7
  },
  "Security": {
    "RequireHttps": true,
    "CookieDomain": "yourdomain.com"
  },
  "Cors": {
    "AllowedOrigins": "https://yourdomain.com",
    "AllowedMethods": ["GET", "POST", "PUT", "DELETE"],
    "AllowCredentials": true
  }
}
```

**Deliverables:**
- ✅ Environment-specific configurations
- ✅ Production-grade security settings
- ✅ Docker environment support

---

### Phase 4: Deployment Scripts & Documentation (NEXT SPRINT)
**Objective:** Automated deployment pipeline  
**Duration:** 2-3 hours

**Files to Create:**

**build-and-push.sh** (Build and push to registry)
```bash
#!/bin/bash
REGISTRY="yourregistry.azurecr.io"  # or docker.io, etc.
VERSION=$(date +%Y%m%d_%H%M%S)

# Build backend
docker build -t $REGISTRY/apex-booking-api:$VERSION \
             -t $REGISTRY/apex-booking-api:latest \
             -f ApexBooking.WebApi/Dockerfile .

# Build frontend
docker build -t $REGISTRY/apex-booking-web:$VERSION \
             -t $REGISTRY/apex-booking-web:latest \
             -f apex-booking-client/Dockerfile \
             --build-arg VITE_API_BASE_URL=https://api.yourdomain.com \
             apex-booking-client/

# Push to registry
docker push $REGISTRY/apex-booking-api:$VERSION
docker push $REGISTRY/apex-booking-web:$VERSION
```

**DEPLOYMENT.md** (Comprehensive guide)
```markdown
# Deployment Guide

## Prerequisites
- Docker Engine 20.10+
- Docker Compose 2.0+
- Azure Container Registry (or similar)

## Local Deployment (Development)
1. Copy .env.example to .env
2. Set JWT keys in .env
3. Run: docker-compose up -d

## Production Deployment (Azure)
1. Push images to ACR
2. Deploy via Azure Container Instances or App Service
3. Set environment variables from Key Vault
4. Run migrations: kubectl exec -it api -- dotnet migrate
5. Verify health: curl https://api.yourdomain.com/health
```

**Deliverables:**
- ✅ Automated build & push scripts
- ✅ Deployment guides for multiple platforms
- ✅ Health check endpoints configured

---

## SECTION 5: LOCAL DEVELOPMENT SUPPORT STRATEGY

### Development Workflow (Post-Setup)

**First Time:**
```powershell
# Clone repo
git clone <repo>
cd ApexBooking

# Setup (one-time)
cd ApexBooking.WebApi
dotnet user-secrets init
dotnet user-secrets set "Jwt:PrivateKeyPem" "<from file>"
dotnet user-secrets set "Jwt:PublicKeyPem" "<from file>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your db>"

# Run
dotnet run
```

**Subsequent Runs:**
```powershell
cd ApexBooking.WebApi
dotnet run  # That's it! User Secrets automatically loaded
```

**Using Docker Locally:**
```powershell
# Copy keys to temp file for docker-compose
$env:JWT_PRIVATE_KEY = Get-Content "C:\Users\Wyrlo\RSA PRIVATE KEY.pem" -Raw
$env:JWT_PUBLIC_KEY = Get-Content "C:\Users\Wyrlo\PUBLIC KEY.pem" -Raw

# Run everything
docker-compose up -d

# Check logs
docker-compose logs -f api
```

### What Stays Local (Developer Machine)
- ✅ User Secrets (in Windows user profile)
- ✅ Git repo (.git ignored)
- ✅ IDE settings (.vscode, .idea ignored)
- ✅ Local database (if not using Docker)

### What Goes to Git
- ✅ Code (all .cs, .tsx, .ts files)
- ✅ Configuration templates (appsettings.*.json)
- ✅ Docker files (Dockerfile, docker-compose.yml)
- ✅ Scripts (build-and-push.sh)
- ✅ Documentation (DEPLOYMENT.md, ENVIRONMENT_VARIABLES.md)

### What NEVER Goes to Git
- ❌ User Secrets (secrets.json)
- ❌ RSA key files
- ❌ .env files
- ❌ bin/ obj/ dist/ directories
- ❌ node_modules

---

## SECTION 6: IMPLEMENTATION ROADMAP

### Timeline & Priorities

| Phase | Task | Duration | Priority | Status |
|-------|------|----------|----------|--------|
| 1 | Initialize User Secrets | 10 min | 🔴 CRITICAL | Not Started |
| 1 | Set JWT Keys in Secrets | 5 min | 🔴 CRITICAL | Not Started |
| 1 | Configure DB Connection | 10 min | 🔴 CRITICAL | Not Started |
| 1 | Verify `dotnet run` works | 5 min | 🔴 CRITICAL | Not Started |
| 2 | Create backend Dockerfile | 30 min | 🟠 HIGH | Not Started |
| 2 | Create frontend Dockerfile | 30 min | 🟠 HIGH | Not Started |
| 2 | Create docker-compose.yml | 45 min | 🟠 HIGH | Not Started |
| 2 | Create .dockerignore | 10 min | 🟠 HIGH | Not Started |
| 3 | Create appsettings.Docker.json | 15 min | 🟠 HIGH | Not Started |
| 3 | Create appsettings.Production.json | 15 min | 🟠 HIGH | Not Started |
| 3 | Update .gitignore | 10 min | 🟠 HIGH | Not Started |
| 4 | Create build-and-push.sh | 30 min | 🟡 MEDIUM | Next Sprint |
| 4 | Create DEPLOYMENT.md | 45 min | 🟡 MEDIUM | Next Sprint |
| 4 | Setup CI/CD pipeline | 2 hrs | 🟡 MEDIUM | Next Sprint |

**Total Effort:** ~6-7 hours (spread across 2-3 sprints)

---

## SECTION 7: SUCCESS CRITERIA

### After Phase 1 (Local Development)
- ✅ `dotnet run` starts without JWT configuration errors
- ✅ Database connection established
- ✅ Health endpoint responds
- ✅ JWT authentication works

### After Phase 2 (Docker Containerization)
- ✅ `docker-compose up` starts all services
- ✅ Frontend accessible at localhost
- ✅ API accessible at localhost:5000
- ✅ Database persists data across restarts

### After Phase 3 (Environment Configuration)
- ✅ Different configs for Docker/Production
- ✅ All secrets via environment variables
- ✅ Security settings enforced per environment

### After Phase 4 (Deployment Ready)
- ✅ Images build successfully
- ✅ Images push to registry
- ✅ Deployment guide is complete
- ✅ Health checks respond on all environments

---

## SECTION 8: RISKS & MITIGATION

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| JWT keys lost/corrupted | Low | Critical | Store in secure vault, backup locally |
| Database migration fails in Docker | Medium | High | Test migrations locally first |
| Environment variable naming mismatch | Medium | Medium | Document variable names, create templates |
| Multi-stage Docker builds fail | Low | High | Test builds locally, validate syntax |
| Performance regression with containers | Low | Medium | Performance test before deployment |

---

## SECTION 9: NEXT STEPS

**Immediate (Today):**
1. Review this plan with team
2. Confirm Docker deployment choice (vs Kubernetes, App Service, etc.)
3. Approve budget/resources if needed

**Next Session:**
1. Execute Phase 1 (Local dev setup)
2. Verify app runs locally
3. Begin Phase 2 (Docker files)

**Post-Implementation:**
- Load test containerized app
- Security audit of Dockerfile
- Backup strategy for database volumes
- Disaster recovery testing

---

## Appendix: Environment Variable Reference

### Required (All Environments)
```
Jwt__PrivateKeyPem=<RSA private key PEM>
Jwt__PublicKeyPem=<RSA public key PEM>
Jwt__Issuer=<issuer URL>
Jwt__Audience=<audience URL>
ConnectionStrings__DefaultConnection=<database connection>
```

### Optional (By Feature)
```
BrevoSmtp__Key=<API key>
EmailSettings__SenderEmail=<email>
GoogleCalendar__ClientId=<ID>
MicrosoftGraph__ClientId=<ID>
```

### Infrastructure
```
ASPNETCORE_ENVIRONMENT=Docker|Production|Development
ASPNETCORE_URLS=http://+:5000
```

---

**Document Version:** 1.0  
**Last Updated:** May 7, 2026  
**Next Review:** Before Phase 2 implementation
