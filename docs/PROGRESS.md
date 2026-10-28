# Book Community Backend - Progress

## Current Status
- Batch 1 in progress (foundation)

## Environment Check (Batch 1)
- Repo: /home/saint/Documents/PAGELY-Book-Community (git, no commits)
- .NET SDK: 8.0.427 at /home/saint/.dotnet (no .NET 10 detected)
- PostgreSQL tools: psql/pg_isready not found in PATH
- PostgreSQL server: unknown/unreachable from current env
- PAGELY (MAUI): exists at /home/saint/Documents/PAGELY (not inspected per restrictions)

## Completed
- Created docs/PLAN.md
- Created docs/PROGRESS.md
- Created docs/MAUI_INTEGRATION.md (placeholder)

## In Progress
- Batch 1: Create solution/project structure, configure API foundation (health, Swagger, error handling), DI, config without hardcoded secrets

## Blockers / Notes
- .NET 10 target requested; SDK 8 present. Will target net8.0 unless .NET 10 becomes available. Document deviation.
- No PostgreSQL available. Will implement schema/migrations/seeding/tests to run without live DB where possible; migrations generated via EF Core tools conceptually but not applied. Document commands.

## Next
- Complete Batch 1 foundation, build (if possible), commit, move to Batch 2

## Batch 1 Completed (2026-10-28)
- Created solution with Api/Core/Infrastructure/Tests (net8.0; .NET 10 SDK not available)
- Added NuGet packages (EF Core 8, Npgsql, Identity, JWT Bearer, Swagger, test libs)
- Configured API with Controllers, Swagger with Bearer auth, Health checks, error handling baseline
- Added HealthController and /health endpoints
- Added .env.example, README.md
- Build succeeded
- Commit: feat: establish API foundation
- Moving to Batch 2: Database foundation

## Batch 2 In Progress
- Added Core entities (User, Book, Genre, BookGenre, Review, ReviewLike, ReadingListEntry, Report)
- Added Infrastructure DbContext with relationships, indexes, unique constraints
- Added DbSeeder for dev data and optional admin

## Batch 2 Completed (2026-10-28)
- Entities and relationships with constraints/indexes
- DbContext configured
- DbSeeder added
- Build succeeded
- Commit: feat: add database schema and migrations
- Note: No PostgreSQL available; migrations not applied. EF Core migration files can be generated with dotnet ef tools when needed.
- Moving to Batch 3: Authentication and authorization

## Batch 2 Completed (2026-10-28) - Final
- Entities and relationships with constraints/indexes
- DbContext configured with Identity
- Initial migration generated (InitialCreate) - not applied (no PostgreSQL)
- DbSeeder added
- Build succeeded
- Commit: feat: add database schema and migrations
- Moving to Batch 3: Authentication and authorization
- Moving to Batch 3: Authentication and authorization

## Batch 3 In Progress
- Implementing auth (register/login/me/logout), roles, JWT, suspended enforcement

## Batch 3 Completed (2026-10-28)
- Auth endpoints: register, login, me (/api/auth/me and /api/me), logout
- JWT auth, roles (Admin/User), Identity configured
- Suspended user enforcement
- DTOs, validation
- Build succeeded
- Commit: feat: implement authentication
- Moving to Batch 4: Catalog and discovery

## Batch 4 In Progress
- Implementing catalog, genres, search/filter/paginate, discovery shelves

## Batch 4 Completed (2026-10-28)
- Catalog listing with search (title/author), genre filter, sorting, pagination (DB-level)
- Book details
- Genres listing
- Discovery: popular/top-rated/recent with ranking rules
- Indexes in place
- Build succeeded
- Commit: feat: add catalog and discovery
- Moving to Batch 5: Reviews and likes

## Batch 5 In Progress
- Implementing reviews, ratings, likes, ownership, aggregates

## Batch 5 Completed (2026-10-28)
- Reviews CRUD with ownership enforcement
- Ratings 1-5, unique per user/book
- Likes/unlikes, unique per user/review
- Review listing for book and current user
- Book average rating and review count updates
- Build succeeded
- Commit: feat: implement reviews and likes
- Moving to Batch 6: Reading lists

## Batch 6 In Progress
- Implementing reading lists
