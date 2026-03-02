# IdentityHub

A Non-Human Identity (NHI) management platform for security teams. Track and remediate machine identity risks — service accounts, API keys, service principals, and certificates — with native Jira integration and a REST API for automation.

## What It Does

- **Report NHI findings as Jira tickets** — create, edit, comment on, and transition tickets from the web portal
- **REST API for automation** — CI/CD pipelines and scanners can submit findings programmatically via API key auth
- **Per-user data isolation** — each user's Jira config, tickets, and API keys are fully private, even within the same organization
- **API key management** — generate, view, and revoke API keys from the portal; tickets created via the API appear in the portal

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
| Auth      | JWT (portal), API Key (external REST API)     |
| Jira      | Jira REST API v3 (Basic Auth with API Token)  |

## Running the Application

### Option 1: Docker Compose (Recommended)

```bash
docker compose up --build
```

- **Frontend:** http://localhost:3000
- **API:** http://localhost:5062
- **Swagger:** http://localhost:5062/swagger (dev mode only)

> **Note:** Update `Jwt__Key` in `docker-compose.yml` to a strong secret before deploying.

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
- **API:** http://localhost:5202
- **Swagger:** http://localhost:5202/swagger

### Getting Started (First Use)

1. **Register** — create an account at `/register`
2. **Connect Jira** — go to Jira Settings, enter your Jira email, API token, and site URL
3. **Create tickets** — select a project and submit NHI findings
4. **Generate an API key** — go to API Keys to create a key for programmatic access

## API Endpoints

| Method | Endpoint                          | Auth    | Description                          |
|--------|-----------------------------------|---------|--------------------------------------|
| POST   | `/api/auth/register`              | Public  | Create account                       |
| POST   | `/api/auth/login`                 | Public  | Login, returns JWT                   |
| POST   | `/api/jira/config`                | JWT     | Save Jira credentials                |
| GET    | `/api/jira/config`                | JWT     | Get connection status                |
| GET    | `/api/jira/projects`              | JWT     | List Jira projects                   |
| POST   | `/api/jira/tickets`               | JWT     | Create NHI finding ticket            |
| GET    | `/api/jira/tickets/recent`        | JWT     | Last 10 app-created tickets          |
| POST   | `/api/keys`                       | JWT     | Create API key                       |
| GET    | `/api/keys`                       | JWT     | List API keys                        |
| DELETE | `/api/keys/{id}`                  | JWT     | Revoke API key                       |
| POST   | `/api/v1/tickets`                 | API Key | Bulk create finding tickets (1–50)   |
| GET    | `/api/v1/tickets?projectKey={key}`| API Key | Get recent tickets for a project     |

## External REST API

The external API requires an `X-Api-Key` header. Generate a key from the portal under **API Keys**.

**Create tickets:**
```bash
curl -X POST http://localhost:5202/api/v1/tickets \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: YOUR_API_KEY" \
  -d '{
    "jiraEmail": "bot@company.com",
    "jiraApiToken": "ATATT3xFfGF0...",
    "jiraSiteUrl": "https://yoursite.atlassian.net",
    "tickets": [{
      "title": "Stale API key: prod-gateway",
      "description": "Last rotated 365 days ago.",
      "projectKey": "NHI",
      "priority": "High"
    }]
  }'
```

**Get recent tickets:**
```bash
curl http://localhost:5202/api/v1/tickets?projectKey=NHI \
  -H "X-Api-Key: YOUR_API_KEY" \
  -H "X-Jira-Email: bot@company.com" \
  -H "X-Jira-Api-Token: ATATT3xFfGF0..." \
  -H "X-Jira-Site-Url: https://yoursite.atlassian.net"
```

Tickets created via the API are linked to the API key's user account and visible in the web portal.

## Testing

```bash
cd IdentityHub.Api
dotnet test
```

Tests cover: authentication, password validation, Jira config isolation, user data isolation, findings controller, and security headers.

## Security

- Passwords: BCrypt hashed
- JWT: HMAC-SHA256 signed, 24h expiry
- Jira tokens: Encrypted at rest (ASP.NET Core Data Protection API)
- API keys: SHA-256 hashed, prefix-based lookup
- Per-user data isolation via UserId foreign keys on all entities
- CORS restricted to frontend origin
- Rate limiting on auth endpoints
- Global exception handler — never leaks stack traces
- Security headers (X-Content-Type-Options, X-Frame-Options, etc.)
- Input validation via Data Annotations on all DTOs
