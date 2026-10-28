using BookCommunity.Api.DTOs.Books;
using BookCommunity.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookCommunity.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    
    public BooksController(ApplicationDbContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public async Task<ActionResult<PagedResult<BookDto>>> GetBooks(
        [FromQuery] string? search,
        [FromQuery] string? genre,
        [FromQuery] string? sortBy = "title",
        [FromQuery] string? sortOrder = "asc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;
        
        var query = _context.Books
            .Include(b => b.BookGenres)
            .ThenInclude(bg => bg.Genre)
            .Where(b => b.IsActive)
            .AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(b => b.Title.ToLower().Contains(searchLower) || 
                                    b.Author.ToLower().Contains(searchLower));
        }
        
        if (!string.IsNullOrWhiteSpace(genre))
        {
            query = query.Where(b => b.BookGenres.Any(bg => bg.Genre.Name.ToLower() == genre.ToLower()));
        }
        
        // Sorting
        query = sortBy?.ToLower() switch
        {
            "title" => sortOrder?.ToLower() == "desc" ? query.OrderByDescending(b => b.Title) : query.OrderBy(b => b.Title),
            "author" => sortOrder?.ToLower() == "desc" ? query.OrderByDescending(b => b.Author) : query.OrderBy(b => b.Author),
            "rating" => sortOrder?.ToLower() == "desc" ? query.OrderByDescending(b => b.AverageRating).ThenBy(b => b.Title) : query.OrderBy(b => b.AverageRating).ThenBy(b => b.Title),
            "created" => sortOrder?.ToLower() == "desc" ? query.OrderByDescending(b => b.CreatedAt) : query.OrderBy(b => b.CreatedAt),
            _ => query.OrderBy(b => b.Title)
        };
        
        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        
        var books = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BookDto
            {
                Id = b.Id,
                Title = b.Title,
                Author = b.Author,
                Description = b.Description,
                CoverImageUrl = b.CoverImageUrl,
                ISBN = b.ISBN,
                PublicationYear = b.PublicationYear,
                PublicationDate = b.PublicationDate,
                AverageRating = b.AverageRating,
                ReviewCount = b.ReviewCount,
                Genres = b.BookGenres.Select(bg => bg.Genre.Name).ToList()
            })
            .ToListAsync();
        
        return Ok(new PagedResult<BookDto>
        {
            Items = books,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        });
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<BookDto>> GetBook(Guid id)
    {
        var book = await _context.Books
            .Include(b => b.BookGenres)
            .ThenInclude(bg => bg.Genre)
            .FirstOrDefaultAsync(b => b.Id == id && b.IsActive);
        
        if (book == null)
            return NotFound();
        
        var dto = new BookDto
        {
            Id = book.Id,
            Title = book.Title,
            Author = book.Author,
            Description = book.Description,
            CoverImageUrl = book.CoverImageUrl,
            ISBN = book.ISBN,
            PublicationYear = book.PublicationYear,
            PublicationDate = book.PublicationDate,
            AverageRating = book.AverageRating,
            ReviewCount = book.ReviewCount,
            Genres = book.BookGenres.Select(bg => bg.Genre.Name).ToList()
        };
        
        return Ok(dto);
    }
}
