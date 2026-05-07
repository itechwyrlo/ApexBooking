# Developer Setup Guide

This guide will walk you through setting up the ApexBooking application for local development.

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/) (for frontend)
- SQL Server (LocalDB or full instance)

---

## Step 1: Clone the Repository

```bash
git clone <repository-url>
cd ApexBooking
```

---

## Step 2: Initialize User Secrets

User Secrets store configuration securely outside the repository.

```bash
cd ApexBooking.WebApi
dotnet user-secrets init
```

This creates a secrets.json file in your user profile (not tracked in git).

---

## Step 3: Configure JWT Keys

The application uses RS256 asymmetric encryption for JWT tokens. You need:
- Private key (for signing tokens)
- Public key (for verifying tokens)

### Set JWT Keys in User Secrets

```bash
cd ApexBooking.WebApi

# Set Private Key
dotnet user-secrets set "Jwt:PrivateKeyPem" "-----BEGIN PRIVATE KEY-----
your-private-key-content-here
-----END PRIVATE KEY-----"

# Set Public Key
dotnet user-secrets set "Jwt:PublicKeyPem" "-----BEGIN PUBLIC KEY-----
your-public-key-content-here
-----END PUBLIC KEY-----"
```

**Note**: Replace the placeholder content with your actual RSA keys.

### Verify Secrets are Set

```bash
dotnet user-secrets list
```

You should see both `Jwt:PrivateKeyPem` and `Jwt:PublicKeyPem` listed.

---

## Step 4: Configure Database Connection

### Set Connection String in User Secrets

```bash
cd ApexBooking.WebApi
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\\mssqllocaldb;Database=ApexBooking;Trusted_Connection=True;MultipleActiveResultSets=true"
```

**Or for full SQL Server:**
```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=YOUR_SERVER;Database=ApexBooking;User Id=YOUR_USER;Password=YOUR_PASSWORD;MultipleActiveResultSets=true"
```

---

## Step 5: Apply Database Migrations

```bash
cd ApexBooking.WebApi
dotnet ef database update --project ../ApexBooking.Core.Persistence
```

This creates the database and applies all migrations.

---

## Step 6: Build the Frontend

The React/Vite frontend builds into the backend's wwwroot folder.

```bash
cd apex-booking-client
npm install
npm run build
```

---

## Step 7: Run the Application

### Option 1: Using .NET CLI

```bash
cd ApexBooking.WebApi
dotnet run
```

### Option 2: Using Visual Studio

1. Open `ApexBooking.sln`
2. Set `ApexBooking.WebApi` as startup project
3. Press F5 or click "Start Debugging"

---

## Step 8: Verify the Application

1. Open your browser and navigate to: `https://localhost:5127`
2. You should see the React application
3. API is available at: `https://localhost:5127/api`

---

## Troubleshooting

### "JWT PublicKeyPem not configured"

- Make sure you initialized User Secrets: `dotnet user-secrets init`
- Verify keys are set: `dotnet user-secrets list`
- Check for typos in the key names (case-sensitive!)

### Database connection failed

- Verify SQL Server is running
- Check connection string in User Secrets
- Try connecting with SQL Server Management Studio first

### Frontend not loading

- Make sure you ran `npm run build` in `apex-booking-client`
- Verify files exist in `ApexBooking.WebApi/wwwroot`

---

## What's Next?

- See [ENVIRONMENT_VARIABLES.md](./ENVIRONMENT_VARIABLES.md) for full configuration reference
- See [ApexBooking.Architecture.md](./ApexBooking.Architecture.md) for architecture overview
