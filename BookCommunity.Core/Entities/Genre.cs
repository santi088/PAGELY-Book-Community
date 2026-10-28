using System.ComponentModel.DataAnnotations;

namespace BookCommunity.Core.Entities;

public class Genre
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<BookGenre> BookGenres { get; set; } = new List<BookGenre>();
}
