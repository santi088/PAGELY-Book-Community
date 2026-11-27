# Admin Search/Filter/Sort/Paging Progress

## Existing backend changes
- ReportDto.BookTitle added via Review.Book join (ReportsController).
- Reports supports ?status filter; default CreatedAt desc.
- Reports now supports ?search, ?reason, ?sort, ?page, ?pageSize; returns PagedResult<ReportDto>.
- Reviews endpoint /api/admin/reviews supports page/pageSize.
- Users /api/admin/users, Genres /api/genres, Books /api/books (search/pageSize) exist.

## Shared components
- admin-web/js/components.js: renderToolbar(config), renderPagination(page,pageSize,total).
- No query-state hook yet (state in memory per page).

## Reports page
- Toolbar inserted (search, reason filter, sort) but filtering/sorting logic missing.
- Book column added.

## Per tab status
- Reports: DONE - toolbar wired (search debounced, reason filter, sort: recent/oldest/reason/reviewer/book, reset), pagination, View modal shows Book title, filter state persists across modal open/close.
- Reviews: TODO - search/filters/sort/paging not implemented.
- Users: TODO - search/filter/sort/paging not implemented.
- Genres: TODO - search/sort not implemented.
- Books: TODO - genre filter/sort not implemented.

## Real lists found
- Report reasons: hardcoded in admin-web (Misinformation, Inappropriate Content, Spam); no backend enum.
- User roles: Admin, User (DbSeeder).
- Genres: from /api/genres (DB table).

## Gotchas for next tabs
- Reuse renderToolbar/renderPagination pattern from Reports; they now accept searchValue, filter value, sortValue.
- State kept in memory per page (currentState object) - survive modal by not tearing down on view.
- Debounce search input (300ms) to avoid excessive API calls.
- PagedResult<T> shape: { items, page, pageSize, totalCount, totalPages }.
- Sort values must match backend switch: recent/oldest/reason/reviewer/book.
git add PROGRESS.md && git commit -m "docs: add admin search/filter progress" && echo done