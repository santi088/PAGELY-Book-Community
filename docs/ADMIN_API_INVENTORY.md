# Admin API Inventory

Base URL: http://localhost:5242 (dev)
Auth: JWT Bearer tokens (Authorization: Bearer <token>). Roles from Identity (User, Admin). Admin endpoints require [Authorize(Roles = "Admin")].

## Auth
| Method | Endpoint | Auth | Request | Response | Notes |
|---|---|---|---|---|---|
| POST | /api/auth/register | None | RegisterDto { Email, Password, FirstName?, LastName? } | AuthResponseDto { Token, Email, UserId, ExpiresAt } | Assigns User role, returns token |
| POST | /api/auth/login | None | LoginDto { Email, Password } | AuthResponseDto { Token, Email, UserId, ExpiresAt } | Returns 401 if invalid, 403/Forbid if suspended |
| GET | /api/auth/me | Bearer | - | UserDto { Id, Email, FirstName, LastName, IsSuspended, Roles[] } | Also available at /api/me |
| POST | /api/auth/logout | Bearer | - | { message } | Client-side token removal (JWT stateless) |

## Catalog/Discovery
| Method | Endpoint | Auth | Request | Response | Notes |
|---|---|---|---|---|---|
| GET | /api/books | None | search?, genre?, sortBy?, sortOrder?, page?, pageSize? | PagedResult<BookDto> | DB-level pagination/search |
| GET | /api/books/{id} | None | - | BookDto | Active books only |
| GET | /api/genres | None | - | List<GenreDto> | All genres |
| GET | /api/discovery | None | shelf=popular\|top-rated\|recent, page?, pageSize? | PagedResult<BookDto> | Ranking rules per spec |

## Reviews
| Method | Endpoint | Auth | Request | Response | Notes |
|---|---|---|---|---|---|
| GET | /api/books/{bookId}/reviews | Optional? | page?, pageSize? | PagedResult<ReviewDto> | Public listing |
| POST | /api/books/{bookId}/reviews | Bearer | CreateReviewDto { Rating(1-5), ReviewText? } | ReviewDto (201) | One review per user/book; 409 if exists |
| PUT | /api/reviews/{id} | Bearer (owner) | UpdateReviewDto { Rating, ReviewText? } | ReviewDto | Owner only |
| DELETE | /api/reviews/{id} | Bearer (owner) | - | 204 | Soft-deleted (IsDeleted) |
| PUT | /api/reviews/{id}/like | Bearer | - | { message } | 409 if already liked |
| DELETE | /api/reviews/{id}/like | Bearer | - | 204 | Removes like |
| GET | /api/me/reviews | Bearer | page?, pageSize? | PagedResult<ReviewDto> | Current user's reviews |

## Reading List
| Method | Endpoint | Auth | Request | Response | Notes |
|---|---|---|---|---|---|
| GET | /api/me/reading-list | Bearer | status?, page?, pageSize? | PagedResult<ReadingListDto> | Filter by status |
| PUT | /api/books/{bookId}/reading-list | Bearer | UpdateReadingListDto { Status } | 200/201 | Add or update |
| DELETE | /api/books/{bookId}/reading-list | Bearer | - | 204 | Remove |

## Reports
| Method | Endpoint | Auth | Request | Response | Notes |
|---|---|---|---|---|---|
| POST | /api/reviews/{reviewId}/reports | Bearer | CreateReportDto { Reason, Details? } | 201/message | One pending report per user/review? (checks pending) |

## Admin
| Method | Endpoint | Auth | Request | Response | Notes |
|---|---|---|---|---|---|
| GET | /api/admin/users | Admin | - | List<UserDto> | All users with roles |
| PUT | /api/admin/users/{id}/suspend | Admin | - | { message } | Suspend user |
| PUT | /api/admin/users/{id}/reactivate | Admin | - | { message } | Reactivate user |
| GET | /api/admin/reports | Admin | status? | List<ReportDto> | All reports, filterable by status |
| PUT | /api/admin/reports/{id}/moderate | Admin | ModerateReportDto { Status, ModerationNotes?, HideReview? } | { message } | Can hide review |
| POST | /api/admin/books | Admin | Book entity/body | Book | Creates book |
| PUT | /api/admin/books/{id} | Admin | Book entity/body | Book | Updates book |
| DELETE | /api/admin/books/{id} | Admin | - | { message } | Soft delete (sets IsActive=false) |
| POST | /api/admin/genres | Admin | Genre | GenreDto | 409 if exists |
| PUT | /api/admin/genres/{id} | Admin | Genre | GenreDto | Update genre |
| DELETE | /api/admin/genres/{id} | Admin | - | 204 | Hard delete genre |

## Notes
- CORS: Not configured in Program.cs (checked). Browser requests from different origin may be blocked in some setups; may need to add CORS for local dev.
- No admin dashboard summary endpoint found; dashboard will need to aggregate or show what's available.
- Reviews: Admin can moderate via reports (hide review). No direct admin review delete endpoint visible.
- User management limited to list/suspend/reactivate (no role change/delete endpoints visible).
