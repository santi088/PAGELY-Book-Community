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
}
