using BookCommunity.Api.DTOs.Auth;
using BookCommunity.Api.DTOs.Books;
using BookCommunity.Api.DTOs.Genres;
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
    
    public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
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
