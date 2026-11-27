using BookCommunity.Api.DTOs.Books;
using BookCommunity.Api.DTOs.Reports;
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
public class ReportsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    
    public ReportsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }
    
    [Authorize]
    [HttpPost("reviews/{reviewId}/reports")]
    public async Task<IActionResult> CreateReport(Guid reviewId, [FromBody] CreateReportDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
        
        var currentUser = await _userManager.FindByIdAsync(userId);
        if (currentUser == null || currentUser.IsSuspended)
            return Forbid();
        
        var review = await _context.Reviews.FindAsync(reviewId);
        if (review == null || review.IsDeleted)
            return NotFound();
        
        // Check if user already reported this review
        var existingReport = await _context.Reports
            .FirstOrDefaultAsync(r => r.ReviewId == reviewId && r.ReporterId == userId && r.Status == ReportStatus.Pending);
        if (existingReport != null)
            return Conflict(new { message = "You already have a pending report for this review" });
        
        var report = new Report
        {
            ReporterId = userId,
            ReviewId = reviewId,
            Reason = dto.Reason,
            Details = dto.Details,
            Status = ReportStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Reports.Add(report);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(CreateReport), new { reviewId }, new { message = "Report submitted" });
    }
    
    [Authorize(Roles = "Admin")]
    [HttpGet("admin/reports")]
    public async Task<ActionResult<PagedResult<ReportDto>>> GetReports(
        [FromQuery] ReportStatus? status,
        [FromQuery] string? search,
        [FromQuery] string? reason,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;
        
        var query = _context.Reports
            .Include(r => r.Reporter)
            .Include(r => r.Review)
                .ThenInclude(rv => rv.Book)
            .Include(r => r.ModeratedBy)
            .AsQueryable();
        
        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.ToLower();
            query = query.Where(r => 
                (r.Reason != null && r.Reason.ToLower().Contains(searchTerm)) ||
                (r.Details != null && r.Details.ToLower().Contains(searchTerm)) ||
                (r.Reporter != null && r.Reporter.Email != null && r.Reporter.Email.ToLower().Contains(searchTerm)) ||
                (r.Review != null && r.Review.ReviewText != null && r.Review.ReviewText.ToLower().Contains(searchTerm)) ||
                (r.Review != null && r.Review.Book != null && r.Review.Book.Title != null && r.Review.Book.Title.ToLower().Contains(searchTerm)));
        }
        
        if (!string.IsNullOrWhiteSpace(reason))
        {
            query = query.Where(r => r.Reason == reason);
        }
        
        query = sort switch
        {
            "oldest" => query.OrderBy(r => r.CreatedAt),
            "reason" => query.OrderBy(r => r.Reason),
            "reviewer" => query.OrderBy(r => r.Reporter.Email),
            "book" => query.OrderBy(r => r.Review.Book.Title),
            _ => query.OrderByDescending(r => r.CreatedAt)
        };
        
        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        
        var reports = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ReportDto
            {
                Id = r.Id,
                ReporterId = r.ReporterId,
                ReporterEmail = r.Reporter.Email ?? "",
                ReviewId = r.ReviewId,
                ReviewText = r.Review.ReviewText,
                BookTitle = r.Review != null && r.Review.Book != null ? r.Review.Book.Title : "Book unavailable",
                ReviewRating = r.Review != null ? r.Review.Rating : 0,
                Reason = r.Reason,
                Details = r.Details,
                Status = r.Status,
                ModeratedById = r.ModeratedById,
                ModeratedByEmail = r.ModeratedBy != null ? r.ModeratedBy.Email : null,
                CreatedAt = r.CreatedAt,
                ModeratedAt = r.ModeratedAt,
                ModerationNotes = r.ModerationNotes
            })
            .ToListAsync();
        
        return Ok(new PagedResult<ReportDto>
        {
            Items = reports,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        });
    }
    
    [Authorize(Roles = "Admin")]
    [HttpPut("admin/reports/{id}/moderate")]
    public async Task<IActionResult> ModerateReport(Guid id, [FromBody] ModerateReportDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminId))
            return Unauthorized();
        
        var report = await _context.Reports
            .Include(r => r.Review)
            .FirstOrDefaultAsync(r => r.Id == id);
        
        if (report == null)
            return NotFound();
        
        report.Status = dto.Status;
        report.ModeratedById = adminId;
        report.ModeratedAt = DateTime.UtcNow;
        report.ModerationNotes = dto.ModerationNotes;
        
        if (dto.HideReview && dto.Status == ReportStatus.Resolved)
        {
            report.Review.IsHidden = true;
            report.Review.UpdatedAt = DateTime.UtcNow;
        }
        
        await _context.SaveChangesAsync();
        return Ok(new { message = "Report moderated" });
    }
}
