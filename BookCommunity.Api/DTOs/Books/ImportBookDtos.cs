using System.ComponentModel.DataAnnotations;

namespace BookCommunity.Api.DTOs.Books;

public class ImportBookRequestDto
{
    [Required]
    [MaxLength(100)]
    public string OpenLibraryId { get; set; } = string.Empty;
}

public class ImportBookResultDto
{
    public Guid BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public bool IsNewImport { get; set; }
    public string? ExistingIsbnMatch { get; set; }
    public string? Message { get; set; }
}
