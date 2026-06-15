# Fintech Wallet API


![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-API-239120?style=for-the-badge&logo=csharp&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-EF_Core-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)
![Swagger](https://img.shields.io/badge/Swagger-OpenAPI-85EA2D?style=for-the-badge&logo=swagger&logoColor=black)
![GitHub Actions](https://img.shields.io/badge/GitHub_Actions-CI%2FCD-2088FF?style=for-the-badge&logo=githubactions&logoColor=white)

A secure ASP.NET Core wallet backend for user registration, JWT/JWE authentication, digital wallet operations, admin balance control, payment gateway callbacks, and automated EC2 deployment.

This API supports user wallets, deposits, withdrawals, transfers, bank statements, admin approvals, dynamic permission policies, and a GitHub Actions based CI/CD pipeline.

---

## Table of Contents

- [Features](#features)
- [Tech Stack](#tech-stack)
- [Architecture](#architecture)
- [Project Structure](#project-structure)
- [GitHub Actions CI/CD](#github-actions-cicd)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [API Endpoints](#api-endpoints)
- [Authentication](#authentication)
- [Database](#database)
- [Useful Commands](#useful-commands)

---

## Features

- User registration with automatic wallet creation
- Secure login with BCrypt password hashing
- JWT Bearer authentication with token encryption support
- Refresh token rotation
- Role-based access control for `User` and `Admin`
- Dynamic permission policies such as `Deposit`, `Transfer`, `Statement`, and `CashOut_Withdraw`
- Manual bank-transfer deposit flow
- Online payment gateway deposit initialization
- Withdraw and wallet-to-wallet transfer
- Bank statement and wallet detail lookup
- Admin wallet lock/unlock
- Admin manual credit/debit adjustment
- Admin pending bank deposit approval
- Payment gateway notify and confirmation endpoints
- Global exception handling with consistent API responses
- Swagger/OpenAPI documentation
- Login rate limiting
- URL-based API versioning, for example `/api/v1/...`
- GitHub Actions build and deployment workflow

---

## Tech Stack

| Layer | Technology |
| --- | --- |
| Framework | ASP.NET Core 8 Web API |
| Language | C# |
| Database | SQL Server |
| ORM | Entity Framework Core 8, Database First |
| Architecture | Clean Architecture |
| Authentication | JWT Bearer with encrypted token support |
| Authorization | Roles and dynamic policies |
| Password Hashing | BCrypt.Net-Next |
| Documentation | Swagger / Swashbuckle |
| Deployment | GitHub Actions, EC2, systemd, Nginx |

---

## Architecture

The project follows Clean Architecture principles. The API layer handles requests, the service layer contains business rules, repository abstractions isolate data access, and EF Core maps the existing SQL Server database using a Database First approach.

```text
Client
  |
  v
API Layer
Controllers, middleware, authentication, authorization
  |
  v
Application / Service Layer
Business rules, wallet operations, payment orchestration
  |
  v
Data Access Layer
Repositories, Unit of Work, EF Core DbContext
  |
  v
Infrastructure
SQL Server database, system services, payment gateway callbacks
```

Payment gateway flow:

```text
WalletController
  |
  v
PaymentService
  |
  v
Gateway Notify / Webhook
  |
  v
Wallet Balance Settlement
```

Core responsibilities:

| Layer | Responsibility |
| --- | --- |
| API | Controllers, routes, authentication, authorization, and middleware |
| Application / Services | Business logic for auth, wallet operations, transfers, deposits, withdrawals, and payments |
| Domain Models | Request/response models and wallet-related business data structures |
| Data Access | Repository interfaces, repository implementations, and Unit of Work |
| Infrastructure | EF Core `WalletdbContext`, SQL Server integration, token service, payment gateway service |
| Cross-Cutting | Global exception handling, policy middleware, validation, hashing, and signature verification |

---

## Project Structure

```text
fintech-wallet-api/
|
|-- .github/
|   `-- workflows/
|       |-- ci-cd.yml              # Main GitHub Actions pipeline
|       |-- reusable-build.yml     # Reusable build and deploy workflow
|       `-- infra/
|           |-- production.service # Production systemd service
|           |-- staging.service    # Staging systemd service
|           `-- walletapi.conf     # Nginx reverse proxy config
|
|-- wallet.sln
|-- README.md
|
`-- wallet/
    |-- Constants/
    |-- Controllers/               # API layer endpoints
    |-- DALs/                      # Data access repositories and Unit of Work
    |-- Data/                      # Database First DbContext and entities
    |-- Exceptions/                # Custom exceptions
    |-- Helpers/                   # Hashing and validators
    |-- Middleware/                # Cross-cutting middleware
    |-- Models/                    # Request and response DTOs / domain-facing contracts
    |-- Properties/                # Launch settings
    |-- Services/                  # Application business logic
    |-- Utils/                     # Utility classes
    |-- Program.cs                 # Startup and DI configuration
    |-- appsettings.json
    |-- appsettings.Staging.json
    |-- appsettings.Production.json
    `-- wallet.csproj
```

---

## GitHub Actions CI/CD

This repository includes a reusable GitHub Actions workflow for build and deployment.

```text
Push to develop  -> Staging workflow
Push to main     -> Production workflow

Workflow:
Restore and build
-> Publish artifact
-> Copy to EC2
-> Update current release
-> Restart systemd service
-> Nginx reverse proxy
-> Health check /health
```

### Workflow Files

| File | Purpose |
| --- | --- |
| `.github/workflows/ci-cd.yml` | Main workflow entry point for `main` and `develop` branches |
| `.github/workflows/reusable-build.yml` | Shared build, publish, artifact, deploy, restart, and health-check workflow |
| `.github/workflows/infra/staging.service` | Staging `systemd` service file |
| `.github/workflows/infra/production.service` | Production `systemd` service file |
| `.github/workflows/infra/walletapi.conf` | Nginx reverse proxy config |

### Branch Flow

| Branch/Event | Environment | Port | Deploy Path |
| --- | --- | --- | --- |
| Push to `develop` | Staging | `5151` | `/var/www/walletapi-staging` |
| Push to `main` | Production | `5001` | `/var/www/walletapi` |
| Pull request to `main` | CI validation | - | Build workflow |

### Pipeline Steps

1. Checkout source code
2. Cache NuGet packages
3. Setup .NET 8 SDK
4. Restore dependencies
5. Build with `Release` configuration
6. Publish into `publish-out`
7. Upload build artifact
8. Download artifact in deploy job
9. Generate `walletapi.env` from GitHub Secrets
10. Copy app, infra files, and env file to EC2
11. Create timestamped release folder
12. Switch `current` symlink to latest release
13. Install/update `systemd` service
14. Install/update Nginx config
15. Restart API service
16. Run `/health` deployment check
17. Keep the latest 5 releases
18. Cleanup Temp Files

### Required GitHub Secrets

```text
EC2_HOST
EC2_USERNAME
EC2_SSH_KEY
DB_CONNECTION
JWT_KEY
JWT_ENCRYPTION_KEY
PAYMENT_SECRET_KEY
```

These secrets are written to the server environment file as:

```text
ConnectionStrings__Wallet
Jwt__Key
Jwt__EncryptionKey
PaymentGateway__SecretKey
```

---

## Getting Started

### Prerequisites

- .NET SDK 8.0 or later
- SQL Server
- Visual Studio 2022, Rider, VS Code, or another C# editor

### Installation

```bash
# Clone the repository
git clone <repository-url>

# Navigate to the project folder
cd fintech-wallet-api

# Restore dependencies
dotnet restore wallet.sln

# Build the solution
dotnet build wallet.sln

# Run the API
dotnet run --project wallet/wallet.csproj
```

The API can be opened at:

```text
http://localhost:5151

```

Swagger UI:

```text
http://localhost:5151/swagger
```

Health check:

```text
GET /health
```

---

## Configuration

Update `wallet/appsettings.json` for local development, or use environment variables for deployment.

```json
{
  "ConnectionStrings": {
    "Wallet": "Server=localhost;Database=WalletDb;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Jwt": {
    "Key": "base64-encoded-signing-key",
    "EncryptionKey": "base64-encoded-encryption-key",
    "Issuer": "MyWalletAPI",
    "Audience": "MyWalletClients"
  },
  "PaymentGateway": {
    "SecretKey": "your-payment-gateway-secret"
  }
}
```

Important:

- `Jwt:Key` must be Base64 encoded.
- `Jwt:EncryptionKey` must be Base64 encoded.
- Do not commit real database strings, JWT secrets, or payment gateway secrets.
- Production secrets should be stored in GitHub Secrets, environment variables.

Environment config files:

| File | Purpose |
| --- | --- |
| `wallet/appsettings.json` | Default app settings |
| `wallet/appsettings.Staging.json` | Staging settings |
| `wallet/appsettings.Production.json` | Production settings |

---

## API Endpoints

Base URL:

```text
/api/v1
```

### Auth

| Method | Endpoint | Description |
| --- | --- | --- |
| POST | `/api/v1/auth/register` | Register a user and create wallet |
| POST | `/api/v1/auth/login` | Login and receive token |
| POST | `/api/v1/auth/refresh-token` | Rotate access and refresh tokens |

### Wallet

Requires `User` role and Bearer token.

| Method | Endpoint | Description |
| --- | --- | --- |
| POST | `/api/v1/wallet/deposit/bank-transfer` | Create manual bank deposit request |
| POST | `/api/v1/wallet/deposit/gateway` | Initialize gateway deposit |
| POST | `/api/v1/wallet/withdraw` | Withdraw from wallet |
| POST | `/api/v1/wallet/transfer` | Transfer to another wallet |
| GET | `/api/v1/wallet/bank-statement` | Get transaction history |
| GET | `/api/v1/wallet/wallet` | Get wallet details |

### Admin

Requires `Admin` role and Bearer token.

| Method | Endpoint | Description |
| --- | --- | --- |
| POST | `/api/v1/admin/wallets/{id}/lock` | Lock or unlock wallet |
| POST | `/api/v1/admin/wallet/adjust-balance` | Credit or debit wallet balance |
| POST | `/api/v1/admin/deposit/approve` | Approve pending bank deposit |

### Payment Gateway

These endpoints allow anonymous access because they are intended for payment gateway callbacks. Signature verification is handled with `PaymentGateway:SecretKey`.

| Method | Endpoint | Description |
| --- | --- | --- |
| GET | `/api/v1/payment/payment-notify` | Handle gateway redirect/notify result |
| POST | `/api/v1/payment/payment-confirm` | Settle gateway transaction callback |

### Transaction Reference and Retry Behavior

For withdraw and transfer requests, clients should generate and send a unique `referenceNo` before calling the API. This prevents duplicate withdrawals or transfers when the backend succeeds but the client loses the response because of a timeout or network issue.

If the same `referenceNo` is sent again and the transaction is already successful, the API returns the existing success result without applying the transaction again.

If `referenceNo` is omitted, the backend generates one, but the client cannot safely retry after a lost response because it may not know that generated value.

For transfers, the API returns one base `referenceNo` to the client, while the database stores two ledger references:

```text
TRF-ABC123456789-OUT
TRF-ABC123456789-IN
```

---

## Authentication

Protected endpoints require this header:

```http
Authorization: Bearer <access-token>
```

The API validates:

- Token issuer
- Token audience
- Token lifetime
- Signing key
- Token decryption key
- User role
- Dynamic permission policy

---

## Database

The API uses `WalletdbContext` with SQL Server and follows a Database First approach.

This project does not use EF Core migrations as the source of truth. The database schema is maintained in SQL Server first, and the EF Core `DbContext` plus entity classes are generated or updated from the existing database structure.

Main entities:

| Entity | Description |
| --- | --- |
| `Users` | Application users |
| `Roles` | User role definitions |
| `RoleClaims` | Dynamic permission claims |
| `Wallets` | Wallet account data |
| `Transactions` | Ledger and transaction records |

When the database schema changes, update the EF Core model from the database instead of adding migrations. A typical scaffold command looks like this:

```bash
dotnet ef dbcontext scaffold "<connection-string>" Microsoft.EntityFrameworkCore.SqlServer --context WalletdbContext --output-dir Data/Entities --context-dir Data --force
```

Review generated changes carefully before committing, especially entity relationships, nullable columns, decimal precision, and default values.

---

## Response Format

Success response:

```json
{
  "success": true,
  "message": "Login successful.",
  "data": {}
}
```

Failure response:

```json
{
  "success": false,
  "message": "Unauthorized access. Token is missing or invalid.",
  "data": null
}
```

---


## Deployment Notes

- Staging runs on `http://localhost:5151`.
- Production runs on `http://localhost:5001`.
- Nginx proxies public HTTP traffic to the local API ports.
- `systemd` keeps the API running as a service.
- GitHub Actions performs a `/health` check after deployment.
- The deployment keeps the latest 5 release folders on the server.

# fintech-wallet-api