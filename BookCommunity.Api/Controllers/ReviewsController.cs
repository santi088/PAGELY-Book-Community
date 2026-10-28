using BookCommunity.Api.DTOs.Books;
using BookCommunity.Api.DTOs.Reviews;
using BookCommunity.Core.Entities;
using BookCommunity.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BookCommunity.Api.Controllers;

[ApiController]
[Route("api")]
public class ReviewsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    
    public ReviewsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }
    
    [HttpGet("books/{bookId}/reviews")]
    public async Task<ActionResult<PagedResult<ReviewDto>>> GetBookReviews(
        Guid bookId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;
        
        var bookExists = await _context.Books.AnyAsync(b => b.Id == bookId && b.IsActive);
        if (!bookExists)
            return NotFound();
        
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        var query = _context.Reviews
            .Include(r => r.User)
            .Include(r => r.ReviewLikes)
            .Where(r => r.BookId == bookId && !r.IsDeleted && !r.IsHidden)
            .OrderByDescending(r => r.CreatedAt);
        
        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        
        var reviews = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ReviewDto
            {
                Id = r.Id,
                UserId = r.UserId,
                UserEmail = r.User.Email ?? "",
                BookId = r.BookId,
                Rating = r.Rating,
                ReviewText = r.ReviewText,
                LikeCount = r.LikeCount,
                IsLikedByCurrentUser = userId != null && r.ReviewLikes.Any(rl => rl.UserId == userId),
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .ToListAsync();
        
        return Ok(new PagedResult<ReviewDto>
        {
            Items = reviews,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        });
    }
    
    [Authorize]
    [HttpPost("books/{bookId}/reviews")]
    public async Task<ActionResult<ReviewDto>> CreateReview(Guid bookId, [FromBody] CreateReviewDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
        
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.IsSuspended)
            return Forbid();
        
        var book = await _context.Books.FindAsync(bookId);
        if (book == null || !book.IsActive)
            return NotFound();
        
        // Check if user already has a review for this book
        var existingReview = await _context.Reviews
            .FirstOrDefaultAsync(r => r.UserId == userId && r.BookId == bookId && !r.IsDeleted);
        if (existingReview != null)
            return Conflict(new { message = "You already have a review for this book" });
        
        var review = new Review
        {
            UserId = userId,
            BookId = bookId,
            Rating = dto.Rating,
            ReviewText = dto.ReviewText,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();
        
        // Update book average rating and review count
        await UpdateBookRatingAsync(bookId);
        
        var reviewDto = new ReviewDto
        {
            Id = review.Id,
            UserId = review.UserId,
            UserEmail = user.Email ?? "",
            BookId = review.BookId,
            Rating = review.Rating,
            ReviewText = review.ReviewText,
            LikeCount = review.LikeCount,
            IsLikedByCurrentUser = false,
            CreatedAt = review.CreatedAt,
            UpdatedAt = review.UpdatedAt
        };
        
        return CreatedAtAction(nameof(GetBookReviews), new { bookId = bookId }, reviewDto);
    }
    
    [Authorize]
    [HttpPut("reviews/{id}")]
    public async Task<ActionResult<ReviewDto>> UpdateReview(Guid id, [FromBody] UpdateReviewDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
        
        var review = await _context.Reviews
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
        
        if (review == null)
            return NotFound();
        
        if (review.UserId != userId)
            return Forbid();
        
        review.Rating = dto.Rating;
        review.ReviewText = dto.ReviewText;
        review.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        await UpdateBookRatingAsync(review.BookId);
        
        var reviewDto = new ReviewDto
        {
            Id = review.Id,
            UserId = review.UserId,
            UserEmail = review.User.Email ?? "",
            BookId = review.BookId,
            Rating = review.Rating,
            ReviewText = review.ReviewText,
            LikeCount = review.LikeCount,
            IsLikedByCurrentUser = await _context.ReviewLikes.AnyAsync(rl => rl.ReviewId == id && rl.UserId == userId),
            CreatedAt = review.CreatedAt,
            UpdatedAt = review.UpdatedAt
        };
        
        return Ok(reviewDto);
    }
    
    [Authorize]
    [HttpDelete("reviews/{id}")]
    public async Task<IActionResult> DeleteReview(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
        
        var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
        if (review == null)
            return NotFound();
        
        if (review.UserId != userId)
            return Forbid();
        
        review.IsDeleted = true;
        review.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        await UpdateBookRatingAsync(review.BookId);
        
        return NoContent();
    }
    
    [Authorize]
    [HttpPut("reviews/{id}/like")]
    public async Task<IActionResult> LikeReview(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
        
        var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
        if (review == null)
            return NotFound();
        
        var existingLike = await _context.ReviewLikes
            .FirstOrDefaultAsync(rl => rl.ReviewId == id && rl.UserId == userId);
        if (existingLike != null)
            return Conflict(new { message = "You already liked this review" });
        
        var like = new ReviewLike
        {
            UserId = userId,
            ReviewId = id,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.ReviewLikes.Add(like);
        review.LikeCount++;
        review.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return Ok(new { message = "Review liked" });
    }
    
    [Authorize]
    [HttpDelete("reviews/{id}/like")]
    public async Task<IActionResult> UnlikeReview(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
        
        var like = await _context.ReviewLikes
            .Include(rl => rl.Review)
            .FirstOrDefaultAsync(rl => rl.ReviewId == id && rl.UserId == userId);
        if (like == null)
            return NotFound();
        
        _context.ReviewLikes.Remove(like);
        if (like.Review.LikeCount > 0)
            like.Review.LikeCount--;
        like.Review.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return NoContent();
    }
    
    [Authorize]
    [HttpGet("me/reviews")]
    public async Task<ActionResult<PagedResult<ReviewDto>>> GetMyReviews(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
        
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;
        
        var query = _context.Reviews
            .Include(r => r.User)
            .Include(r => r.ReviewLikes)
            .Where(r => r.UserId == userId && !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAt);
        
        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        
        var reviews = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ReviewDto
            {
                Id = r.Id,
                UserId = r.UserId,
                UserEmail = r.User.Email ?? "",
                BookId = r.BookId,
                Rating = r.Rating,
                ReviewText = r.ReviewText,
                LikeCount = r.LikeCount,
                IsLikedByCurrentUser = r.ReviewLikes.Any(rl => rl.UserId == userId),
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .ToListAsync();
        
        return Ok(new PagedResult<ReviewDto>
        {
            Items = reviews,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        });
    }
    
    private async Task UpdateBookRatingAsync(Guid bookId)
    {
        var book = await _context.Books.FindAsync(bookId);
        if (book == null) return;
        
        var reviews = await _context.Reviews
            .Where(r => r.BookId == bookId && !r.IsDeleted && !r.IsHidden)
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
