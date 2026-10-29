namespace BookCommunity.Api.DTOs.Books;

public class OpenLibraryBookCandidateDto
{
    public string OpenLibraryId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<string> Authors { get; set; } = new();
    public string? Description { get; set; }
    public string? Isbn10 { get; set; }
    public string? Isbn13 { get; set; }
    public string? PublicationDate { get; set; }
    public int? PublicationYear { get; set; }
    public string? CoverImageUrl { get; set; }
    public List<string> Subjects { get; set; } = new();
}
