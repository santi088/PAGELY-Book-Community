using BookCommunity.Core.Entities;

namespace BookCommunity.Api.DTOs.Reports;

public class ReportDto
{
    public Guid Id { get; set; }
    public string ReporterId { get; set; } = string.Empty;
    public string ReporterEmail { get; set; } = string.Empty;
    public Guid ReviewId { get; set; }
    public string? ReviewText { get; set; }
    public int ReviewRating { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Details { get; set; }
    public ReportStatus Status { get; set; }
    public string? ModeratedById { get; set; }
    public string? ModeratedByEmail { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModeratedAt { get; set; }
    public string? ModerationNotes { get; set; }
}
