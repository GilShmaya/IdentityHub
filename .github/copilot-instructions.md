# IdentityHub — Copilot Instructions

## Project Overview

IdentityHub is a Non-Human Identity (NHI) management platform PoC. It allows users to track and manage service accounts, API keys, service principals, and other machine identities. The core feature is a Jira integration that lets users report NHI findings directly to their Jira workspace.

## Architecture

This project uses a **two-layer architecture** with clean separation:

```
IdentityHub/
├── backend/                  # ASP.NET Core Web API (.NET 8+)
│   └── IdentityHub.Api/      # Main API project
├── frontend/                 # React TypeScript SPA
├── docker-compose.yml
└── .github/
```

- **Backend:** ASP.NET Core Web API (C#), Entity Framework Core, SQLite
- **Frontend:** React with TypeScript (separate SPA, fully independent)
- **Database:** SQLite via EF Core (lightweight, no external DB needed)
- **App Auth:** JWT-based authentication (multi-user, stateless)
- **Jira Auth:** API Token — user provides their Jira email + API token + site URL (Basic Auth to Jira REST API v3)
- **External API Auth:** API key-based for programmatic access

## Development Principles

Always follow the conventions defined in `.github/dotnet-dev-principles.md` for all backend C# code. Key highlights:

- Write concise, idiomatic C# using C# 10+ features
- Use PascalCase for classes/methods/public members, camelCase for locals/private fields, UPPERCASE for constants
- Prefix interfaces with "I" (e.g., `IJiraService`)
- Use async/await for all I/O-bound operations
- Use Dependency Injection everywhere
- Follow RESTful API design with attribute routing
- Return consistent JSON error responses with appropriate HTTP status codes
- Use Data Annotations or Fluent Validation for model validation
- Use xUnit for unit tests, Moq or NSubstitute for mocking

## Domain Models

- **User** — Id, Email, PasswordHash (BCrypt)
- **JiraConfiguration** — UserId, Email, ApiToken (encrypted), SiteUrl. One per user.
- **TicketReference** — Id, UserId, JiraIssueKey, ProjectKey, Title, CreatedAt. Tracks tickets created via this app.
- **ApiKey** — Id, UserId, KeyHash, Name, CreatedAt, IsRevoked. For external API access.

## API Endpoints

### Authentication
- `POST /api/auth/register` — Create a new user account
- `POST /api/auth/login` — Login, returns JWT token

### Jira Configuration
- `POST /api/jira/config` — Save/update Jira credentials (email, API token, site URL)
- `GET /api/jira/config` — Get current Jira connection status (never return raw token)

### Jira Operations
- `GET /api/jira/projects` — List available Jira projects
- `POST /api/jira/tickets` — Create an NHI finding ticket (title, description, projectKey)
- `GET /api/jira/tickets/recent?projectKey={key}` — Get 10 most recent app-created tickets

### External API
- `POST /api/v1/findings` — API key-authenticated endpoint for external systems to create NHI finding tickets
- `POST /api/keys` — Create a new API key (JWT-authenticated)
- `DELETE /api/keys/{id}` — Revoke an API key
- `GET /api/keys` — List user's API keys

## Multi-Tenancy

- Each user has their own Jira configuration and data
- Data is isolated per user via the UserId foreign key on all models
- Jira API tokens are encrypted at rest (ASP.NET Core Data Protection API or AES)
- API keys are hashed before storage (like passwords)
- Never expose one user's data to another

## Frontend Guidelines

- Use React with TypeScript (strict mode)
- Organize code into: `components/`, `pages/`, `services/`, `types/`, `hooks/`
- Use React Router for navigation with protected routes
- Use Axios with JWT interceptor for API calls
- Store JWT token securely (httpOnly cookie preferred, or localStorage)
- Show clear, meaningful error messages to users
- Recent tickets view: each ticket is clickable and opens the corresponding Jira issue in a new browser tab

## Security Requirements

- All passwords hashed with BCrypt
- JWT tokens with appropriate expiration
- HTTPS enforced
- CORS configured for frontend origin only
- Jira credentials encrypted at rest
- API keys hashed before storage
- Global exception handling — never leak stack traces to clients
- Input validation on all endpoints

## Testing Strategy

- **Unit tests:** xUnit + Moq/NSubstitute for backend services
- **Integration tests:** WebApplicationFactory for API endpoint testing
- **Frontend tests:** Jest + React Testing Library for component tests

## Running the Project

Support both:
1. **Docker Compose:** `docker-compose up` runs both backend and frontend
2. **Manual:** `dotnet run` in backend/, `npm start` in frontend/
