using System.ComponentModel.DataAnnotations;

namespace BookCommunity.Core.Entities;

public class Review
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    
    [Required]
    public Guid BookId { get; set; }
    public Book Book { get; set; } = null!;
    
    [Range(1, 5)]
    public int Rating { get; set; }
    
    [MaxLength(2000)]
    public string? ReviewText { get; set; }
    
    public int LikeCount { get; set; } = 0;
    
    public bool IsHidden { get; set; } = false;
    public bool IsDeleted { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<ReviewLike> ReviewLikes { get; set; } = new List<ReviewLike>();
    public ICollection<Report> Reports { get; set; } = new List<Report>();
}
