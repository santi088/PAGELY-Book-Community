# Admin Search/Filter/Sort/Paging Progress

## Existing backend changes
- ReportDto.BookTitle added via Review.Book join (ReportsController).
- Reports supports ?status filter; default CreatedAt desc.
- Reports now supports ?search, ?reason, ?sort, ?page, ?pageSize; returns PagedResult<ReportDto>.
- Reviews endpoint /api/admin/reviews supports page/pageSize.
- Users /api/admin/users, Genres /api/genres, Books /api/books (search/pageSize) exist.

## Shared components
- admin-web/js/components.js: renderToolbar(config), renderPagination(page,pageSize,total).
- admin-web/js/list-utils.js: debounce, createListStore(initial), bindListToolbar(containerEl, store, handlers), syncToolbarUI(containerEl, store).

## Per tab status (ALL COMPLETE)
- Reports: DONE - migrated to shared list-utils module, toolbar (search debounced, reason filter, sort: recent/oldest/reason/reviewer/book, reset), pagination, View modal shows Book title, filter state persists across modal open/close.
- Reviews: DONE - added sort (newest/oldest/highest/lowest/mostliked/book/reader), Reset button, status pills kept as custom HTML, rating filter in shared toolbar, state persists across actions.
- Users: DONE - search by email/name (debounced), role filter (from /api/admin/roles), suspended filter, sort (name/email/newest, asc/desc), pagination (page size 20), header count "X of Y", suspend/reactivate with self-protection.
- Books: DONE - genre filter (from /api/genres), visibility filter (All/Visible/Hidden), sort (newest/oldest/title/author/rating/reviewcount, asc/desc), reset, pagination (page size 24), Edit/Remove preserved, Open Library tab unchanged.
- Genres: DONE - search by name/description, sort (name A-Z/Z-A), pagination (page size 20), header count "X of Y", Add/Edit/Delete preserved.
- Dashboard: DONE - replaced with decision-oriented dashboard using GET /api/admin/dashboard endpoint.
  - A. Needs attention: pending reports + oldest pending age (warning >3 days), new reports 7d, hidden/removed reviews 7d.
  - B. Trends: 7/30-day toggle, new reviews/users per day (sparkline), reports by reason, resolved/dismissed/pending breakdown.
  - C. Catalog insights: books per genre (bar chart), zero-visible-book genres, top 5 most reviewed, top 5 highest rated, top 5 most saved, missing cover/description.
  - D. Moderation insights: top 5 most reported books, top 5 most reported reviewers.
  - E. Context strip: visible/hidden books, genres, active/suspended users, visible reviews.
  - Empty states, loading state, error isolation, captions under each section, admin-only guard.

## API changes
- /api/admin/dashboard: NEW endpoint returning all dashboard data in single response with aggregate queries (COUNT/GROUP BY). Supports ?days=7|30.
- /api/admin/users: added search, role, suspended, sort, page, pageSize params; returns PagedResult<UserDto> (backward compatible).
- /api/admin/roles: NEW endpoint returning distinct role names from DB.
- /api/books: added isActive filter param, added reviewcount sort option. Removed hardcoded IsActive=true filter.
- /api/genres: added search, sort, page, pageSize params; returns PagedResult<GenreDto> when paging params present.
- /api/admin/reviews: added search, status, rating, sort params.

## Frontend changes
- admin-web/js/list-utils.js: NEW shared module (debounce, createListStore, bindListToolbar, syncToolbarUI).
- admin-web/pages/dashboard.html: REPLACED with decision-oriented dashboard (Needs attention, Trends, Catalog insights, Moderation insights, Context strip).
- admin-web/pages/reports.html: migrated to shared module, added Harassment and Others reason filter options.
- admin-web/pages/users.html: rewritten with shared module.
- admin-web/pages/books.html: catalog tab uses shared module; Open Library tab unchanged.
- admin-web/pages/genres.html: rewritten with shared module.
- admin-web/pages/reviews.html: rewritten with shared module, status pills kept as custom HTML.

## Tests
- BookCommunity.Tests/AdminListFilterTests.cs: NEW - 9 tests covering Users (search/role/suspended/roles), Books (isActive/reviewcount sort), Genres (search/sort/paging), Reviews (status/rating/sort).
- BookCommunity.Tests/DashboardControllerTests.cs: NEW - 4 tests covering dashboard counts, empty database, days parameter, non-admin 403. All use in-memory database.
- All 42 tests pass (29 original + 9 list filter + 4 dashboard).

## Files served (admin-web static server on :8080)
- admin-web/pages/*.html - served directly (no build step)
- admin-web/js/*.js - ES modules loaded by HTML pages
- admin-web/css/styles.css - stylesheet
- API runs on :5242 (CORS allows localhost:8080)

## How to run
API: dotnet run --project BookCommunity.Api --urls "http://0.0.0.0:5242"
Admin web: python3 -m http.server 8080 (from admin-web/)
Tests: dotnet test BookCommunity.Tests/BookCommunity.Tests.csproj

## IMPORTANT: Hard-refresh (Ctrl+Shift+R) before testing UI changes - ES modules may be cached.

## Widgets skipped (data field missing)
- None - all entity fields needed exist (ApplicationUser.CreatedAt, Report.ModeratedAt, Report.Reason as string).

## NOT done (headless browser test)
- No Playwright/jsdom in project; not added per "no heavy new dependencies" rule. Manual verification recommended.
