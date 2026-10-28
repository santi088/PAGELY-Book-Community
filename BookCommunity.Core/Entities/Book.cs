using System.ComponentModel.DataAnnotations;

namespace BookCommunity.Core.Entities;

public class Book
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string Author { get; set; } = string.Empty;
    
    [MaxLength(2000)]
    public string? Description { get; set; }
    
    [MaxLength(1000)]
    public string? CoverImageUrl { get; set; }
    
    [MaxLength(20)]
    public string? ISBN { get; set; }
    
    public int? PublicationYear { get; set; }
    
    public DateTime PublicationDate { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public double AverageRating { get; set; } = 0;
    
    public int ReviewCount { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<BookGenre> BookGenres { get; set; } = new List<BookGenre>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<ReadingListEntry> ReadingListEntries { get; set; } = new List<ReadingListEntry>();
}
