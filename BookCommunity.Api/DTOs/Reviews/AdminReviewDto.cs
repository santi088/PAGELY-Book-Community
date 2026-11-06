namespace BookCommunity.Api.DTOs.Reviews;

public class AdminReviewDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public Guid BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? ReviewText { get; set; }
    public int LikeCount { get; set; }
    public bool IsHidden { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
