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
- Dashboard: no changes.

## API changes
- /api/admin/users: added search, role, suspended, sort, page, pageSize params; returns PagedResult<UserDto> (backward compatible - old callers without paging params still work via new PagedResult shape).
- /api/admin/roles: NEW endpoint returning distinct role names from DB.
- /api/books: added isActive filter param, added reviewcount sort option. Removed hardcoded IsActive=true filter (now controlled by isActive param).
- /api/genres: added search, sort, page, pageSize params; returns PagedResult<GenreDto> when paging params present, plain list otherwise (backward compatible).
- /api/admin/reviews: added search, status, rating, sort params.

## Frontend changes
- admin-web/js/list-utils.js: NEW shared module (debounce, createListStore, bindListToolbar, syncToolbarUI).
- admin-web/pages/reports.html: migrated to shared module.
- admin-web/pages/users.html: rewritten with shared module.
- admin-web/pages/books.html: catalog tab uses shared module; Open Library tab unchanged.
- admin-web/pages/genres.html: rewritten with shared module.
- admin-web/pages/reviews.html: rewritten with shared module, status pills kept as custom HTML.

## Tests
- BookCommunity.Tests/AdminListFilterTests.cs: NEW - 9 tests covering Users (search/role/suspended/roles), Books (isActive/reviewcount sort), Genres (search/sort/paging), Reviews (status/rating/sort). All use in-memory database, never touch real Postgres.
- All 38 tests pass (29 original + 9 new).

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
