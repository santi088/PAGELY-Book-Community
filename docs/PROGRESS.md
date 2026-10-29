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

## Batch 6 Completed (2026-10-28)
- Reading list add/update/remove, list with pagination and status filter
- Unique per user/book enforced
- Build succeeded
- Commit: feat: implement reading lists
- Moving to Batch 7: Reports and administration

## Batch 7 In Progress
- Implementing reports, moderation, admin endpoints

## Batch 7 Completed (2026-10-28)
- Report creation, admin report queue, moderation (dismiss/resolve, hide review)
- User management (list, suspend/reactivate)
- Book/Genre CRUD for admins (books soft-deleted by deactivation)
- Build succeeded
- Commit: feat: add moderation and admin endpoints
- Moving to Batch 8: Hardening and handoff

## Batch 8 In Progress
- Tests, docs, hardening

## Batch 8 Completed (2026-10-28)
- Added API.md documentation
- Basic tests added and passing
- All batches completed
- Build succeeded, tests passed
- Commits created for each batch
- PostgreSQL not available in environment - migrations generated but not applied; commands documented
- Moving to final report

## Task 1: Windows Setup (2026-10-28)
- Created docs/WINDOWS_SETUP.md with PowerShell-friendly instructions (Postgres install, role/db creation, user-secrets/env, migrations, run, health check, troubleshooting)
- Updated README.md with link to Windows setup
- Verified on Fedora: dotnet build succeeds, dotnet test passes (1/1), EF Core tool available, API runs and /health returns Healthy
- Windows-specific commands not executed on Fedora (documented as requiring Windows testing)
- No secrets committed; configuration uses user-secrets/env vars

## Task 2: Admin Frontend (2026-10-28) - Batch 1-9
- Created admin-web/ with HTML/CSS/Vanilla JS (no build tools)
- API inventory in docs/ADMIN_API_INVENTORY.md
- Shared api.js, auth.js, layout.js
- Pages: login, dashboard, books, genres, users, reviews, reports
- Added CORS for http://localhost:8080 in API (small justified change)
- Documentation: admin-web/README.md, updated root README
- Tested: backend builds and tests pass
- No fake data; all features use real API endpoints where available

## Task 2b: Admin Seeding (2026-10-28)
- Wired DbSeeder.SeedAsync into Program.cs startup with scoped services (UserManager, RoleManager, ApplicationDbContext)
- Seeder creates Admin/User roles if missing, creates/promotes admin from Admin__Email/Admin__Password env vars, seeds genres/books if empty; safe to run repeatedly
- Updated docs/WINDOWS_SETUP.md and admin-web/README.md with exact PowerShell and Bash commands
- Build succeeded

## Task 3: Fix Admin Access Loop (2026-10-28)
- Fixed alert loops in admin frontend (removed repeated alerts, use inline errors/redirects)
- Fixed role checking to handle both camelCase/PascalCase
- Root cause likely: role claim type mismatch or missing claims in token; need to verify JWT claims

## Task 2c: Admin UI CRUD + Cleanup (2026-10-28)
- Removed 655 bin/obj files from git tracking (chore: remove bin/obj from git tracking)
- Implemented full CRUD for books (create/edit/delete via POST/PUT/DELETE /api/admin/books)
- Implemented full CRUD for genres (create/edit/delete via POST/PUT/DELETE /api/admin/genres)
- Implemented user suspend/reactivate (PUT /api/admin/users/{id}/suspend|reactivate)
- Implemented report moderation (PUT /api/admin/reports/{id}/moderate with dismiss/resolve/hide review)
- Backend already had POST /api/admin/books, PUT /api/admin/books/{id}, DELETE /api/admin/books/{id}, POST/PUT/DELETE /api/admin/genres
- Build and tests pass

## Task 4: Fix EF Core Migration Strategy (2026-10-28)

### Root Cause
`DbSeeder.SeedAsync()` called `context.Database.EnsureCreatedAsync()` on every application startup. This created tables based on the current model but bypassed the migration system. The migration `20261028110917_InitialCreate` was recorded in `__EFMigrationsHistory` as applied, but the `EnsureCreatedAsync()` path created the schema independently of the migration's `Up` method.

### Investigation Results
- `__EFMigrationsHistory` table EXISTS with `20261028110917_InitialCreate` recorded as applied
- All 10 tables exist: AspNetUsers, AspNetRoles, AspNetUserRoles, Books, Genres, BookGenres, Reviews, ReviewLikes, ReadingListEntries, Reports
- All 4 unique indexes exist: IX_Genres_Name, IX_Reviews_UserId_BookId, IX_ReviewLikes_UserId_ReviewId, IX_ReadingListEntries_UserId_BookId
- Data preserved: 2 users, 2 roles, 6 genres, 2 books, 0 reviews
- Database is fully consistent with `InitialCreate` migration

### Changes Made
1. **`BookCommunity.Infrastructure/Data/DbSeeder.cs`**: Removed `await context.Database.EnsureCreatedAsync();` from `SeedAsync()`. Seeding (roles, admin user, genres, books) continues to work via existence checks — no schema creation.
2. **Removed** `AddMissingUniqueIndexes` migration that was generated but not needed (all indexes already exist in DB).

### Verification
- `dotnet build`: 0 errors, 0 warnings ✓
- `dotnet test`: 1/1 passed ✓
- API starts successfully, `/health` returns `Healthy` ✓
- `/api/genres` returns all 6 genres ✓
- `/api/books` returns 2 books with correct data ✓
- No `EnsureCreatedAsync()` called on startup ✓
- Existing data preserved ✓

### Future Migration Workflow
- Create migration: `dotnet tool run dotnet-ef migrations add <Name> --project BookCommunity.Infrastructure --startup-project BookCommunity.Api`
- Apply migration: `dotnet tool run dotnet-ef database update --project BookCommunity.Infrastructure --startup-project BookCommunity.Api`
- Do NOT rely on `EnsureCreatedAsync()` for schema management

### Remaining Limitations
- Windows-specific commands not executed on this Fedora environment
- .NET 10 SDK not available; project targets net8.0

## Task 4b: Google Books Search Service (Batch 1) (2026-10-29)

### Goal
Read-only Google Books search — search external API and return normalized candidates. No import/persistence.

### Files Created
1. **`BookCommunity.Api/DTOs/Books/GoogleBookCandidateDto.cs`**: Normalized candidate DTO with GoogleBooksId, Title, Authors, Description, Isbn10, Isbn13, PublicationDate, PublicationYear, CoverImageUrl, Categories
2. **`BookCommunity.Api/Services/IGoogleBooksService.cs`**: Interface with `SearchAsync(string query, int pageSize, CancellationToken)`
3. **`BookCommunity.Api/Services/GoogleBooksService.cs`**: Implementation using `IHttpClientFactory`
   - Calls `https://www.googleapis.com/books/v1/volumes?q=...`
   - Validates query (null/empty/whitespace → empty list)
   - Clamps pageSize to 1-40 (default 20)
   - 15-second timeout on HttpClient
   - Handles missing/malformed fields gracefully (null/missing → defaults)
   - Extracts ISBN-10/ISBN-13 from industryIdentifiers
   - Parses publicationDate for year extraction
   - Prefers thumbnail over smallThumbnail for cover image
   - Returns empty list on HTTP errors, timeouts, JSON parse errors, exceptions (no internal details leaked)
   - Uses CancellationToken throughout

### Files Modified
4. **`BookCommunity.Api/Program.cs`**: Registered `AddHttpClient<IGoogleBooksService, GoogleBooksService>` with 15s timeout
5. **`BookCommunity.Api/Controllers/AdminController.cs`**: Added `IGoogleBooksService` dependency + `GET /api/admin/books/search?q=...&pageSize=20` endpoint (admin-only via controller-level `[Authorize(Roles = "Admin")]`)

### Tests Created (all passing)
6. **`BookCommunity.Tests/GoogleBooksServiceTests.cs`** (13 tests):
   - Normal response mapping (all fields)
   - Missing optional fields (minimal response)
   - Empty items array
   - Null/empty/whitespace query → empty list
   - PageSize clamping
   - Non-success status codes (500, 429)
   - HttpRequestException → empty list
   - TaskCanceledException (timeout) → empty list
   - Malformed JSON → empty list
   - Missing volume ID → skipped
   - Publication date format parsing (full date, year-only, malformed)
7. **`BookCommunity.Tests/AdminBooksSearchControllerTests.cs`** (4 tests):
   - Unauthenticated → 401
   - Non-admin role → 403
   - Admin, no query → 400
   - Admin, with query → not 401/403

### Verification
- `dotnet build`: 0 errors, 0 warnings ✓
- `dotnet test`: 17/17 passed ✓

### Endpoint Usage
```
GET /api/admin/books/search?q=harry+potter&pageSize=10
Authorization: Bearer <jwt-token>
```

### Limitations
- No Google API key required (anonymous requests have rate limits)
- No import/persistence yet (read-only search only)
- Windows-specific commands not tested on Fedora
- .NET 10 SDK not available; project targets net8.0

## Task 4c: Google Books Import Persistence (Batch 2) (2026-10-29)

### Goal
Allow administrators to import a selected Google Books search result into PostgreSQL.

### Database Migration
- **Generated**: `20261029024518_AddBookProviderFields`
- **Applied**: Yes, to configured development database
- **Changes**:
  1. Added `ExternalId` (varchar(100), nullable) column to `Books`
  2. Added `ExternalProvider` (varchar(50), nullable) column to `Books`
  3. Created unique index `IX_Books_ExternalProvider_ExternalId` with filter `"ExternalProvider" IS NOT NULL AND "ExternalId" IS NOT NULL`
  4. Created unique index `IX_Books_ISBN` with filter `"ISBN" IS NOT NULL`
- **Safety verified**: No duplicate non-null ISBNs existed before migration; all existing data preserved (2 users, 2 roles, 6 genres, 2 books)

### Files Created
1. **`BookCommunity.Api/DTOs/Books/ImportBookDtos.cs`**: `ImportBookRequestDto` (GoogleBooksId), `ImportBookResultDto` (BookId, Title, Author, IsNewImport, ExistingIsbnMatch, Message)
2. **`BookCommunity.Tests/AdminBooksImportControllerTests.cs`** (9 tests):
   - 401 unauthenticated
   - 403 non-admin
   - 400 missing provider ID
   - 404 provider not found
   - Creates new book when no duplicate
   - Returns existing when duplicate provider ID
   - Returns existing ISBN match with ExistingIsbnMatch
   - Imports without ISBN
   - Reuses existing genre without duplicate

### Files Modified
3. **`BookCommunity.Core/Entities/Book.cs`**: Added `ExternalProvider` (string?, max 50) and `ExternalId` (string?, max 100)
4. **`BookCommunity.Infrastructure/Data/ApplicationDbContext.cs`**: Added unique indexes on `(ExternalProvider, ExternalId)` and `ISBN` (both filtered for non-null)
5. **`BookCommunity.Api/Services/IGoogleBooksService.cs`**: Added `GetVolumeAsync(string volumeId, CancellationToken)`
6. **`BookCommunity.Api/Services/GoogleBooksService.cs`**: Implemented `GetVolumeAsync` - fetches single volume from `https://www.googleapis.com/books/v1/volumes/{id}`, reuses `MapVolumeInfo`
7. **`BookCommunity.Api/Controllers/AdminController.cs`**: Added `POST /api/admin/books/import` endpoint with full import logic:
   - Validates provider ID
   - Fetches volume from Google Books (authoritative metadata)
   - Checks existing by (ExternalProvider, ExternalId)
   - Checks existing by ISBN (Isbn13 preferred, then Isbn10)
   - Creates new book with mapped metadata
   - Maps genres/categories - reuses existing genres, creates new if needed
   - Returns `ImportBookResultDto` with IsNewImport flag

### Files Created (Tests)
8. **`BookCommunity.Tests/GoogleBooksServiceTests.cs`**: Added 5 `GetVolumeAsync` tests (valid volume, not found, missing volumeInfo, empty ID, HTTP error)

### Verification
- `dotnet build`: 0 errors, 0 warnings ✓
- `dotnet test`: 31/31 passed ✓ (17 previous + 14 new)
- Migration `AddBookProviderFields` generated and applied ✓
- Database schema consistent with migration ✓
- Existing data preserved ✓

### Endpoint Usage
```
POST /api/admin/books/import
Authorization: Bearer <jwt-token>
Content-Type: application/json

{ "googleBooksId": "vol123" }
```

### Limitations
- No Google API key required (anonymous requests have rate limits)
- Windows-specific commands not tested on Fedora
- .NET 10 SDK not available; project targets net8.0

## Task 4d: Google Books Import UI (Batch 3) (2026-10-29)

### Goal
Connect admin books page to Google Books search and import endpoints.

### Files Modified
1. **`admin-web/pages/books.html`**: Added Import from Google Books section:
   - Search input + Search button calling `GET /api/admin/books/search?q=...`
   - Candidate grid showing title, author, publication date/year, ISBN, cover image, description
   - Missing cover images handled gracefully (placeholder div)
   - Missing/incomplete metadata handled (falls back to "Unknown" or "Not available")
   - Import button per candidate → `POST /api/admin/books/import` with `{ googleBooksId }`
   - States: loading, empty-results, success (new import), duplicate-existing-book (provider ID match), duplicate-existing-book (ISBN match), error
   - Refreshes book list after successful import via `loadBooks(1)`
   - Prevents duplicate submissions with `isImporting` flag
   - All API-supplied data rendered via `escapeHtml()` (no innerHTML of untrusted data)
   - Cover image URLs validated with `^https?:\/\/` regex before using as `src`
   - Existing create/edit/delete/filter/pagination behavior unchanged

### Verification
- `dotnet build`: 0 errors, 0 warnings ✓
- `dotnet test`: 31/31 passed ✓ (unchanged from Batch 2)
- Frontend changes are HTML/JS only - no .NET compilation required
- Browser testing not performed (static frontend, requires running backend + frontend server)

### Endpoint Flow
```
1. GET /api/admin/books/search?q=harry+potter&pageSize=20  (returns GoogleBookCandidateDto[])
2. POST /api/admin/books/import  body: { googleBooksId: "vol123" }  (returns ImportBookResultDto)
   - IsNewImport=true → newly created book
   - IsNewImport=false + ExistingIsbnMatch → existing book by ISBN
   - IsNewImport=false + no ExistingIsbnMatch → existing book by provider ID
```

### Limitations
- Browser testing not performed (requires running backend API on localhost:5242 + frontend server on port 8080)
- No .NET build/test impact from frontend changes

## Task 4: End-to-End Verification and Bug Fixes (Batch 4) (2026-10-29)

### Bug Fixed
- **Root cause**: Backend serialized JSON with PascalCase (default System.Text.Json), but frontend expected camelCase property names (`result.isNewImport`, `result.title`, `candidate.googleBooksId`, etc.). Frontend could never read response properties correctly.
- **Fix**: Added `AddJsonOptions` with `JsonNamingPolicy.CamelCase` to `Program.cs`. All API responses now use camelCase, matching frontend expectations.
- **Verification**: Catalog response confirmed using camelCase properties (`id`, `title`, `author`, etc.)

### Automated Verification (all passed)
- `dotnet build`: 0 errors, 0 warnings ✓
- `dotnet test`: 31/31 passed ✓
- Search without auth → 401 ✓
- Import without auth → 401 ✓
- Search with User role → 403 ✓
- Import with User role → 403 ✓
- Catalog public endpoint → 200 ✓
- Genres public endpoint → 200 ✓
- Google Books API rate limiting (429) handled gracefully → empty results ✓

### End-to-End Workflow (live API testing)
- Started API on http://127.0.0.1:5242 with Admin__Email/Admin__Password env vars
- Registered testadmin@test.com, promoted to Admin role via direct SQL (non-destructive)
- Login successful, JWT contains both "User" and "Admin" roles
- Health endpoint: "Healthy" ✓
- Google Books search: returns empty (API rate-limited at 429, handled gracefully) ✓
- Admin auth enforcement verified on both search and import endpoints ✓
- Existing catalog and discovery endpoints unaffected ✓

### Browser Testing
- **NOT performed** — no browser (Firefox, Chrome, Chromium) available in this environment
- Frontend changes are HTML/JS only, no .NET compilation impact
- Manual browser testing requires: backend on localhost:5242 + frontend server on port 8080

### Test Data Created During Verification
- testadmin@test.com (Admin role) — registered via API, promoted via SQL
- regularuser@test.com (User role) — registered via API
- Test books imported during unit test runs (in database)
- These are test-only accounts, not existing user data

### Files Changed (Batch 4)
- `BookCommunity.Api/Program.cs` — Added camelCase JSON serialization configuration

### Remaining Limitations
- Browser workflow not verified (no browser available)
- Google Books API rate-limited in this environment (429) — full import flow not end-to-end tested against live API
- Unit tests with mocked HTTP cover all import scenarios comprehensively (31/31 passing)

## Task 4e: Admin Dashboard Interaction Fixes (Batch 5) (2026-10-29)

### Root Cause
All admin HTML pages (`books.html`, `genres.html`, `users.html`) use `<script type="module">`. In ES modules, all functions and variables are scoped to the module and are **NOT** added to the global `window` object. However, inline HTML event handlers like `onclick="editGenre('${id}')"` resolve function names against `window`. Since all handler functions were module-scoped, they were inaccessible from inline handlers, causing `Uncaught ReferenceError: editGenre is not defined` (and equivalent errors for every inline handler).

### Bugs Fixed
- **Books tab**: Next/Previous pagination, Edit, Delete, Add Book, Cancel, Save, Google Books Search, Import Book — all inline handlers broken
- **Genres tab**: Add Genre, Cancel, Save Genre, Edit, Delete — all inline handlers broken
- **Users tab**: Suspend, Reactivate — all inline handlers broken

### Fix Approach
Replaced all inline `onclick`/`onsubmit` handlers with `data-action`/`data-id`/`data-page`/`data-title` attributes and attached event listeners via `addEventListener` after each `content.innerHTML = html` render call:

1. **`admin-web/pages/books.html`**: Added `attachBookListeners(content)` — attaches listeners for show/hide form, save (form submit), edit, delete, prev/next page, Google Books search, import book
2. **`admin-web/pages/genres.html`**: Added `attachGenreListeners(content)` — attaches listeners for show/hide form, save (form submit), edit, delete
3. **`admin-web/pages/users.html`**: Added `attachUserListeners(content)` — attaches listeners for suspend, reactivate

### Verification
- `dotnet build`: 0 errors, 0 warnings ✓
- `dotnet test`: 31/31 passed ✓ (unchanged — frontend-only fix)
- Browser testing **NOT performed** — no browser available in this environment
- Manual browser testing requires: backend on localhost:5242 + frontend server on port 8080

### Files Changed
- `admin-web/pages/books.html` — Replaced inline handlers with data-action attributes + event listener attachment
- `admin-web/pages/genres.html` — Replaced inline handlers with data-action attributes + event listener attachment
- `admin-web/pages/users.html` — Replaced inline handlers with data-action attributes + event listener attachment

### Remaining Limitations
- Browser workflow not verified (no browser available)
- All fixes verified syntactically and by code review; event listener attachment follows the same pattern used successfully in `layout.js`

## Task 4f: Fix Remaining Bugs (2026-10-29)

### Priority 1 — User suspension
- **Investigation**: Traced end-to-end via live API (admin token). `PUT /api/admin/users/{id}/suspend` returns 200 OK and `PUT /api/admin/users/{id}/reactivate` returns 200 OK. Backend `AdminController` has no self-suspension guard; the API permits suspending/reactivating any user (including self) as long as caller is admin. Login returns 403 for suspended users (AuthController/MeController enforce suspension). The previous instruction said "do not suppress errors, fake success, weaken authorization, or remove existing self-suspension protections" - no such protections exist in current source. To improve UX and prevent accidental self-lockout, admin-web now prevents self-suspension/reactivation in the UI (disables/hides actions for current user) and loads current user before rendering users list. API behavior unchanged.
- **Verification**: Suspended `fcf4262b-78a3-40a5-bbb0-90dec7ec7e19` via API → 200, reactivated → 200. Suspended `testadmin@test.com` via API → 200, subsequent login returns 403 (confirmed). Reactivated via DB/script → login restored. All operations use admin JWT and correct endpoints.

### Priority 2 — Book search
- **A) Search existing books**: Added "Search existing books" section to `admin-web/pages/books.html`. Uses existing `GET /api/books` with `search` query parameter (confirmed from `BooksController`: accepts `search`, filters title/author case-insensitively). Search submits to same list, resets to page 1, preserves pagination/Edit/Delete controls, clears restores full catalog.
- **B) Search Google Books to import**: Section labeled "Import from Google Books" remains separate. Frontend calls `GET /api/admin/books/search?q=...&pageSize=20` with admin auth (api client attaches Bearer token). Backend returns `[]` when Google Books returns non-success (rate limited/429) or no items; Google Books API currently rate-limited (429) in this environment so results return empty arrays. Frontend now handles 429 explicitly with message "Google Books rate limit exceeded. Please try again later." Import uses `POST /api/admin/books/import` with `{ googleBooksId }` and refreshes catalog on success. Auth enforced (401/403) as tested.

### Priority 3 — Modal dialogs for Genre and Book
- **Genre modal**: Replaced appended form with centered modal + backdrop. Add opens blank form, Edit populates form, Save/Cancel/Close/Escape work, validation/API errors shown inside modal, success closes modal and refreshes list, Delete unchanged.
- **Book modal**: Replaced appended form with centered modal + backdrop. Edit opens modal with populated fields, preserves existing fields/validation, Save uses existing API contract, Cancel/Close/Escape/backdrop dismiss without saving, success closes modal and refreshes catalog, API failures visible.

### Verification
- `dotnet build`: 0 errors, 0 warnings ✓
- `dotnet test`: 31/31 passed ✓
- API verified: auth enforcement, suspension/reactivation, local catalog search (title/author), Google Books search returns 200 with `[]` due to external rate limiting (429 from Google) — handled gracefully
- Browser UI changes implemented per requirements; no browser available in environment for interactive testing

### Files Changed
- `admin-web/pages/users.html` — Prevent self-suspension in UI, load current user
- `admin-web/pages/genres.html` — Modal dialog for add/edit with backdrop/escape/close
- `admin-web/pages/books.html` — Modal dialog for add/edit, local catalog search + clear, improved Google Books error handling

### Root Causes (summary)
- **User suspension "does not work"**: API works correctly; no backend self-suspension protection existed. UI now prevents accidental self-suspension.
- **Local catalog search missing**: No UI control existed; added search using existing `GET /api/books?search=`.
- **Google Books search issues**: Frontend/backend integration correct with auth; external API is rate-limited (429) causing empty results — now surfaced clearly to user.

## Task 4g: Fix Books tab crash (2026-10-29)

### Root Cause
`admin-web/pages/books.html` had a syntax error causing `Uncaught SyntaxError: Unexpected end of input (at books.html:543:5)`. The issues were:
1. When adding local catalog search handlers/functions, `clearCatalogSearch()` was left with a missing closing brace and its body was concatenated into `searchGoogleBooks()` (function declaration inside function body / unclosed block) causing unbalanced braces.
2. A regex/string escaping issue in `renderImportCandidates()` (`/^https?:\\/\\//i.test(...)`) also caused parsing/tokenization problems.

### Fix
- Corrected `clearCatalogSearch()` to properly close its function and call `loadBooks(1)`.
- Repaired `renderImportCandidates()` cover URL check to avoid problematic escaping.
- Ensured all braces/brackets/template literals balanced; file ends correctly at script close.

### Verification
- Braces/parens/brackets balanced, template literals balanced, Python tokenizer compiles the script content successfully.
- `dotnet build`: 0 errors, 0 warnings ✓
- `dotnet test`: 31/31 passed ✓
- Browser testing: not performed (no browser available in environment). Genres and Users tabs unchanged and were working before.

## Task 4h: Fix duplicate attachBookListeners (2026-10-29)

### Root Cause
`admin-web/pages/books.html` had `function attachBookListeners(content)` declared twice (duplicate). The second declaration caused `Uncaught SyntaxError: Identifier 'attachBookListeners' has already been declared (at books.html:407:9)`, crashing the Books tab. This was introduced during edits.

### Fix
Removed the duplicate `attachBookListeners` function definition, keeping the first one with modal/Escape/backdrop and catalog/Google Books search handlers intact. All Books functionality preserved.

### Verification
- Only one declaration remains. Braces/brackets/backticks balanced, script tokenizes cleanly.
- `dotnet build`: 0 errors, 0 warnings ✓
- `dotnet test`: 31/31 passed ✓
- Browser testing: not performed (no browser available). Genres/Users unchanged.

## Task 4i: Fix TDZ error for currentCatalogPage (2026-10-29)

### Root Cause
In ES modules, `let`/`const` declarations are not hoisted in a usable form; `loadBooks()` was referenced in a top-level `await` block before `currentCatalogPage`/`currentCatalogSearch` were declared, causing `ReferenceError: Cannot access 'currentCatalogPage' before initialization`.

### Fix
Moved `currentCatalogPage` and `currentCatalogSearch` declarations to the top of the module script (before they are used). Preserved all Books functionality.

### Verification
- Syntax/balance checks pass.
- `dotnet build`: 0 errors, 0 warnings ✓
- `dotnet test`: 31/31 passed ✓
- API returns books (76 total). Browser testing: not performed (no browser available in environment).

## Task 4j: Fix missing importCandidates variable (2026-10-29)

### Root Cause
`importCandidates`, `isImporting`, and `editingBookId` were used throughout the Books page script but not declared, causing `ReferenceError: importCandidates is not defined`.

### Fix
Declared the variables at module scope alongside other state variables.

### Verification
- Braces balanced, syntax checks pass.
- `dotnet build`: 0 errors, 0 warnings ✓
- `dotnet test`: 31/31 passed ✓
- Browser testing: not performed (no browser available).

## Task 4k: Google Books robustness + dashboard modernization (2026-10-29)

### Google Books root cause
`GoogleBooksService` swallowed every upstream failure (429, 403, timeout, network, malformed JSON) and returned an empty list/null. The admin endpoint therefore returned HTTP 200 with `[]`, so the UI could not distinguish an actual empty result from a failed request and showed misleading empty results. Frontend also lacked loading/disabled/empty/error/retry/stale-response handling.

### Implementation
Backend:
- Added `GoogleBooksException` with status code and user-facing message.
- `GoogleBooksService.SearchAsync`/`GetVolumeAsync` now throw on upstream failures; 404 remains a genuine not-found for import.
- `AdminController` maps `GoogleBooksException` to the appropriate HTTP status/message while preserving auth/authz.
- Search returns proper error responses instead of 200 + empty array.

Frontend:
- Added request-id protection against stale responses, duplicate-submission protection, Enter-to-search, Clear, Retry, loading/disabled button state, distinct empty vs error states, per-book import loading, duplicate/import feedback, and catalog refresh after import.
- Improved Google Books candidate cards with cover fallbacks and compact metadata.

Modernization:
- Rewrote shared admin CSS with design tokens, responsive layout, improved tables/forms/modals/buttons/badges/alerts/loading/empty states.
- Updated layout with active navigation, page headers/descriptions, responsive sidebar.
- Applied table wrappers and modal improvements to Books/Genres/Users pages.

### Tests
- Updated `GoogleBooksServiceTests` for successful, empty, 429/403/timeout/network/malformed behavior.
- `dotnet build`: 0 errors, 0 warnings.
- `dotnet test`: 31/31 passed.
- Frontend script syntax checked with Python tokenizer for all admin pages: all passed.

### Browser/live verification
- No browser available in this environment.
- Live Google Books request tested once via curl and correctly returned HTTP 429 with a user-facing message; repeated upstream calls were avoided.
- Mocked tests prove successful/error handling, but do not prove Google's live service is currently accessible.

### Boundaries
- Work stayed in `~/Documents/PAGELY-Book-Community`.
- Database was not reset/dropped/reseeded.
- No MAUI files were touched.
- No commit or push occurred.

## Task 4l: 429 evidence and visible UI remediation (2026-10-29)

### Google Books 429 evidence
- Exact upstream URL: `https://www.googleapis.com/books/v1/volumes?q=<encoded query>&maxResults=20`
- Query is encoded with `Uri.EscapeDataString`.
- Upstream response: HTTP 429, no `Retry-After` header.
- Sanitized body reason: `rateLimitExceeded`; status `RESOURCE_EXHAUSTED`; quota metric `Queries`, limit `Queries per day`, consumer `project_number:624717413613`, `quota_limit_value: 0`.
- No Google Books API key is configured in appsettings/user-secrets/environment.
- Duplicate upstream requests were not found in the intended search flow: search runs only on explicit submit/Enter, and import performs one volume lookup. Accumulated document-level Escape listeners were fixed. In-flight guards remain for search and import.
- Implemented optional API-key support via `GoogleBooks:ApiKey` / `GoogleBooks__ApiKey`, plus `Retry-After` handling when present. This cannot override Google's exhausted default daily quota; live search remains blocked until a valid key/quota or reset is available.

### UI remediation
- Added backward-compatible CSS variable aliases so existing inline styles continue to resolve after the token rename.
- Added cache-busting query string to stylesheet links.
- Fixed accumulated Escape-key listeners.
- Headless Firefox was used to render `books.html`; no interactive visual inspection was possible in this environment.

### Verification
- `dotnet build`: 0 errors, 0 warnings.
- `dotnet test`: 31/31 passed.
- Frontend script tokenizer checks: all passed.

## Task 4m: Replace Google Books with Open Library (2026-10-29)

### Implementation
- Removed Google Books service, exception, interface, and DTO; added Open Library equivalents.
- Backend search uses `https://openlibrary.org/search.json` with required fields, URL-encoded query, page-size validation, and PAGELY User-Agent.
- Import reuses provider/ISBN duplicate checks with provider `OpenLibrary`; stores edition/work identifiers; fetches edition, work, and author metadata as needed.
- Frontend section renamed to "Import from Open Library"; candidate property changed to `openLibraryId`.
- `ImportBookRequestDto.GoogleBooksId` renamed to `OpenLibraryId`.

### Live verification
- Real Open Library search succeeded (`numFound: 461` for "the hobbit").
- Real edition fetch succeeded and author resolution succeeded (J.R.R. Tolkien).
- Import was not executed against the live database to avoid unnecessary catalog mutation; import persistence/duplicate handling is covered by mocked tests.

### Tests
- Replaced Google service tests with Open Library service tests.
- `dotnet build`: 0 errors, 0 warnings.
- `dotnet test`: 28/28 passed.
- Frontend script tokenizer checks: all passed.

### Boundaries
- Work remained in `~/Documents/PAGELY-Book-Community`.
- Database was not reset/dropped/reseeded.
- No MAUI files were touched.
- No commit or push occurred.
