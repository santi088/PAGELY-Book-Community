using BookCommunity.Api.DTOs.Books;
using BookCommunity.Api.DTOs.ReadingList;
using BookCommunity.Core.Entities;
using BookCommunity.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BookCommunity.Api.Controllers;

[ApiController]
[Route("api/me/reading-list")]
[Authorize]
public class ReadingListController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    
    public ReadingListController(ApplicationDbContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public async Task<ActionResult<PagedResult<ReadingListDto>>> GetReadingList(
        [FromQuery] ReadingStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
        
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;
        
        var query = _context.ReadingListEntries
            .Include(rle => rle.Book)
            .Where(rle => rle.UserId == userId)
            .AsQueryable();
        
        if (status.HasValue)
        {
            query = query.Where(rle => rle.Status == status.Value);
        }
        
        query = query.OrderByDescending(rle => rle.UpdatedAt);
        
        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(rle => new ReadingListDto
            {
                Id = rle.Id,
                BookId = rle.BookId,
                BookTitle = rle.Book.Title,
                BookAuthor = rle.Book.Author,
                CoverImageUrl = rle.Book.CoverImageUrl,
                Status = rle.Status,
                CreatedAt = rle.CreatedAt,
                UpdatedAt = rle.UpdatedAt
            })
            .ToListAsync();
        
        return Ok(new PagedResult<ReadingListDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        });
    }
    
    [HttpPut("/api/books/{bookId}/reading-list")]
    public async Task<IActionResult> AddOrUpdateReadingList(Guid bookId, [FromBody] UpdateReadingListDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
        
        var book = await _context.Books.FindAsync(bookId);
        if (book == null || !book.IsActive)
            return NotFound();
        
        var existing = await _context.ReadingListEntries
            .FirstOrDefaultAsync(rle => rle.UserId == userId && rle.BookId == bookId);
        
        if (existing != null)
        {
            existing.Status = dto.Status;
            existing.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Reading list updated" });
        }
        
        var entry = new ReadingListEntry
        {
            UserId = userId,
            BookId = bookId,
            Status = dto.Status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _context.ReadingListEntries.Add(entry);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetReadingList), new { }, new { message = "Added to reading list" });
    }
    
    [HttpDelete("/api/books/{bookId}/reading-list")]
    public async Task<IActionResult> RemoveFromReadingList(Guid bookId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
        
        var entry = await _context.ReadingListEntries
            .FirstOrDefaultAsync(rle => rle.UserId == userId && rle.BookId == bookId);
        
        if (entry == null)
            return NotFound();
        
        _context.ReadingListEntries.Remove(entry);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
