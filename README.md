# AuthService

[![CI](https://github.com/dennismorina/AuthService/actions/workflows/ci.yml/badge.svg)](https://github.com/dennismorina/AuthService/actions/workflows/ci.yml)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-336791)
![JWT](https://img.shields.io/badge/Auth-JWT-orange)
![Docker](https://img.shields.io/badge/Docker-ready-2496ED)
![License](https://img.shields.io/badge/License-MIT-green)

A production-oriented authentication and authorization sample focused on **JWT security, refresh-token rotation, RBAC, permission policies, abuse protection and security auditing**.

AuthService deliberately stays focused on identity and access control rather than becoming another generic CRUD API.

## What it demonstrates

- ASP.NET Core / .NET 10
- JWT access tokens
- Short-lived access tokens
- Opaque refresh tokens stored only as SHA-256 hashes
- Refresh-token rotation
- Refresh-token family revocation on reuse detection
- Optimistic concurrency protection for concurrent refresh attempts
- ASP.NET Core password hashing
- Password-strength policy
- Failed-login tracking and temporary account lockout
- IP-partitioned rate limiting for register, login and refresh endpoints
- Role Based Access Control
- Permission-based authorization policies
- Direct user permission grants
- Security audit events
- PostgreSQL 17
- Entity Framework Core migrations
- Docker / Docker Compose
- Unit and integration tests
- Real PostgreSQL security smoke test in GitHub Actions
- Dependabot

## Authorization model

```text
User
 ├── UserRoles
 │      └── Role
 │            └── RolePermissions
 │                    └── Permission
 │
 └── UserPermissions
        └── Permission
```

Roles group permissions, while direct user permissions can grant additional capabilities.

Built-in permissions:

```text
profile.read
admin.access
users.read
users.write
roles.read
roles.write
security-events.read
```

Built-in roles:

```text
User  -> profile.read
Admin -> all built-in permissions
```

Permissions are evaluated from PostgreSQL at authorization time rather than embedded into the JWT. Role or permission changes therefore take effect immediately without waiting for the current access token to expire.

## Token lifecycle

```text
Login / Register
      |
      +--> JWT access token (short-lived)
      |
      +--> opaque refresh token
               |
               v
            Refresh
               |
        old token marked used
               |
        new token in same family
```

Only the SHA-256 hash of a refresh token is persisted. The raw token is returned once to the client.

If an already-used refresh token is presented again, AuthService treats it as possible token theft and revokes the entire refresh-token family:

```text
Token A -> Token B -> Token C
   ^
   |
reuse detected
   |
   +--> revoke A, B and C
```

A concurrency token on refresh-token records also prevents two simultaneous refresh requests from silently creating two valid branches from the same token.

## Security controls

### Password hashing

Passwords are hashed with ASP.NET Core's `PasswordHasher<TUser>` and are never stored in plaintext.

### Login lockout

Default settings:

```text
5 failed attempts
15 minute lockout
```

Authentication failures return a generic response rather than exposing whether a specific account exists or is currently locked.

### Rate limiting

Default per-IP limits:

| Endpoint | Limit |
|---|---:|
| Register | 3 / minute |
| Login | 5 / minute |
| Refresh / Logout | 10 / minute |

Rate-limit rejection returns `429 Too Many Requests`.

### Security events

AuthService records security-relevant operations such as:

- successful registration
- successful and failed login
- account lockout
- successful refresh
- refresh-token reuse detection
- logout
- role changes
- direct permission changes
- role-permission changes

Passwords, JWTs and raw refresh tokens are never written to the audit log.

## Project structure

```text
AuthService
├── src
│   ├── AuthService.Api
│   ├── AuthService.Application
│   ├── AuthService.Domain
│   └── AuthService.Infrastructure
├── tests
│   ├── AuthService.UnitTests
│   └── AuthService.IntegrationTests
├── .github
│   ├── workflows
│   │   └── ci.yml
│   └── dependabot.yml
├── docker-compose.yml
├── AuthService.http
├── AuthService.sln
└── README.md
```

## API

### Authentication

| Method | Endpoint | Purpose |
|---|---|---|
| `POST` | `/api/auth/register` | Register and issue token pair |
| `POST` | `/api/auth/login` | Authenticate and issue token pair |
| `POST` | `/api/auth/refresh` | Rotate refresh token and issue new pair |
| `POST` | `/api/auth/logout` | Revoke the refresh-token family |
| `GET` | `/api/auth/me` | Return roles and effective permissions |

### Administration

| Method | Endpoint | Required permission |
|---|---|---|
| `GET` | `/api/admin/ping` | `admin.access` |
| `GET` | `/api/admin/users` | `users.read` |
| `PUT` | `/api/admin/users/{id}/roles` | `users.write` |
| `PUT` | `/api/admin/users/{id}/permissions` | `users.write` |
| `GET` | `/api/admin/roles` | `roles.read` |
| `PUT` | `/api/admin/roles/{role}/permissions` | `roles.write` |
| `GET` | `/api/admin/permissions` | `roles.read` |
| `GET` | `/api/admin/security-events` | `security-events.read` |

## Quick start with Docker

Requirements:

- Docker Desktop

Start PostgreSQL and the API:

```bash
docker compose up --build -d
```

Services:

| Service | Address |
|---|---|
| API | `http://localhost:8082` |
| Health | `http://localhost:8082/health` |
| Readiness | `http://localhost:8082/health/ready` |
| OpenAPI | `http://localhost:8082/openapi/v1.json` |
| PostgreSQL | `localhost:5435` |

Development bootstrap admin:

```text
Email:    admin@authservice.local
Password: AuthService_2026!
```

> The database credentials, bootstrap account and signing key in this repository are development-only values for the disposable local Docker environment. Real deployments must supply secrets externally.

Stop the environment:

```bash
docker compose down
```

Delete the local PostgreSQL volume too:

```bash
docker compose down -v
```

## Example login

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "admin@authservice.local",
  "password": "AuthService_2026!"
}
```

Response:

```json
{
  "accessToken": "...",
  "accessTokenExpiresAtUtc": "...",
  "refreshToken": "...",
  "refreshTokenExpiresAtUtc": "..."
}
```

Use the access token:

```http
GET /api/auth/me
Authorization: Bearer <access-token>
```

## Refresh-token rotation

```http
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "<refresh-token>"
}
```

The supplied token is consumed and replaced. Reusing that old token later revokes the complete token family.

## Database migrations

Database schema changes are versioned through committed Entity Framework Core migrations.

The initial schema is provided by:

```text
InitialCreate
```

On application startup, pending migrations are applied automatically using EF Core `MigrateAsync()`.

The migration history is stored in PostgreSQL's standard:

```text
__EFMigrationsHistory
```

This keeps database initialization reproducible across local Docker environments and CI.

## Testing

Run all tests:

```bash
dotnet test --solution AuthService.sln --configuration Release
```

Unit tests cover security-domain behavior including:

- password policy
- login lockout behavior
- refresh-token state transitions
- refresh-token hashing

Integration tests exercise the HTTP pipeline including:

- registration and authenticated profile access
- forbidden admin access for normal users
- admin permission access
- refresh-token rotation
- refresh-token reuse detection and family revocation
- generic authentication failures

## Continuous integration

Every push and pull request targeting `main` runs:

```text
Build & Test
     |
     v
PostgreSQL Auth Security Smoke Test
```

The Docker smoke test starts a real PostgreSQL 17 instance and AuthService, then verifies:

```text
Health / readiness
       |
Initial EF Core migration
       |
Admin login
       |
Authenticated /me request
       |
RBAC-protected admin endpoint
       |
Normal user receives 403
       |
Refresh token rotation
       |
Old-token reuse returns 401
       |
Replacement token also becomes invalid
```

This validates authentication, authorization, refresh-token family protection and database persistence against a real PostgreSQL instance.

## Technology stack

| Area | Technology |
|---|---|
| Language | C# |
| Runtime | .NET 10 |
| API | ASP.NET Core |
| Authentication | JWT Bearer |
| Password hashing | ASP.NET Core Identity PasswordHasher |
| Authorization | RBAC + permission policies |
| Database | PostgreSQL 17 |
| ORM | Entity Framework Core 10 |
| PostgreSQL provider | Npgsql EF Core 10 |
| Abuse protection | ASP.NET Core Rate Limiting |
| Testing | xUnit v3 |
| Coverage | Coverlet MTP |
| Containers | Docker / Docker Compose |
| CI | GitHub Actions |
| Dependency updates | Dependabot |

## Design goals

AuthService focuses on security behavior that is often missing from simple JWT examples:

- short-lived access tokens
- secure refresh-token storage
- token rotation
- token theft / reuse response
- concurrency-safe refresh behavior
- password hashing and upgrade support
- account lockout
- rate limiting
- roles and permissions
- immediate permission changes
- auditable security operations
- versioned database migrations
- real-database CI validation

The project is not intended to become a full OAuth 2.0 or OpenID Connect authorization server. Building a standards-compliant identity provider is intentionally outside the scope of this repository.

## License

MIT.