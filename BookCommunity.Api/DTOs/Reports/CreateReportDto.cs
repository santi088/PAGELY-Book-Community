using System.ComponentModel.DataAnnotations;

namespace BookCommunity.Api.DTOs.Reports;

public class CreateReportDto
{
    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Details { get; set; }
}
