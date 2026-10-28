using BookCommunity.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace BookCommunity.Api.DTOs.Reports;

public class ModerateReportDto
{
    [Required]
    public ReportStatus Status { get; set; }
    
    [MaxLength(500)]
    public string? ModerationNotes { get; set; }
    
    public bool HideReview { get; set; } = false;
}
