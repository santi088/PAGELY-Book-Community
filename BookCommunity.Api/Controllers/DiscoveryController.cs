using BookCommunity.Api.DTOs.Books;
using BookCommunity.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookCommunity.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiscoveryController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    
    public DiscoveryController(ApplicationDbContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public async Task<ActionResult<PagedResult<BookDto>>> GetDiscovery(
        [FromQuery] string shelf = "popular",
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
        
        query = shelf.ToLower() switch
        {
            "popular" => query.OrderByDescending(b => b.ReviewCount).ThenByDescending(b => b.AverageRating).ThenBy(b => b.Title),
            "top-rated" => query.Where(b => b.ReviewCount > 0)
                               .OrderByDescending(b => b.AverageRating)
                               .ThenByDescending(b => b.ReviewCount)
                               .ThenBy(b => b.Title),
            "recent" => query.OrderByDescending(b => b.CreatedAt).ThenBy(b => b.Title),
            _ => query.OrderByDescending(b => b.ReviewCount).ThenBy(b => b.Title)
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
}
