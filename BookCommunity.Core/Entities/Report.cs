using System.ComponentModel.DataAnnotations;

namespace BookCommunity.Core.Entities;

public class Report
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public string ReporterId { get; set; } = string.Empty;
    public ApplicationUser Reporter { get; set; } = null!;
    
    [Required]
    public Guid ReviewId { get; set; }
    public Review Review { get; set; } = null!;
    
    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Details { get; set; }
    
    public ReportStatus Status { get; set; } = ReportStatus.Pending;
    
    public string? ModeratedById { get; set; }
    public ApplicationUser? ModeratedBy { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModeratedAt { get; set; }
    
    [MaxLength(500)]
    public string? ModerationNotes { get; set; }
}

public enum ReportStatus
{
    Pending = 0,
    Dismissed = 1,
    Resolved = 2
}
