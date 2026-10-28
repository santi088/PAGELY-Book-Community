using System.ComponentModel.DataAnnotations;

namespace BookCommunity.Core.Entities;

public class ReadingListEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    
    [Required]
    public Guid BookId { get; set; }
    public Book Book { get; set; } = null!;
    
    [Required]
    public ReadingStatus Status { get; set; } = ReadingStatus.WantToRead;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum ReadingStatus
{
    WantToRead = 0,
    Reading = 1,
    Finished = 2
}
