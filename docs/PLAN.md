# Book Community Backend - Implementation Plan

## Project Goal
Build a production-minded backend for the student book-community application (separate from the existing PAGELY MAUI app). 
- ASP.NET Core Web API targeting .NET 10 (environment has .NET 8 SDK; will use .NET 8 if .NET 10 not available, but target per requirements)
- PostgreSQL database
- Entity Framework Core, Npgsql
- Authentication/Authorization
- Catalog, Discovery, Reviews/Ratings/Likes, Reading Lists, Reports/Moderation, Admin
- xUnit tests, Swagger/OpenAPI
- Full documentation

## Constraints
- Work only in /home/saint/Documents/PAGELY-Book-Community
- Do not touch /home/saint/Documents/PAGELY (MAUI app)
- No sudo/system package installs. If missing deps, document commands and continue with independent work
- No hardcoded secrets/creds
- Search/pagination in PostgreSQL, not in-memory
- DTOs only, no EF entities exposed
- Server-side authz/ownership
- Proper uniqueness/constraints/indexes
- Migrations via EF Core
- No pretending JWT logout invalidates without revocation

## Environment Notes (Batch 1 findings)
- Repo empty git repo (no commits). Branch main.
- dotnet SDK present at /home/saint/.dotnet (8.0.427). .NET 10 SDK not detected.
- psql/pg_isready not in PATH.
- PostgreSQL not obviously running/accessible from this environment.
- Will use .NET 8 if .NET 10 unavailable, but document deviation if needed.

## Batch Plan
1. **Batch 1 - Foundation**: Inspect tools/env, create solution/projects, configure API/DI/health/Swagger/error handling, write PLAN/PROGRESS/MAUI_INTEGRATION, build if possible. Commit.
2. **Batch 2 - DB Foundation**: Entities, EF Core/Npgsql, migrations, seeding, constraints/indexes. Document. Commit.
3. **Batch 3 - AuthN/Z**: Identity/JWT or suitable auth, roles, suspended enforcement, register/login/me/logout, validation. Commit.
4. **Batch 4 - Catalog/Discovery**: Books/Genres, search/filter/paginate in SQL, indexes, shelves (popular/top-rated/recent), recommendations. Commit.
5. **Batch 5 - Reviews/Likes**: Create/edit/delete own reviews, ratings 1-5, unique per user/book, likes, ownership, aggregates. Commit.
6. **Batch 6 - Reading Lists**: Statuses WantToRead/Reading/Finished, unique per user/book, filter/paginate. Commit.
7. **Batch 7 - Reports/Moderation/Admin**: Reports, queue, actions, user/book/genre moderation, soft-delete/deactivation policy. Commit.
8. **Batch 8 - Hardening/Handoff**: Tests, docs, verify contract, integration guide, final report. Commit.

## Milestones
- Runnable API skeleton with health/Swagger
- Migrations created (and applied if DB available)
- Core features implemented per spec
- Tests cover critical rules
- Complete docs (API, DB, README, PROGRESS, PLAN)
- Clear blockers documented
