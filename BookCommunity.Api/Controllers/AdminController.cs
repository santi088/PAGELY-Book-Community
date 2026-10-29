using BookCommunity.Api.DTOs.Auth;
using BookCommunity.Api.DTOs.Books;
using BookCommunity.Api.DTOs.Genres;
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
    public async Task<ActionResult<List<UserDto>>> GetUsers()
    {
        var users = await _userManager.Users.ToListAsync();
        var result = new List<UserDto>();
        
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
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
        
        return Ok(result);
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
        
        // Soft delete by deactivating
        book.IsActive = false;
        book.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Book deactivated" });
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
            return Ok(new ImportBookResultDto
            {
                BookId = existingByProvider.Id,
                Title = existingByProvider.Title,
                Author = existingByProvider.Author,
                IsNewImport = false,
                Message = "Book already imported from Open Library"
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
                return Ok(new ImportBookResultDto
                {
                    BookId = existingByIsbn.Id,
                    Title = existingByIsbn.Title,
                    Author = existingByIsbn.Author,
                    IsNewImport = false,
                    ExistingIsbnMatch = existingByIsbn.ISBN,
                    Message = "Book with this ISBN already exists in the catalog"
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
}
