using BookCommunity.Api.DTOs.Admin;
using BookCommunity.Api.DTOs.Auth;
using BookCommunity.Api.DTOs.Books;
using BookCommunity.Api.DTOs.Genres;
using BookCommunity.Api.DTOs.Reviews;
using BookCommunity.Api.Services;
using BookCommunity.Core.Entities;
using BookCommunity.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookCommunity.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOpenLibraryService _openLibraryService;
    
    public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IOpenLibraryService openLibraryService)
    {
        _context = context;
        _userManager = userManager;
        _openLibraryService = openLibraryService;
    }
    
    [HttpGet("users")]
    public async Task<ActionResult<PagedResult<UserDto>>> GetUsers(
        [FromQuery] string? search,
        [FromQuery] string? role,
        [FromQuery] bool? suspended,
        [FromQuery] string? sort = "name",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

var users = await _userManager.Users.ToListAsync();

        var result = new List<UserDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                if (!((user.Email != null && user.Email.ToLower().Contains(searchLower)) ||
                      (user.FirstName != null && user.FirstName.ToLower().Contains(searchLower)) ||
                      (user.LastName != null && user.LastName.ToLower().Contains(searchLower))))
                    continue;
            }

            if (!string.IsNullOrWhiteSpace(role) && !roles.Contains(role))
                continue;

            if (suspended.HasValue && user.IsSuspended != suspended.Value)
                continue;

            result.Add(new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? "",
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsSuspended = user.IsSuspended,
                Roles = roles
            });
        }

        result = sort?.ToLower() switch
        {
            "name" => result.OrderBy(u => u.Email).ToList(),
            "namedesc" => result.OrderByDescending(u => u.Email).ToList(),
            "email" => result.OrderBy(u => u.Email).ToList(),
            "emaildesc" => result.OrderByDescending(u => u.Email).ToList(),
            "newest" => result.OrderByDescending(u => u.Id).ToList(),
            "oldest" => result.OrderBy(u => u.Id).ToList(),
            _ => result.OrderBy(u => u.Email).ToList()
        };

        var totalCount = result.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var paged = result
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(new PagedResult<UserDto>
        {
            Items = paged,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        });
    }

    [HttpPut("users/{id}/suspend")]
    public async Task<IActionResult> SuspendUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();
        
        user.IsSuspended = true;
        user.SuspendedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        
        return Ok(new { message = "User suspended" });
    }
    
    [HttpPut("users/{id}/reactivate")]
    public async Task<IActionResult> ReactivateUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();
        
        user.IsSuspended = false;
        user.SuspendedAt = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        
        return Ok(new { message = "User reactivated" });
    }
    
    [HttpPost("books")]
    public async Task<ActionResult<BookDto>> CreateBook([FromBody] Book book)
    {
        book.Id = Guid.NewGuid();
        book.CreatedAt = DateTime.UtcNow;
        book.UpdatedAt = DateTime.UtcNow;
        book.AverageRating = 0;
        book.ReviewCount = 0;
        
        _context.Books.Add(book);
        await _context.SaveChangesAsync();
        
        return CreatedAtAction(nameof(CreateBook), new { id = book.Id }, book);
    }
    
    [HttpPut("books/{id}")]
    public async Task<IActionResult> UpdateBook(Guid id, [FromBody] Book bookUpdate)
    {
        var book = await _context.Books.FindAsync(id);
        if (book == null)
            return NotFound();
        
        book.Title = bookUpdate.Title;
        book.Author = bookUpdate.Author;
        book.Description = bookUpdate.Description;
        book.CoverImageUrl = bookUpdate.CoverImageUrl;
        book.ISBN = bookUpdate.ISBN;
        book.PublicationYear = bookUpdate.PublicationYear;
        book.PublicationDate = bookUpdate.PublicationDate;
        book.IsActive = bookUpdate.IsActive;
        book.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return Ok(book);
    }
    
    [HttpDelete("books/{id}")]
    public async Task<IActionResult> DeleteBook(Guid id)
    {
        var book = await _context.Books.FindAsync(id);
        if (book == null)
            return NotFound();

        var readingEntries = await _context.ReadingListEntries
            .Where(r => r.BookId == id)
            .ToListAsync();
        _context.ReadingListEntries.RemoveRange(readingEntries);

        // Soft delete by deactivating
        book.IsActive = false;
        book.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Book deactivated and removed from reading lists" });
    }
    
    [HttpGet("books/search")]
    public async Task<ActionResult<IReadOnlyList<OpenLibraryBookCandidateDto>>> SearchBooks(
        [FromQuery] string q,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(new { message = "Search query is required" });
        }
        
        try
        {
            var results = await _openLibraryService.SearchAsync(q, pageSize, cancellationToken);
            return Ok(results);
        }
        catch (OpenLibraryException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message, retryAfterSeconds = ex.RetryAfterSeconds });
        }
    }
    
    [HttpPost("books/import")]
    public async Task<ActionResult<ImportBookResultDto>> ImportBook(
        [FromBody] ImportBookRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.OpenLibraryId))
        {
            return BadRequest(new { message = "Open Library ID is required" });
        }
        
        // Fetch record metadata from Open Library (authoritative source)
        OpenLibraryBookCandidateDto? candidate;
        try
        {
            candidate = await _openLibraryService.GetRecordAsync(dto.OpenLibraryId, cancellationToken);
        }
        catch (OpenLibraryException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message, retryAfterSeconds = ex.RetryAfterSeconds });
        }

        if (candidate == null)
        {
            return NotFound(new { message = "Book not found in Open Library" });
        }
        
        // Check for existing import from same provider
        var existingByProvider = await _context.Books
            .FirstOrDefaultAsync(b => b.ExternalProvider == "OpenLibrary" && b.ExternalId == dto.OpenLibraryId, cancellationToken);
        
        if (existingByProvider != null)
        {
            if (!existingByProvider.IsActive)
            {
                existingByProvider.IsActive = true;
                existingByProvider.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return Ok(new ImportBookResultDto
            {
                BookId = existingByProvider.Id,
                Title = existingByProvider.Title,
                Author = existingByProvider.Author,
                IsNewImport = false,
                Message = "Book restored from Open Library"
            });
        }
        
        // Check for existing book by ISBN
        string? isbnToCheck = candidate.Isbn13 ?? candidate.Isbn10;
        if (!string.IsNullOrWhiteSpace(isbnToCheck))
        {
            var existingByIsbn = await _context.Books
                .FirstOrDefaultAsync(b => b.ISBN == isbnToCheck, cancellationToken);
            
            if (existingByIsbn != null)
            {
                if (!existingByIsbn.IsActive)
                {
                    existingByIsbn.IsActive = true;
                    existingByIsbn.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
                return Ok(new ImportBookResultDto
                {
                    BookId = existingByIsbn.Id,
                    Title = existingByIsbn.Title,
                    Author = existingByIsbn.Author,
                    IsNewImport = false,
                    ExistingIsbnMatch = existingByIsbn.ISBN,
                    Message = "Book restored by ISBN"
                });
            }
        }
        
        // Create new book
        var book = new Book
        {
            Title = candidate.Title,
            Author = candidate.Authors.Count > 0 ? string.Join(", ", candidate.Authors) : "Unknown author",
            Description = candidate.Description,
            CoverImageUrl = candidate.CoverImageUrl,
            ISBN = isbnToCheck,
            PublicationYear = candidate.PublicationYear,
            PublicationDate = !string.IsNullOrWhiteSpace(candidate.PublicationDate) 
                && DateTime.TryParse(candidate.PublicationDate.Trim(), out var pubDate)
                ? DateTime.SpecifyKind(pubDate, DateTimeKind.Utc)
                : DateTime.UtcNow,
            IsActive = true,
            AverageRating = 0,
            ReviewCount = 0,
            ExternalProvider = "OpenLibrary",
            ExternalId = candidate.OpenLibraryId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _context.Books.Add(book);
        await _context.SaveChangesAsync();
        
        // Map genres/categories - reuse existing genres, create new ones if needed
        if (candidate.Subjects.Any())
        {
            foreach (var categoryName in candidate.Subjects)
            {
                var genre = await _context.Genres
                    .FirstOrDefaultAsync(g => g.Name.ToLower() == categoryName.ToLower(), cancellationToken);
                
                if (genre == null)
                {
                    genre = new Genre
                    {
                        Name = categoryName,
                        Description = null,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Genres.Add(genre);
                    await _context.SaveChangesAsync();
                }
                
                var bookGenre = new BookGenre
                {
                    BookId = book.Id,
                    GenreId = genre.Id
                };
                _context.BookGenres.Add(bookGenre);
            }
            await _context.SaveChangesAsync();
        }
        
        return CreatedAtAction(nameof(ImportBook), new { id = book.Id }, new ImportBookResultDto
        {
            BookId = book.Id,
            Title = book.Title,
            Author = book.Author,
            IsNewImport = true,
            Message = "Book imported successfully from Open Library"
        });
    }
    
    [HttpGet("reviews")]
    public async Task<ActionResult<PagedResult<AdminReviewDto>>> GetReviews(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int? rating,
        [FromQuery] string? sort = "newest",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var query = _context.Reviews
            .Include(r => r.User)
            .Include(r => r.Book)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(r =>
                (r.ReviewText != null && r.ReviewText.ToLower().Contains(searchLower)) ||
                (r.Book != null && r.Book.Title != null && r.Book.Title.ToLower().Contains(searchLower)) ||
                (r.User != null && r.User.Email != null && r.User.Email.ToLower().Contains(searchLower)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var statusLower = status.ToLower();
            query = statusLower switch
            {
                "visible" => query.Where(r => !r.IsDeleted && !r.IsHidden),
                "hidden" => query.Where(r => !r.IsDeleted && r.IsHidden),
                "deleted" => query.Where(r => r.IsDeleted),
                _ => query
            };
        }

        if (rating.HasValue && rating.Value >= 1 && rating.Value <= 5)
        {
            query = query.Where(r => r.Rating == rating.Value);
        }

        query = sort?.ToLower() switch
        {
            "oldest" => query.OrderBy(r => r.CreatedAt),
            "highest" => query.OrderByDescending(r => r.Rating).ThenByDescending(r => r.CreatedAt),
            "lowest" => query.OrderBy(r => r.Rating).ThenByDescending(r => r.CreatedAt),
            "mostliked" => query.OrderByDescending(r => r.LikeCount).ThenByDescending(r => r.CreatedAt),
            "book" => query.OrderBy(r => r.Book.Title),
            "reader" => query.OrderBy(r => r.User.Email),
            _ => query.OrderByDescending(r => r.CreatedAt)
        };

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var reviews = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new AdminReviewDto
            {
                Id = r.Id,
                UserId = r.UserId,
                UserEmail = r.User.Email ?? "",
                BookId = r.BookId,
                BookTitle = r.Book.Title,
                BookAuthor = r.Book.Author,
                BookCoverImageUrl = r.Book.CoverImageUrl,
                Rating = r.Rating,
                ReviewText = r.ReviewText,
                LikeCount = r.LikeCount,
                IsHidden = r.IsHidden,
                IsDeleted = r.IsDeleted,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .ToListAsync();

        return Ok(new PagedResult<AdminReviewDto>
        {
            Items = reviews,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        });
    }

    [HttpDelete("reviews/{id}")]
    public async Task<IActionResult> DeleteReview(Guid id)
    {
        var review = await _context.Reviews
            .Include(r => r.Book)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (review == null)
            return NotFound();

        if (!review.IsDeleted)
        {
            review.IsDeleted = true;
            review.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var book = review.Book;
            if (book != null)
            {
                var reviews = await _context.Reviews
                    .Where(r => r.BookId == book.Id && !r.IsDeleted && !r.IsHidden)
                    .ToListAsync();

                if (reviews.Any())
                {
                    book.AverageRating = Math.Round(reviews.Average(r => r.Rating), 2);
                    book.ReviewCount = reviews.Count;
                }
                else
                {
                    book.AverageRating = 0;
                    book.ReviewCount = 0;
                }

                book.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        return Ok(new { message = "Review removed" });
    }

    [HttpPost("genres")]
    public async Task<ActionResult<GenreDto>> CreateGenre([FromBody] Genre genre)
    {
        if (await _context.Genres.AnyAsync(g => g.Name.ToLower() == genre.Name.ToLower()))
            return Conflict(new { message = "Genre already exists" });
        
        genre.Id = Guid.NewGuid();
        genre.CreatedAt = DateTime.UtcNow;
        _context.Genres.Add(genre);
        await _context.SaveChangesAsync();
        
        return Ok(new GenreDto { Id = genre.Id, Name = genre.Name, Description = genre.Description });
    }
    
    [HttpPut("genres/{id}")]
    public async Task<IActionResult> UpdateGenre(Guid id, [FromBody] Genre genreUpdate)
    {
        var genre = await _context.Genres.FindAsync(id);
        if (genre == null)
            return NotFound();
        
        genre.Name = genreUpdate.Name;
        genre.Description = genreUpdate.Description;
        await _context.SaveChangesAsync();
        return Ok(new GenreDto { Id = genre.Id, Name = genre.Name, Description = genre.Description });
    }
    
    [HttpDelete("genres/{id}")]
    public async Task<IActionResult> DeleteGenre(Guid id)
    {
        var genre = await _context.Genres.FindAsync(id);
        if (genre == null)
            return NotFound();
        
        _context.Genres.Remove(genre);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("roles")]
    public async Task<ActionResult<List<string>>> GetRoles()
    {
        var roles = new HashSet<string>();
        var users = await _userManager.Users.ToListAsync();
        foreach (var user in users)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            foreach (var r in userRoles)
                roles.Add(r);
        }
        return Ok(roles.OrderBy(r => r).ToList());
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardDto>> GetDashboard([FromQuery] int days = 7)
    {
        if (days != 7 && days != 30) days = 7;

        var cutoff = DateTime.UtcNow.AddDays(-days);
        var cutoff7 = DateTime.UtcNow.AddDays(-7);

        // Context strip: visible books, hidden books, genres, active/suspended users, visible reviews
        var visibleBooks = await _context.Books.CountAsync(b => b.IsActive);
        var hiddenBooks = await _context.Books.CountAsync(b => !b.IsActive);
        var totalGenres = await _context.Genres.CountAsync();

        var allUsers = await _userManager.Users.ToListAsync();
        var activeUsers = allUsers.Count(u => !u.IsSuspended);
        var suspendedUsers = allUsers.Count(u => u.IsSuspended);

        var visibleReviews = await _context.Reviews.CountAsync(r => !r.IsDeleted && !r.IsHidden);

        // Needs attention: pending reports + oldest pending age
        var pendingReports = await _context.Reports
            .Include(r => r.Review)
            .ThenInclude(r => r.Book)
            .Where(r => r.Status == ReportStatus.Pending)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();

        var pendingReportCount = pendingReports.Count;
        var oldestPendingReportDays = pendingReportCount > 0
            ? (int)(DateTime.UtcNow - pendingReports.Min(r => r.CreatedAt)).TotalDays
            : 0;

        // New reports in last 7 days
        var newReportsLast7Days = await _context.Reports.CountAsync(r => r.CreatedAt >= cutoff7);

        // Hidden/removed reviews in last 7 days
        var hiddenRemovedReviewsLast7Days = await _context.Reviews.CountAsync(r =>
            r.CreatedAt >= cutoff7 && (r.IsHidden || r.IsDeleted));

        // Trends: new reviews per day (last N days)
        var newReviewsPerDay = await _context.Reviews
            .Where(r => r.CreatedAt >= cutoff)
            .Select(r => r.CreatedAt.Date)
            .ToListAsync();
        var newReviewsPerDayGrouped = newReviewsPerDay
            .GroupBy(d => d)
            .Select(g => new DailyTrendDto { Date = g.Key.ToString("yyyy-MM-dd"), Count = g.Count() })
            .OrderBy(g => g.Date)
            .ToList();

        // Trends: new users per day (ApplicationUser has CreatedAt)
        var newUsersPerDayGrouped = allUsers
            .Where(u => u.CreatedAt >= cutoff)
            .GroupBy(u => u.CreatedAt.Date)
            .Select(g => new DailyTrendDto { Date = g.Key.ToString("yyyy-MM-dd"), Count = g.Count() })
            .OrderBy(g => g.Date)
            .ToList();

        // Trends: reports by reason (distinct reason values from DB)
        var reportsByReason = await _context.Reports
            .GroupBy(r => r.Reason)
            .Select(g => new ReasonCountDto { Reason = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .ToListAsync();

        // Trends: resolved vs dismissed vs pending breakdown
        var statusBreakdown = await _context.Reports
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();
        var breakdown = new ReportStatusBreakdownDto
        {
            Pending = statusBreakdown.FirstOrDefault(s => s.Status == ReportStatus.Pending)?.Count ?? 0,
            Dismissed = statusBreakdown.FirstOrDefault(s => s.Status == ReportStatus.Dismissed)?.Count ?? 0,
            Resolved = statusBreakdown.FirstOrDefault(s => s.Status == ReportStatus.Resolved)?.Count ?? 0
        };

        // Catalog insights: books per genre
        var booksPerGenre = await _context.BookGenres
            .Include(bg => bg.Genre)
            .Include(bg => bg.Book)
            .Where(bg => bg.Book.IsActive)
            .GroupBy(bg => bg.Genre.Name)
            .Select(g => new GenreBookCountDto { GenreName = g.Key, BookCount = g.Count() })
            .OrderByDescending(g => g.BookCount)
            .ToListAsync();

        // Genres with zero visible books
        var genresWithZeroVisibleBooks = await _context.Genres
            .Where(g => !g.BookGenres.Any(bg => bg.Book.IsActive))
            .Select(g => g.Name)
            .ToListAsync();

        // Top 5 most reviewed visible books
        var top5MostReviewed = await _context.Books
            .Where(b => b.IsActive && b.ReviewCount > 0)
            .OrderByDescending(b => b.ReviewCount)
            .ThenBy(b => b.Title)
            .Take(5)
            .Select(b => new BookSummaryDto
            {
                Id = b.Id,
                Title = b.Title,
                Author = b.Author,
                Count = b.ReviewCount,
                AverageRating = b.AverageRating,
                ReviewCount = b.ReviewCount
            })
            .ToListAsync();

        // Top 5 highest rated visible books (require at least 1 review)
        var top5HighestRated = await _context.Books
            .Where(b => b.IsActive && b.ReviewCount > 0)
            .OrderByDescending(b => b.AverageRating)
            .ThenByDescending(b => b.ReviewCount)
            .Take(5)
            .Select(b => new BookSummaryDto
            {
                Id = b.Id,
                Title = b.Title,
                Author = b.Author,
                Count = b.ReviewCount,
                AverageRating = b.AverageRating,
                ReviewCount = b.ReviewCount
            })
            .ToListAsync();

        // Top 5 most saved to reading lists
        var top5MostSaved = await _context.ReadingListEntries
            .Include(rle => rle.Book)
            .Where(rle => rle.Book.IsActive)
            .GroupBy(rle => new { rle.Book.Id, rle.Book.Title, rle.Book.Author })
            .Select(g => new BookSummaryDto
            {
                Id = g.Key.Id,
                Title = g.Key.Title,
                Author = g.Key.Author,
                Count = g.Count(),
                AverageRating = null,
                ReviewCount = 0
            })
            .OrderByDescending(b => b.Count)
            .Take(5)
            .ToListAsync();

        // Visible books missing cover or description
        var booksMissingCoverOrDesc = await _context.Books
            .Where(b => b.IsActive && (string.IsNullOrWhiteSpace(b.CoverImageUrl) || string.IsNullOrWhiteSpace(b.Description)))
            .OrderBy(b => b.Title)
            .Take(5)
            .Select(b => new BookSummaryDto
            {
                Id = b.Id,
                Title = b.Title,
                Author = b.Author,
                Count = 0,
                AverageRating = null,
                ReviewCount = 0
            })
            .ToListAsync();

        var booksMissingCoverOrDescCount = await _context.Books
            .CountAsync(b => b.IsActive && (string.IsNullOrWhiteSpace(b.CoverImageUrl) || string.IsNullOrWhiteSpace(b.Description)));

        // Moderation insights: top 5 most reported books
        var top5MostReportedBooks = await _context.Reports
            .Include(r => r.Review)
            .ThenInclude(r => r.Book)
            .Where(r => r.Review.Book.IsActive)
            .GroupBy(r => new { r.Review.Book.Id, r.Review.Book.Title })
            .Select(g => new ReportedItemSummaryDto
            {
                Id = g.Key.Id.ToString(),
                Name = g.Key.Title,
                ReportCount = g.Count()
            })
            .OrderByDescending(g => g.ReportCount)
            .Take(5)
            .ToListAsync();

        // Top 5 most reported reviewers
        var top5MostReportedReviewers = await _context.Reports
            .Include(r => r.Reporter)
            .GroupBy(r => new { r.ReporterId, r.Reporter.Email })
            .Select(g => new ReportedItemSummaryDto
            {
                Id = g.Key.ReporterId,
                Name = g.Key.Email ?? g.Key.ReporterId,
                ReportCount = g.Count()
            })
            .OrderByDescending(g => g.ReportCount)
            .Take(5)
            .ToListAsync();

        var result = new DashboardDto
        {
            VisibleBooks = visibleBooks,
            HiddenBooks = hiddenBooks,
            TotalGenres = totalGenres,
            ActiveUsers = activeUsers,
            SuspendedUsers = suspendedUsers,
            VisibleReviews = visibleReviews,
            PendingReports = pendingReportCount,
            OldestPendingReportDays = oldestPendingReportDays,
            NewReportsLast7Days = newReportsLast7Days,
            HiddenRemovedReviewsLast7Days = hiddenRemovedReviewsLast7Days,
            NewReviewsPerDay = newReviewsPerDayGrouped.Select(d => new DailyTrend { Date = d.Date, Count = d.Count }).ToList(),
            NewUsersPerDay = newUsersPerDayGrouped.Select(d => new DailyTrend { Date = d.Date, Count = d.Count }).ToList(),
            ReportsByReason = reportsByReason.Select(r => new ReasonCount { Reason = r.Reason, Count = r.Count }).ToList(),
            StatusBreakdown = new ReportStatusBreakdown
            {
                Pending = breakdown.Pending,
                Dismissed = breakdown.Dismissed,
                Resolved = breakdown.Resolved
            },
            BooksPerGenre = booksPerGenre.Select(g => new GenreBookCount { GenreName = g.GenreName, BookCount = g.BookCount }).ToList(),
            GenresWithZeroVisibleBooks = genresWithZeroVisibleBooks,
            Top5MostReviewed = top5MostReviewed.Select(b => new BookSummary
            {
                Id = b.Id,
                Title = b.Title,
                Author = b.Author,
                Count = b.Count,
                AverageRating = b.AverageRating,
                ReviewCount = b.ReviewCount
            }).ToList(),
            Top5HighestRated = top5HighestRated.Select(b => new BookSummary
            {
                Id = b.Id,
                Title = b.Title,
                Author = b.Author,
                Count = b.Count,
                AverageRating = b.AverageRating,
                ReviewCount = b.ReviewCount
            }).ToList(),
            Top5MostSaved = top5MostSaved.Select(b => new BookSummary
            {
                Id = b.Id,
                Title = b.Title,
                Author = b.Author,
                Count = b.Count,
                AverageRating = null,
                ReviewCount = 0
            }).ToList(),
            BooksMissingCoverOrDescription = booksMissingCoverOrDescCount,
            BooksMissingCoverOrDescriptionSample = booksMissingCoverOrDesc.Select(b => new BookSummary
            {
                Id = b.Id,
                Title = b.Title,
                Author = b.Author,
                Count = 0,
                AverageRating = null,
                ReviewCount = 0
            }).ToList(),
            Top5MostReportedBooks = top5MostReportedBooks.Select(b => new ReportedItemSummary
            {
                Id = b.Id,
                Name = b.Name,
                ReportCount = b.ReportCount
            }).ToList(),
            Top5MostReportedReviewers = top5MostReportedReviewers.Select(r => new ReportedItemSummary
            {
                Id = r.Id,
                Name = r.Name,
                ReportCount = r.ReportCount
            }).ToList()
        };

        return Ok(result);
    }
}

// Helper DTOs for dashboard queries (internal use only)
public class DailyTrendDto
{
    public string Date { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ReasonCountDto
{
    public string Reason { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ReportStatusBreakdownDto
{
    public int Pending { get; set; }
    public int Dismissed { get; set; }
    public int Resolved { get; set; }
}

public class GenreBookCountDto
{
    public string GenreName { get; set; } = string.Empty;
    public int BookCount { get; set; }
}

public class BookSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int Count { get; set; }
    public double? AverageRating { get; set; }
    public int ReviewCount { get; set; }
}

public class ReportedItemSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int ReportCount { get; set; }
}
