using BookCommunity.Api.DTOs.Books;

namespace BookCommunity.Api.Services;

public interface IOpenLibraryService
{
    Task<IReadOnlyList<OpenLibraryBookCandidateDto>> SearchAsync(string query, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<OpenLibraryBookCandidateDto?> GetRecordAsync(string openLibraryId, CancellationToken cancellationToken = default);
}
