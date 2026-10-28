# Windows Setup Guide (Backend)

This guide is for Windows 10/11. All commands are PowerShell-friendly. Run them from the directories specified.

## 1) Install .NET SDK
Download and install the .NET 8 SDK from the official site:
- https://dotnet.microsoft.com/download

Verify in PowerShell:
```powershell
dotnet --version
```
(Should be 8.x)

## 2) Install PostgreSQL
Download PostgreSQL (recommended 15+ or 16+) from:
- https://www.postgresql.org/download/windows/

During install set a postgres superuser password. Ensure the PostgreSQL service is running (Services app or `pg_ctl`).
Verify with pgAdmin or psql.

## 3) Create database and user
Using `psql` (as postgres superuser):
```powershell
psql -U postgres -h localhost -p 5432
```
In psql:
```sql
CREATE ROLE bookcommunity WITH LOGIN PASSWORD 'CHANGE_THIS_STRONG_PASSWORD';
CREATE DATABASE bookcommunity OWNER bookcommunity;
\c bookcommunity
GRANT ALL PRIVILEGES ON SCHEMA public TO bookcommunity;
\q
```

Or use pgAdmin: create role `bookcommunity` with login and password, create database `bookcommunity` owned by that role.

## 4) Configure connection string and secrets
From the repo root in PowerShell:
```powershell
cd .\BookCommunity.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=bookcommunity;Username=bookcommunity;Password=CHANGE_THIS_STRONG_PASSWORD"
dotnet user-secrets set "Jwt:Key" "REPLACE_WITH_A_LONG_RANDOM_SECRET_AT_LEAST_32_CHARS"
```

Alternatively set environment variables for the process:
```powershell
$env:ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=bookcommunity;Username=bookcommunity;Password=CHANGE_THIS_STRONG_PASSWORD"
$env:Jwt__Key="REPLACE_WITH_A_LONG_RANDOM_SECRET_AT_LEAST_32_CHARS"
```

Do **not** commit passwords/secrets. `appsettings.json` has empty placeholders.

## 5) EF Core tooling
Local tool manifest exists (`.config/dotnet-tools.json`). From repo root:
```powershell
cd <repo-root>
dotnet tool restore
```
Or use `dotnet ef` if installed globally.

## 6) Apply migrations (do not reset data)
From repo root:
```powershell
dotnet tool run dotnet-ef database update --project BookCommunity.Infrastructure --startup-project BookCommunity.Api
```
This applies existing migrations only. It does **not** guarantee data exists (see seeding notes).

## 7) Run the API
From repo root:
```powershell
dotnet run --project BookCommunity.Api
```
Check console output for URL (e.g., http://localhost:5242 or https://localhost:7xxx).

## 8) Test health endpoint
```powershell
curl http://localhost:5242/health
# or
Invoke-WebRequest -Uri http://localhost:5242/health
```
Expected: `Healthy`

## 9) Troubleshooting
- **Port conflicts**: Change port in `Properties/launchSettings.json` or use `--urls`
- **Connection errors**: Verify Postgres running, credentials, firewall
- **Migration errors**: Ensure DB exists and user has privileges
- **JWT errors**: Ensure `Jwt:Key` is set (long random string)
- **HTTPS redirect warnings**: Harmless on HTTP; can remove `app.UseHttpsRedirection()` for local HTTP-only dev

## Notes
- Creating DB vs applying migrations vs seeding: creating DB is step 3; applying migrations is step 6 (schema); seeding is app-internal (DbSeeder) and depends on environment/config - no automatic import is guaranteed.
- Do not reset/delete data.
