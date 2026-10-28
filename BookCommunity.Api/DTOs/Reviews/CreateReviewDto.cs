using System.ComponentModel.DataAnnotations;

namespace BookCommunity.Api.DTOs.Reviews;

public class CreateReviewDto
{
    [Range(1, 5)]
    public int Rating { get; set; }
    
    [MaxLength(2000)]
    public string? ReviewText { get; set; }
}
