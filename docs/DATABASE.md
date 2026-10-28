# Database Design

## Entities (planned)
- ApplicationUser (ASP.NET Core Identity)
- Book
- Genre
- BookGenre (join)
- Review
- ReviewLike
- ReadingListEntry
- Report

## Relationships
- Book <-> Genre many-to-many via BookGenre
- User 1:N Reviews
- Book 1:N Reviews
- Review 1:N ReviewLikes
- User 1:N ReviewLikes (unique per user+review)
- User 1:N ReadingListEntry
- Book 1:N ReadingListEntry (unique per user+book)
- Review 1:N Reports
- User 1:N Reports (reporter)

## Constraints/Indexes (planned)
- Unique: (Review.UserId, Review.BookId)
- Unique: (ReviewLike.UserId, ReviewLike.ReviewId)
- Unique: (ReadingListEntry.UserId, ReadingListEntry.BookId)
- Unique: (BookGenre.BookId, BookGenre.GenreId)
- Unique: normalized email
- Indexes on Book.Title, Book.Author, Genre.Name, Reviews.BookId, etc.
- Soft delete/deactivation where specified

## Migrations
Created via EF Core. Apply with `dotnet ef database update`. If DB unavailable, migrations generated only.

## Seeding
Development seed for genres/books and optional admin user via safe initialization (no hardcoded prod creds).
