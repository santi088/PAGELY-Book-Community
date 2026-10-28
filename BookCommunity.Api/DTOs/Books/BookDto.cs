namespace BookCommunity.Api.DTOs.Books;

public class BookDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? ISBN { get; set; }
    public int? PublicationYear { get; set; }
    public DateTime PublicationDate { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public List<string> Genres { get; set; } = new List<string>();
}
