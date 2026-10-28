using System.ComponentModel.DataAnnotations;

namespace BookCommunity.Core.Entities;

public class ReviewLike
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    
    [Required]
    public Guid ReviewId { get; set; }
    public Review Review { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
