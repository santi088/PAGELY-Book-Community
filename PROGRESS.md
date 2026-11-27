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
- admin-web/js/layout.js: renderLayout(), initLayout(), updateSidebarUserInfo(), escapeHtml().

## Per tab status (ALL COMPLETE)
- Reports: DONE - shared module, toolbar (search debounced, reason filter incl. Harassment/Others, sort, reset), pagination, reads ?status=?sort= from URL.
- Reviews: DONE - shared module, sort, Reset, status pills, rating filter, reads ?status= from URL.
- Users: DONE - search, role filter (/api/admin/roles), suspended filter, sort, pagination (20), header count, suspend/reactivate with self-protection.
- Books: DONE - genre filter, visibility filter, sort, reset, pagination (24), missingCoverOrDesc filter (reads ?filter=missingCoverOrDesc from URL), Edit/Remove preserved, Open Library tab unchanged.
- Genres: DONE - search, sort, pagination (20), header count, Add/Edit/Delete preserved.
- Dashboard: DONE - decision-oriented dashboard via GET /api/admin/dashboard endpoint.
  - A. Needs attention: pending reports + oldest age (warning >3 days), new reports 7d, hidden/removed reviews 7d, previous-period comparisons, cards are clickable links.
  - B. Trends: NEW grouped bar chart "New users and reviews per week" with Month/Year selector (disabled future months). Week buckets: 1-7, 8-14, 15-21, 22-28, 29-end. Reporting timezone Asia/Manila. Zero-filled weeks. Empty month shows axes + "No new users or reviews this month". SVG inline chart with accessibility (role=img, aria-label, hidden data table). Reports by reason and Report resolution side by side with counts + percentages.
  - C. Catalog insights: books per genre (top 8 + Other row, Other has no bar), zero-visible genres (chips link to Books), top 5 most reviewed/highest rated (min 3 reviews)/most saved, missing cover/description (chips + View all link).
  - D. Moderation insights: top 5 most reported books/reviewers (min 2 reports).
  - E. Context strip: visible/hidden books, genres, active/suspended users, visible reviews.
  - Empty states, loading state, error isolation, section captions, pluralization, Updated time + Refresh button, admin-only guard.
  - Card shadow polish: dashboard cards have soft shadow tinted from ink color (0 1px 2px rgba(ink,0.06), 0 6px 16px rgba(ink,0.10)), hover lift.

## API changes
- /api/admin/dashboard: NEW endpoint returning all dashboard data in single response with aggregate queries (COUNT/GROUP BY). Supports ?days=7|30. Zero-filled day ranges, thresholds (named constants), previous-period comparisons, percentages.
- /api/admin/dashboard/trends: NEW endpoint returning week-bucketed data for selected month/year. Supports ?year=YYYY&month=M. Week buckets: 1-7, 8-14, 15-21, 22-28, 29-end. Reporting timezone Asia/Manila (UTC+8) via DateTime.SpecifyKind normalization. Zero-filled weeks. Future months return zeros. Invalid year/month returns 400. earliestYear from earliest review or user.
- /api/admin/users: added search, role, suspended, sort, page, pageSize params; returns PagedResult<UserDto>.
- /api/admin/roles: NEW endpoint returning distinct role names from DB.
- /api/auth/me: NEW [HttpGet("me")] on GetCurrentUser so frontend can load user profile.
- /api/books: added isActive filter param, missingCoverOrDesc filter param, reviewcount sort option. Removed hardcoded IsActive=true filter.
- /api/genres: added search, sort, page, pageSize params; returns PagedResult<GenreDto> when paging params present.
- /api/admin/reviews: added search, status, rating, sort params.

## Frontend changes
- admin-web/js/list-utils.js: NEW shared module.
- admin-web/js/layout.js: renderLayout, initLayout, updateSidebarUserInfo, escapeHtml. Handles email/Email casing, updates sidebar dynamically.
- admin-web/js/auth.js: Fixed PascalCase/camelCase property handling in login/loadCurrentUser; loadCurrentUser non-fatal on failure.
- admin-web/pages/dashboard.html: REPLACED with polished dashboard (grouped bar chart, month/year selector, pluralization, card shadows, clickable cards, chips, percentages, refresh).
- admin-web/pages/reports.html: shared module, Harassment/Others reasons, reads ?status=?sort= from URL.
- admin-web/pages/users.html: shared module.
- admin-web/pages/books.html: shared module, missingCoverOrDesc filter, reads ?filter=missingCoverOrDesc from URL.
- admin-web/pages/genres.html: shared module.
- admin-web/pages/reviews.html: shared module, reads ?status= from URL.
- All pages: cache-busting v=7 on ES module imports.

## Tests
- BookCommunity.Tests/AdminListFilterTests.cs: NEW - 9 tests.
- BookCommunity.Tests/DashboardControllerTests.cs: NEW - 11 tests (counts, empty DB, days param, non-admin 403, zero-filled ranges, thresholds, previous-period, percentages, trend week bucketing, trend validation, trend earliestYear).
- All 57 tests pass (3 skipped due to test isolation). In-memory database only.

## Files served (admin-web static server on :8080)
- admin-web/pages/*.html - served directly
- admin-web/js/*.js - ES modules loaded by HTML pages
- admin-web/css/styles.css - stylesheet
- API runs on :5242 (CORS allows localhost:8080)

## How to run
API: dotnet run --project BookCommunity.Api --urls "http://0.0.0.0:5242"
Admin web: python3 -m http.server 8080 (from admin-web/)
Tests: dotnet test BookCommunity.Tests/BookCommunity.Tests.csproj

## IMPORTANT: Hard-refresh (Ctrl+Shift+R) before testing UI changes - ES modules may be cached.

## Widgets skipped
- None - all entity fields needed exist (ApplicationUser.CreatedAt, Report.CreatedAt, Report.Reason as string).

## NOT done (headless browser test)
- No Playwright/jsdom in project per "no heavy new dependencies" rule. Manual verification recommended.