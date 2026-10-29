using System.Net;
using System.Text;
using System.Text.Json;
using BookCommunity.Api.DTOs.Books;
using BookCommunity.Api.Services;
using Moq;
using Moq.Protected;
using Xunit;

namespace BookCommunity.Tests;

public class OpenLibraryServiceTests
{
    private static readonly string ValidSearchJson = """
        {
          "numFound": 2,
          "docs": [
            {
              "key": "/works/OL1W",
              "title": "Test Book",
              "author_name": ["Author One", "Author Two"],
              "first_publish_year": 2023,
              "isbn": ["1234567890", "9781234567890"],
              "cover_i": 123,
              "edition_key": ["OL1M"],
              "subject": ["Fiction", "Mystery"]
            },
            {
              "key": "/works/OL2W",
              "title": "Minimal Book"
            }
          ]
        }
        """;

    private static readonly string EmptySearchJson = """
        {
          "numFound": 0,
          "docs": []
        }
        """;

    private static HttpMessageHandler CreateHandler(HttpResponseMessage response)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(response)
            .Verifiable();
        return handlerMock.Object;
    }

    [Fact]
    public async Task SearchAsync_ReturnsCandidates_ForValidResponse()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ValidSearchJson, Encoding.UTF8, "application/json")
        };
        var service = new OpenLibraryService(new HttpClient(CreateHandler(response)));

        var results = await service.SearchAsync("test query", 20, CancellationToken.None);

        Assert.Equal(2, results.Count);
        var candidate = results[0];
        Assert.Equal("OL1M", candidate.OpenLibraryId);
        Assert.Equal("Test Book", candidate.Title);
        Assert.Equal(new[] { "Author One", "Author Two" }, candidate.Authors);
        Assert.Equal("1234567890", candidate.Isbn10);
        Assert.Equal("9781234567890", candidate.Isbn13);
        Assert.Equal(2023, candidate.PublicationYear);
        Assert.Equal("https://covers.openlibrary.org/b/id/123-M.jpg", candidate.CoverImageUrl);
        Assert.Equal(new[] { "Fiction", "Mystery" }, candidate.Subjects);
    }

    [Fact]
    public async Task SearchAsync_HandlesMissingOptionalFields()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ValidSearchJson, Encoding.UTF8, "application/json")
        };
        var service = new OpenLibraryService(new HttpClient(CreateHandler(response)));

        var results = await service.SearchAsync("minimal", 20, CancellationToken.None);

        var candidate = results[1];
        Assert.Equal("/works/OL2W", candidate.OpenLibraryId);
        Assert.Equal("Minimal Book", candidate.Title);
        Assert.Empty(candidate.Authors);
        Assert.Null(candidate.Isbn10);
        Assert.Null(candidate.Isbn13);
        Assert.Null(candidate.CoverImageUrl);
        Assert.Empty(candidate.Subjects);
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmpty_ForEmptyDocs()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(EmptySearchJson, Encoding.UTF8, "application/json")
        };
        var service = new OpenLibraryService(new HttpClient(CreateHandler(response)));

        var results = await service.SearchAsync("nonexistent", 20, CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmpty_ForNullOrWhitespaceQuery()
    {
        var service = new OpenLibraryService(new HttpClient(CreateHandler(new HttpResponseMessage(HttpStatusCode.OK))));

        Assert.Empty(await service.SearchAsync(null!, 20, CancellationToken.None));
        Assert.Empty(await service.SearchAsync("", 20, CancellationToken.None));
        Assert.Empty(await service.SearchAsync("   ", 20, CancellationToken.None));
    }

    [Fact]
    public async Task SearchAsync_ThrowsOpenLibraryException_ForNonSuccessStatusCode()
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("error", Encoding.UTF8, "application/json")
        };
        var service = new OpenLibraryService(new HttpClient(CreateHandler(response)));

        var ex = await Assert.ThrowsAsync<OpenLibraryException>(() => service.SearchAsync("test", 20, CancellationToken.None));
        Assert.Equal(500, ex.StatusCode);
    }

    [Fact]
    public async Task SearchAsync_ThrowsOpenLibraryException_ForRateLimitedResponse()
    {
        var response = new HttpResponseMessage((HttpStatusCode)429)
        {
            Content = new StringContent("rate limited", Encoding.UTF8, "application/json")
        };
        var service = new OpenLibraryService(new HttpClient(CreateHandler(response)));

        var ex = await Assert.ThrowsAsync<OpenLibraryException>(() => service.SearchAsync("test", 20, CancellationToken.None));
        Assert.Equal(429, ex.StatusCode);
    }

    [Fact]
    public async Task SearchAsync_ThrowsOpenLibraryException_ForMalformedJson()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{ not valid json", Encoding.UTF8, "application/json")
        };
        var service = new OpenLibraryService(new HttpClient(CreateHandler(response)));

        var ex = await Assert.ThrowsAsync<OpenLibraryException>(() => service.SearchAsync("test", 20, CancellationToken.None));
        Assert.Equal(502, ex.StatusCode);
    }

    [Fact]
    public async Task SearchAsync_ThrowsOpenLibraryException_ForTimeout()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new TaskCanceledException());

        var service = new OpenLibraryService(new HttpClient(handlerMock.Object));

        var ex = await Assert.ThrowsAsync<OpenLibraryException>(() => service.SearchAsync("test", 20, CancellationToken.None));
        Assert.Equal(504, ex.StatusCode);
    }

    [Fact]
    public async Task SearchAsync_ThrowsOpenLibraryException_ForNetworkError()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Network down"));

        var service = new OpenLibraryService(new HttpClient(handlerMock.Object));

        var ex = await Assert.ThrowsAsync<OpenLibraryException>(() => service.SearchAsync("test", 20, CancellationToken.None));
        Assert.Equal(503, ex.StatusCode);
    }

    private sealed class RoutingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public RoutingHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }

    [Fact]
    public async Task GetRecordAsync_ReturnsMappedEdition()
    {
        var editionJson = """
            {
              "title": "Volume Test Book",
              "authors": [{ "key": "/authors/OL1A" }],
              "publish_date": "2022-10-01",
              "isbn_10": ["1234567890"],
              "isbn_13": ["9780000000001"],
              "covers": [456],
              "subjects": ["Science Fiction"],
              "works": [{ "key": "/works/OL1W" }]
            }
            """;
        var authorJson = """{ "name": "Volume Author" }""";
        var workJson = """{ "description": "A volume test description" }""";

        var handler = new RoutingHandler(request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path.Contains("/authors/OL1A.json"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(authorJson, Encoding.UTF8, "application/json")
                };
            }
            if (path.Contains("/works/OL1W.json"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(workJson, Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(editionJson, Encoding.UTF8, "application/json")
            };
        });

        var service = new OpenLibraryService(new HttpClient(handler));
        var result = await service.GetRecordAsync("OL1M", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("OL1M", result!.OpenLibraryId);
        Assert.Equal("Volume Test Book", result.Title);
        Assert.Equal(new[] { "Volume Author" }, result.Authors);
        Assert.Equal("A volume test description", result.Description);
        Assert.Equal("1234567890", result.Isbn10);
        Assert.Equal("9780000000001", result.Isbn13);
        Assert.Equal("2022-10-01", result.PublicationDate);
        Assert.Equal(2022, result.PublicationYear);
        Assert.Equal("https://covers.openlibrary.org/b/id/456-M.jpg", result.CoverImageUrl);
        Assert.Equal(new[] { "Science Fiction" }, result.Subjects);
    }

    [Fact]
    public async Task GetRecordAsync_ReturnsNull_ForNotFound()
    {
        var handler = CreateHandler(new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("not found", Encoding.UTF8, "application/json")
        });
        var service = new OpenLibraryService(new HttpClient(handler));

        var result = await service.GetRecordAsync("OL999M", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetRecordAsync_ReturnsNull_ForMissingTitle()
    {
        var json = """{ "authors": [{ "key": "/authors/OL1A" }] }""";
        var handler = CreateHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
        var service = new OpenLibraryService(new HttpClient(handler));

        var result = await service.GetRecordAsync("OL1M", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetRecordAsync_ReturnsNull_ForEmptyId()
    {
        var service = new OpenLibraryService(new HttpClient(CreateHandler(new HttpResponseMessage(HttpStatusCode.OK))));

        Assert.Null(await service.GetRecordAsync(null!, CancellationToken.None));
        Assert.Null(await service.GetRecordAsync("", CancellationToken.None));
        Assert.Null(await service.GetRecordAsync("   ", CancellationToken.None));
    }

    [Fact]
    public async Task GetRecordAsync_ThrowsOpenLibraryException_ForNetworkError()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Network down"));

        var service = new OpenLibraryService(new HttpClient(handlerMock.Object));

        var ex = await Assert.ThrowsAsync<OpenLibraryException>(() => service.GetRecordAsync("OL1M", CancellationToken.None));
        Assert.Equal(503, ex.StatusCode);
    }
}
