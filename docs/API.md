# API Documentation

Base URL: (configurable) - see README

## Authentication
- POST /api/auth/register
- POST /api/auth/login  
- GET /api/auth/me (or /api/me)
- POST /api/auth/logout

JWT Bearer token required for protected endpoints.

## Catalog
- GET /api/books - search, genre filter, sort, pagination
- GET /api/books/{id}
- GET /api/genres
- GET /api/discovery?shelf=popular|top-rated|recent

## Reviews
- GET /api/books/{id}/reviews
- POST /api/books/{id}/reviews (auth)
- PUT /api/reviews/{id} (auth, owner)
- DELETE /api/reviews/{id} (auth, owner)
- PUT /api/reviews/{id}/like (auth)
- DELETE /api/reviews/{id}/like (auth)
- GET /api/me/reviews (auth)

## Reading List
- GET /api/me/reading-list?status=WantToRead|Reading|Finished (auth)
- PUT /api/books/{id}/reading-list (auth)
- DELETE /api/books/{id}/reading-list (auth)

## Reports
- POST /api/reviews/{id}/reports (auth)

## Admin
- GET /api/admin/reports
- PUT /api/admin/reports/{id}/moderate
- GET /api/admin/users
- PUT /api/admin/users/{id}/suspend
- PUT /api/admin/users/{id}/reactivate
- POST /api/admin/books
- PUT /api/admin/books/{id}
- DELETE /api/admin/books/{id} (soft delete)
- POST /api/admin/genres
- PUT /api/admin/genres/{id}
- DELETE /api/admin/genres/{id}

All endpoints return appropriate status codes (200, 201, 204, 400, 401, 403, 404, 409). Errors use Problem Details where applicable.
