# IdentityHub

A Non-Human Identity (NHI) management platform that allows users to track and manage service accounts, API keys, service principals, and other machine identities — with Jira integration for reporting NHI findings.

## Architecture

```
IdentityHub/
├── IdentityHub.Api/          # ASP.NET Core Web API (.NET 10)
│   ├── IdentityHub.Api/      # Main API project
│   └── IdentityHub.Tests/    # xUnit test project
├── IdentityHub.Web/          # React TypeScript SPA (Vite)
├── docker-compose.yml
└── .github/
```

| Layer     | Stack                                        |
|-----------|----------------------------------------------|
| Backend   | ASP.NET Core, Entity Framework Core, SQLite  |
| Frontend  | React 19, TypeScript, Vite, React Router     |
| Auth      | JWT (app), API Key (external), BCrypt         |
| Jira      | Jira REST API v3 (Basic Auth with API Token)  |

## Quick Start

### Option 1: Docker Compose (Recommended)

```bash
docker compose up --build
```

- **Frontend:** http://localhost:3000
- **API:** http://localhost:5062
- **Swagger:** http://localhost:5062/swagger (dev mode only)

### Option 2: Manual

**Backend:**
```bash
cd IdentityHub.Api
dotnet run --project IdentityHub.Api
```

**Frontend:**
```bash
cd IdentityHub.Web
npm install
npm run dev
```

- **Frontend:** http://localhost:5173
- **API:** http://localhost:5062
- **Swagger:** http://localhost:5062/swagger

## Features

### User Authentication
- Register/login with email and password
- JWT tokens with 24-hour expiration
- Passwords hashed with BCrypt

### Jira Integration
- Connect your Jira workspace (email + API token + site URL)
- API tokens encrypted at rest using ASP.NET Core Data Protection API
- Browse and select from your Jira projects
- Create NHI finding tickets with title and description
- View 10 most recent tickets created from this app (clickable → opens Jira)

### External REST API
- API key-authenticated endpoint for external systems (scanners, CI/CD)
- `POST /api/v1/findings` with `X-Api-Key` header
- Multiple API keys per user, each independently revocable
- Keys hashed with SHA-256 before storage

### Multi-Tenancy
- Each user has isolated data (Jira config, tickets, API keys)
- Data scoped via UserId foreign keys on all entities
- All database queries in authenticated endpoints filter by the current user's ID
- No cross-user data leakage — users can only read and modify their own records
- External API (`/api/v1/tickets`) is stateless and credential-scoped (no user account required)

## API Endpoints

| Method | Endpoint                          | Auth    | Description                     |
|--------|-----------------------------------|---------|---------------------------------|
| POST   | `/api/auth/register`              | Public  | Create account                  |
| POST   | `/api/auth/login`                 | Public  | Login, returns JWT              |
| POST   | `/api/jira/config`                | JWT     | Save Jira credentials           |
| GET    | `/api/jira/config`                | JWT     | Get connection status           |
| GET    | `/api/jira/projects`              | JWT     | List Jira projects              |
| POST   | `/api/jira/tickets`               | JWT     | Create NHI finding ticket       |
| GET    | `/api/jira/tickets/recent`        | JWT     | Last 10 app-created tickets     |
| POST   | `/api/keys`                       | JWT     | Create API key                  |
| GET    | `/api/keys`                       | JWT     | List API keys                   |
| DELETE | `/api/keys/{id}`                  | JWT     | Revoke API key                  |
| POST   | `/api/v1/findings`                | API Key | Create finding (external)       |

## External API Usage Example

```bash
# Create an API key via the UI, then:
curl -X POST http://localhost:5062/api/v1/findings \
  -H "X-Api-Key: ih_your-api-key-here" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Stale Service Account: svc-deploy-prod",
    "description": "Service account has not been rotated in 180 days",
    "projectKey": "NHI"
  }'
```

## Design Decisions

1. **SQLite** — Zero-config database. No external server needed for reviewers. EF Core makes it trivially swappable to PostgreSQL/SQL Server later.

2. **Data Protection API for Jira tokens** — Uses ASP.NET Core's built-in encryption framework rather than manual AES, providing key rotation and secure key storage out of the box.

3. **Dual authentication** — JWT for interactive users, API Key scheme for programmatic access. Implemented as separate authentication schemes that coexist cleanly.

4. **Multiple API keys per user** — Industry-standard pattern (GitHub, AWS, Stripe). Each key is named, independently revocable, and SHA-256 hashed before storage.

5. **Issue type "Task"** — NHI finding tickets are created as Jira Tasks. This is the most universally available issue type across Jira configurations.

6. **Recent tickets from local DB** — Instead of querying Jira's search API (which has rate limits and requires JQL), we track created tickets locally. This is faster and only shows tickets created through this app.

7. **Vite + React** — Modern, fast build tooling. Chosen over Create React App (deprecated) for better performance and active maintenance.

## Testing

```bash
cd IdentityHub.Api
dotnet test
```

Tests cover:
- **AuthService** — Registration, login, duplicate email, wrong password, non-existent user
- **ApiKeyService** — Key creation, validation, revocation, wrong user isolation

## Security

- Passwords: BCrypt hashed
- JWT: HMAC-SHA256 signed, 24h expiry
- Jira tokens: Encrypted at rest (Data Protection API)
- API keys: SHA-256 hashed before storage
- CORS: Restricted to frontend origin
- Global exception handler: Never leaks stack traces
- Input validation: Data annotations on all DTOs
