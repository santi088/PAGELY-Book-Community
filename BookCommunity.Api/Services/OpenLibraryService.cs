using System.Net;
using System.Text.Json;
using BookCommunity.Api.DTOs.Books;
using Microsoft.Extensions.Configuration;

namespace BookCommunity.Api.Services;

public class OpenLibraryService : IOpenLibraryService
{
    private readonly HttpClient _httpClient;
    private readonly string _userAgent;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 40;
    private const int MaxSubjects = 8;
    private const int MaxAuthorsToFetch = 5;

    public OpenLibraryService(HttpClient httpClient, IConfiguration? configuration = null)
    {
        _httpClient = httpClient;
        var contact = configuration?["OpenLibrary:ContactEmail"];
        _userAgent = string.IsNullOrWhiteSpace(contact)
            ? "PAGELY-Book-Community/1.0"
            : $"PAGELY-Book-Community/1.0 (contact: {contact})";
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", _userAgent);
    }

    public async Task<IReadOnlyList<OpenLibraryBookCandidateDto>> SearchAsync(
        string query,
        int pageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<OpenLibraryBookCandidateDto>();
        }

        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var url = $"https://openlibrary.org/search.json?q={Uri.EscapeDataString(query.Trim())}&limit={pageSize}&fields=key,title,author_name,first_publish_year,isbn,cover_i,edition_key,subject";

        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new OpenLibraryException(GetStatusCode(response.StatusCode), GetStatusMessage(response.StatusCode), GetRetryAfterSeconds(response));
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new OpenLibraryException(502, "Open Library returned an empty response.");
            }

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(json);
            }
            catch (JsonException)
            {
                throw new OpenLibraryException(502, "Open Library returned an invalid response.");
            }

            using (document)
            {
                var root = document.RootElement;

                if (!root.TryGetProperty("docs", out var docs) || docs.ValueKind != JsonValueKind.Array)
                {
                    return new List<OpenLibraryBookCandidateDto>();
                }

                var results = new List<OpenLibraryBookCandidateDto>();

                foreach (var doc in docs.EnumerateArray())
                {
                    var candidate = MapSearchDoc(doc);
                    if (candidate != null)
                    {
                        results.Add(candidate);
                    }
                }

                return results;
            }
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new OpenLibraryException(504, "Open Library request timed out.");
        }
        catch (HttpRequestException)
        {
            throw new OpenLibraryException(503, "Unable to reach Open Library.");
        }
        catch (OpenLibraryException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new OpenLibraryException(502, "Open Library request failed.");
        }
    }

    public async Task<OpenLibraryBookCandidateDto?> GetRecordAsync(
        string openLibraryId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(openLibraryId))
        {
            return null;
        }

        var id = openLibraryId.Trim();
        if (id.Length > 100)
        {
            return null;
        }

        var path = NormalizePath(id);
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var url = $"https://openlibrary.org{path}.json";

        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new OpenLibraryException(GetStatusCode(response.StatusCode), GetStatusMessage(response.StatusCode), GetRetryAfterSeconds(response));
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new OpenLibraryException(502, "Open Library returned an empty response.");
            }

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(json);
            }
            catch (JsonException)
            {
                throw new OpenLibraryException(502, "Open Library returned an invalid response.");
            }

            using (document)
            {
                var root = document.RootElement;
                var typeKey = root.TryGetProperty("type", out var typeProp) && typeProp.ValueKind == JsonValueKind.Object && typeProp.TryGetProperty("key", out var typeKeyProp)
                    ? typeKeyProp.GetString()
                    : null;

                var isEdition = typeKey == "/type/edition" || id.EndsWith("M", StringComparison.Ordinal) || path.StartsWith("/books/", StringComparison.Ordinal);

                string? title = root.TryGetProperty("title", out var titleProp) && titleProp.ValueKind == JsonValueKind.String
                    ? titleProp.GetString()
                    : null;

                if (string.IsNullOrWhiteSpace(title))
                {
                    return null;
                }

                var authors = new List<string>();
                var authorKeys = new List<string>();

                if (isEdition && root.TryGetProperty("authors", out var editionAuthors) && editionAuthors.ValueKind == JsonValueKind.Array)
                {
                    foreach (var author in editionAuthors.EnumerateArray())
                    {
                        if (author.ValueKind == JsonValueKind.Object && author.TryGetProperty("key", out var keyProp) && keyProp.ValueKind == JsonValueKind.String)
                        {
                            var key = keyProp.GetString();
                            if (!string.IsNullOrWhiteSpace(key))
                            {
                                authorKeys.Add(key!);
                            }
                        }
                        else if (author.ValueKind == JsonValueKind.String)
                        {
                            var name = author.GetString();
                            if (!string.IsNullOrWhiteSpace(name))
                            {
                                authors.Add(name!);
                            }
                        }
                    }
                }
                else if (!isEdition && root.TryGetProperty("authors", out var workAuthors) && workAuthors.ValueKind == JsonValueKind.Array)
                {
                    foreach (var author in workAuthors.EnumerateArray())
                    {
                        if (author.ValueKind == JsonValueKind.Object && author.TryGetProperty("author", out var authorProp) && authorProp.ValueKind == JsonValueKind.Object && authorProp.TryGetProperty("key", out var keyProp) && keyProp.ValueKind == JsonValueKind.String)
                        {
                            var key = keyProp.GetString();
                            if (!string.IsNullOrWhiteSpace(key))
                            {
                                authorKeys.Add(key!);
                            }
                        }
                    }
                }

                if (authors.Count == 0 && authorKeys.Count > 0)
                {
                    var fetchedAuthors = await FetchAuthorNamesAsync(authorKeys.Take(MaxAuthorsToFetch).ToList(), cancellationToken);
                    authors.AddRange(fetchedAuthors);
                }

                if (authors.Count == 0)
                {
                    if (root.TryGetProperty("by_statement", out var byProp) && byProp.ValueKind == JsonValueKind.String)
                    {
                        var byStatement = byProp.GetString();
                        if (!string.IsNullOrWhiteSpace(byStatement))
                        {
                            authors.Add(byStatement!);
                        }
                    }
                }

                JsonElement? workRoot = null;
                if (isEdition && root.TryGetProperty("works", out var works) && works.ValueKind == JsonValueKind.Array)
                {
                    foreach (var work in works.EnumerateArray())
                    {
                        if (work.ValueKind == JsonValueKind.Object && work.TryGetProperty("key", out var workKeyProp) && workKeyProp.ValueKind == JsonValueKind.String)
                        {
                            var workKey = workKeyProp.GetString();
                            if (!string.IsNullOrWhiteSpace(workKey))
                            {
                                workRoot = await FetchWorkAsync(workKey!, cancellationToken);
                                break;
                            }
                        }
                    }
                }

                var workTitle = workRoot.HasValue ? ExtractTitle(workRoot.Value) : null;
                if (!string.IsNullOrWhiteSpace(workTitle))
                {
                    title = workTitle;
                }

                var description = ExtractDescription(root);
                if (string.IsNullOrWhiteSpace(description) && workRoot.HasValue)
                {
                    description = ExtractDescription(workRoot.Value);
                }

                if (authors.Count == 0 && workRoot.HasValue)
                {
                    var workAuthorKeys = ExtractAuthorKeys(workRoot.Value);
                    if (workAuthorKeys.Count > 0)
                    {
                        var fetchedAuthors = await FetchAuthorNamesAsync(workAuthorKeys.Take(MaxAuthorsToFetch).ToList(), cancellationToken);
                        authors.AddRange(fetchedAuthors);
                    }
                }

                var isbn10 = ExtractStringArray(root, "isbn_10").FirstOrDefault();
                var isbn13 = ExtractStringArray(root, "isbn_13").FirstOrDefault();
                if (string.IsNullOrWhiteSpace(isbn10) || string.IsNullOrWhiteSpace(isbn13))
                {
                    var isbns = ExtractStringArray(root, "isbn");
                    foreach (var isbn in isbns)
                    {
                        if (string.IsNullOrWhiteSpace(isbn))
                        {
                            continue;
                        }

                        if (isbn.Length == 10 && string.IsNullOrWhiteSpace(isbn10))
                        {
                            isbn10 = isbn;
                        }
                        else if (isbn.Length == 13 && string.IsNullOrWhiteSpace(isbn13))
                        {
                            isbn13 = isbn;
                        }
                    }
                }

                var publicationDate = root.TryGetProperty("publish_date", out var pubDateProp) && pubDateProp.ValueKind == JsonValueKind.String
                    ? pubDateProp.GetString()
                    : null;

                int? publicationYear = ExtractPublicationYear(publicationDate);
                if (!publicationYear.HasValue && root.TryGetProperty("first_publish_year", out var firstYearProp) && firstYearProp.ValueKind == JsonValueKind.Number && firstYearProp.TryGetInt32(out var firstYear))
                {
                    publicationYear = firstYear;
                }

                var coverImageUrl = ExtractCoverImageUrl(root);

                var subjects = new List<string>();
                if (root.TryGetProperty("subjects", out var subjectsProp) && subjectsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var subject in subjectsProp.EnumerateArray())
                    {
                        if (subject.ValueKind == JsonValueKind.String)
                        {
                            var name = subject.GetString();
                            if (!string.IsNullOrWhiteSpace(name) && !subjects.Contains(name!, StringComparer.OrdinalIgnoreCase))
                            {
                                subjects.Add(name!);
                                if (subjects.Count >= MaxSubjects)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }

                return new OpenLibraryBookCandidateDto
                {
                    OpenLibraryId = id,
                    Title = title,
                    Authors = authors,
                    Description = description,
                    Isbn10 = isbn10,
                    Isbn13 = isbn13,
                    PublicationDate = publicationDate,
                    PublicationYear = publicationYear,
                    CoverImageUrl = coverImageUrl,
                    Subjects = subjects
                };
            }
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new OpenLibraryException(504, "Open Library request timed out.");
        }
        catch (HttpRequestException)
        {
            throw new OpenLibraryException(503, "Unable to reach Open Library.");
        }
        catch (OpenLibraryException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new OpenLibraryException(502, "Open Library request failed.");
        }
    }

    private static OpenLibraryBookCandidateDto? MapSearchDoc(JsonElement doc)
    {
        var workKey = doc.TryGetProperty("key", out var keyProp) && keyProp.ValueKind == JsonValueKind.String
            ? keyProp.GetString()
            : null;

        string? editionKey = null;
        if (doc.TryGetProperty("edition_key", out var editionProp) && editionProp.ValueKind == JsonValueKind.Array)
        {
            editionKey = editionProp.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String)
                .Select(e => e.GetString())
                .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
        }

        var openLibraryId = !string.IsNullOrWhiteSpace(editionKey) ? editionKey! : workKey ?? string.Empty;
        if (string.IsNullOrWhiteSpace(openLibraryId))
        {
            return null;
        }

        var title = doc.TryGetProperty("title", out var titleProp) && titleProp.ValueKind == JsonValueKind.String
            ? titleProp.GetString() ?? string.Empty
            : string.Empty;

        var authors = new List<string>();
        if (doc.TryGetProperty("author_name", out var authorsProp) && authorsProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var author in authorsProp.EnumerateArray())
            {
                if (author.ValueKind == JsonValueKind.String)
                {
                    var name = author.GetString();
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        authors.Add(name!);
                    }
                }
            }
        }

        var publicationYear = doc.TryGetProperty("first_publish_year", out var yearProp) && yearProp.ValueKind == JsonValueKind.Number && yearProp.TryGetInt32(out var year)
            ? year
            : (int?)null;

        var isbn10 = (string?)null;
        var isbn13 = (string?)null;
        if (doc.TryGetProperty("isbn", out var isbnProp) && isbnProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var isbn in isbnProp.EnumerateArray())
            {
                if (isbn.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var value = isbn.GetString();
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                if (value!.Length == 10 && string.IsNullOrWhiteSpace(isbn10))
                {
                    isbn10 = value;
                }
                else if (value.Length == 13 && string.IsNullOrWhiteSpace(isbn13))
                {
                    isbn13 = value;
                }
            }
        }

        string? coverImageUrl = null;
        if (doc.TryGetProperty("cover_i", out var coverProp) && coverProp.ValueKind == JsonValueKind.Number && coverProp.TryGetInt32(out var coverId))
        {
            coverImageUrl = $"https://covers.openlibrary.org/b/id/{coverId}-M.jpg";
        }

        var subjects = new List<string>();
        if (doc.TryGetProperty("subject", out var subjectProp) && subjectProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var subject in subjectProp.EnumerateArray())
            {
                if (subject.ValueKind == JsonValueKind.String)
                {
                    var name = subject.GetString();
                    if (!string.IsNullOrWhiteSpace(name) && !subjects.Contains(name!, StringComparer.OrdinalIgnoreCase))
                    {
                        subjects.Add(name!);
                        if (subjects.Count >= MaxSubjects)
                        {
                            break;
                        }
                    }
                }
            }
        }

        return new OpenLibraryBookCandidateDto
        {
            OpenLibraryId = openLibraryId,
            Title = title,
            Authors = authors,
            Description = null,
            Isbn10 = isbn10,
            Isbn13 = isbn13,
            PublicationDate = publicationYear?.ToString(),
            PublicationYear = publicationYear,
            CoverImageUrl = coverImageUrl,
            Subjects = subjects
        };
    }

    private async Task<List<string>> FetchAuthorNamesAsync(List<string> authorKeys, CancellationToken cancellationToken)
    {
        var names = new List<string>();
        var tasks = authorKeys.Select(key => FetchAuthorNameAsync(key, cancellationToken)).ToList();
        var results = await Task.WhenAll(tasks);
        foreach (var name in results)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                names.Add(name!);
            }
        }
        return names;
    }

    private async Task<string?> FetchAuthorNameAsync(string authorKey, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync($"https://openlibrary.org{authorKey}.json", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            using var document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("name", out var nameProp) && nameProp.ValueKind == JsonValueKind.String)
            {
                return nameProp.GetString();
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private async Task<string?> FetchWorkDescriptionAsync(string workKey, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync($"https://openlibrary.org{workKey}.json", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            using var document = JsonDocument.Parse(json);
            return ExtractDescription(document.RootElement);
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractDescription(JsonElement root)
    {
        if (!root.TryGetProperty("description", out var descProp))
        {
            return null;
        }

        if (descProp.ValueKind == JsonValueKind.String)
        {
            return descProp.GetString();
        }

        if (descProp.ValueKind == JsonValueKind.Object && descProp.TryGetProperty("value", out var valueProp) && valueProp.ValueKind == JsonValueKind.String)
        {
            return valueProp.GetString();
        }

        return null;
    }

    private static List<string> ExtractStringArray(JsonElement root, string propertyName)
    {
        var values = new List<string>();
        if (root.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in prop.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var value = item.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        values.Add(value!);
                    }
                }
            }
        }
        return values;
    }

    private static string? ExtractCoverImageUrl(JsonElement root)
    {
        if (root.TryGetProperty("covers", out var coversProp) && coversProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var cover in coversProp.EnumerateArray())
            {
                if (cover.ValueKind == JsonValueKind.Number && cover.TryGetInt32(out var coverId))
                {
                    return $"https://covers.openlibrary.org/b/id/{coverId}-M.jpg";
                }
            }
        }

        return null;
    }

    private static int? ExtractPublicationYear(string? publicationDate)
    {
        if (string.IsNullOrWhiteSpace(publicationDate))
        {
            return null;
        }

        var trimmed = publicationDate.Trim();
        if (DateTime.TryParse(trimmed, out var date))
        {
            return date.Year;
        }

        if (int.TryParse(trimmed, out var year) && year >= 1000 && year <= 9999)
        {
            return year;
        }

        var digits = new string(trimmed.TakeWhile(char.IsDigit).ToArray());
        if (digits.Length == 4 && int.TryParse(digits, out var parsedYear))
        {
            return parsedYear;
        }

        return null;
    }

    private static string NormalizePath(string id)
    {
        if (id.StartsWith("/works/", StringComparison.Ordinal) || id.StartsWith("/books/", StringComparison.Ordinal))
        {
            return id;
        }

        if (id.StartsWith("OL", StringComparison.Ordinal) && id.EndsWith("M", StringComparison.Ordinal))
        {
            return "/books/" + id;
        }

        if (id.StartsWith("OL", StringComparison.Ordinal) && id.EndsWith("W", StringComparison.Ordinal))
        {
            return "/works/" + id;
        }

        return string.Empty;
    }



    private async Task<JsonElement?> FetchWorkAsync(string workKey, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync($"https://openlibrary.org{workKey}.json", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractTitle(JsonElement root)
    {
        if (root.TryGetProperty("title", out var titleProp) && titleProp.ValueKind == JsonValueKind.String)
        {
            return titleProp.GetString();
        }

        return null;
    }

    private static List<string> ExtractAuthorKeys(JsonElement root)
    {
        var keys = new List<string>();
        if (root.TryGetProperty("authors", out var authorsProp) && authorsProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var author in authorsProp.EnumerateArray())
            {
                if (author.ValueKind == JsonValueKind.Object &&
                    author.TryGetProperty("author", out var authorProp) &&
                    authorProp.ValueKind == JsonValueKind.Object &&
                    authorProp.TryGetProperty("key", out var keyProp) &&
                    keyProp.ValueKind == JsonValueKind.String)
                {
                    var key = keyProp.GetString();
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        keys.Add(key!);
                    }
                }
            }
        }

        return keys;
    }

    private static int GetStatusCode(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.TooManyRequests => 429,
        HttpStatusCode.Forbidden => 403,
        HttpStatusCode.NotFound => 404,
        HttpStatusCode.RequestTimeout => 408,
        _ => (int)statusCode >= 400 && (int)statusCode < 600 ? (int)statusCode : 502
    };

    private static string GetStatusMessage(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.TooManyRequests => "Open Library rate limit exceeded. Please try again later.",
        HttpStatusCode.Forbidden => "Open Library access is restricted.",
        HttpStatusCode.NotFound => "Open Library record was not found.",
        HttpStatusCode.RequestTimeout => "Open Library request timed out.",
        _ => $"Open Library request failed with status {(int)statusCode}."
    };

    private static int? GetRetryAfterSeconds(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter is null)
        {
            return null;
        }

        if (retryAfter.Delta.HasValue)
        {
            return (int)Math.Ceiling(retryAfter.Delta.Value.TotalSeconds);
        }

        return null;
    }
}
