# Book Community Backend API

ASP.NET Core Web API (net8.0) for the Book Community application. PostgreSQL + EF Core + Identity + JWT.

## Prerequisites
- .NET 8 SDK
- PostgreSQL (for full functionality). If unavailable, see docs/PROGRESS.md.

## Configuration
Copy `.env.example` to environment variables or `appsettings.Development.json` (do not commit secrets).
Set:
- `ConnectionStrings:DefaultConnection`
- `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpireMinutes`
- Optional admin seed values

## Run
```bash
dotnet restore
dotnet build
dotnet run --project BookCommunity.Api
```
Swagger: https://localhost:7xxx/swagger (check console output)

## Migrations
```bash
dotnet ef migrations add <Name> --project BookCommunity.Infrastructure --startup-project BookCommunity.Api
dotnet ef database update --project BookCommunity.Infrastructure --startup-project BookCommunity.Api
```
Note: If PostgreSQL not available, migrations can be generated but not applied; see docs/PROGRESS.md.

## Testing
```bash
dotnet test
```
Some integration tests may require a running PostgreSQL instance.

## Documentation
- docs/API.md - API contract
- docs/DATABASE.md - Schema/relationships
- docs/PLAN.md - Plan
- docs/PROGRESS.md - Progress/blockers
- docs/MAUI_INTEGRATION.md - MAUI integration guide
