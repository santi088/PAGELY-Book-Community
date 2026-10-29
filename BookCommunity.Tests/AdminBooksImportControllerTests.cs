using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using BookCommunity.Api.DTOs.Books;
using BookCommunity.Api.Services;
using BookCommunity.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace BookCommunity.Tests;

public class AdminBooksImportControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private const string TestJwtKey = "test-key-that-is-at-least-32-characters-long-for-testing";
    private const string TestIssuer = "TestIssuer";
    private const string TestAudience = "TestAudience";

    public AdminBooksImportControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Reconfigure the existing Bearer scheme with test key
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = TestIssuer,
                        ValidAudience = TestAudience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey))
                    };
                });
            });
        });
    }

    private string GenerateToken(string role = "Admin")
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "test-user-id"),
            new(ClaimTypes.Email, "test@example.com"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task ImportBook_Returns401_WhenUnauthenticated()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/books/import",
            new ImportBookRequestDto { OpenLibraryId = "test123" });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ImportBook_Returns403_WhenNonAdmin()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateToken("User"));

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/books/import",
            new ImportBookRequestDto { OpenLibraryId = "test123" });

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ImportBook_Returns400_WhenMissingProviderId()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateToken("Admin"));

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/books/import",
            new ImportBookRequestDto { OpenLibraryId = "" });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ImportBook_Returns404_WhenProviderNotFound()
    {
        // Arrange
        var mockService = new Mock<IOpenLibraryService>();
        mockService.Setup(s => s.GetRecordAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((OpenLibraryBookCandidateDto?)null);

        var client = CreateClientWithMockService(mockService.Object);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateToken("Admin"));

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/books/import",
            new ImportBookRequestDto { OpenLibraryId = "nonexistent" });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ImportBook_CreatesNewBook_WhenNoDuplicate()
    {
        // Arrange - use a unique volume ID
        var volumeId = $"newimport_{Guid.NewGuid():N}";
        var mockService = new Mock<IOpenLibraryService>();
        mockService.Setup(s => s.GetRecordAsync(volumeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenLibraryBookCandidateDto
            {
                OpenLibraryId = volumeId,
                Title = "New Import Book",
                Authors = new List<string> { "New Author" },
                Description = "A new import description",
                Isbn13 = $"978{Guid.NewGuid():N}".Substring(0, 13),
                PublicationDate = "2023-01-01",
                PublicationYear = 2023,
                CoverImageUrl = "https://example.com/cover.jpg",
                Subjects = new List<string> { "Fiction" }
            });

        var client = CreateClientWithMockService(mockService.Object);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateToken("Admin"));

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/books/import",
            new ImportBookRequestDto { OpenLibraryId = volumeId });

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("New Import Book", body);
        Assert.Contains("true", body); // IsNewImport
    }

    [Fact]
    public async Task ImportBook_ReturnsExisting_WhenDuplicateProviderId()
    {
        // Arrange - use a unique volume ID
        var volumeId = $"duptest_{Guid.NewGuid():N}";
        var mockService = new Mock<IOpenLibraryService>();
        mockService.Setup(s => s.GetRecordAsync(volumeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenLibraryBookCandidateDto
            {
                OpenLibraryId = volumeId,
                Title = "Duplicate Test Book",
                Authors = new List<string> { "Dup Author" },
                Description = "A duplicate test",
                Isbn13 = null,
                Isbn10 = null,
                PublicationDate = null,
                PublicationYear = null,
                CoverImageUrl = null,
                Subjects = new List<string>()
            });

        var client = CreateClientWithMockService(mockService.Object);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateToken("Admin"));

        // First import
        var firstResponse = await client.PostAsJsonAsync("/api/admin/books/import",
            new ImportBookRequestDto { OpenLibraryId = volumeId });
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        // Second import - should return existing
        var secondResponse = await client.PostAsJsonAsync("/api/admin/books/import",
            new ImportBookRequestDto { OpenLibraryId = volumeId });

        // Assert
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        var body = await secondResponse.Content.ReadAsStringAsync();
        Assert.Contains("Duplicate Test Book", body);
        Assert.Contains("false", body); // IsNewImport = false
    }

    [Fact]
    public async Task ImportBook_ReturnsExistingIsbnMatch_WhenIsbnExists()
    {
        // Arrange - import a book with an ISBN, then try importing another book with same ISBN
        var volumeId1 = $"isbnbook1_{Guid.NewGuid():N}";
        var volumeId2 = $"isbnbook2_{Guid.NewGuid():N}";
        var testIsbn = $"978{Guid.NewGuid():N}".Substring(0, 13);

        var mockService1 = new Mock<IOpenLibraryService>();
        mockService1.Setup(s => s.GetRecordAsync(volumeId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenLibraryBookCandidateDto
            {
                OpenLibraryId = volumeId1,
                Title = "ISBN Book 1",
                Authors = new List<string> { "Author 1" },
                Description = "First ISBN book",
                Isbn13 = testIsbn,
                Isbn10 = null,
                PublicationDate = "2023-02-01",
                PublicationYear = 2023,
                CoverImageUrl = null,
                Subjects = new List<string>()
            });

        var client1 = CreateClientWithMockService(mockService1.Object);
        client1.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateToken("Admin"));

        // Import first book with ISBN
        var firstResponse = await client1.PostAsJsonAsync("/api/admin/books/import",
            new ImportBookRequestDto { OpenLibraryId = volumeId1 });
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        // Now try to import a different Google Books ID with the same ISBN
        var mockService2 = new Mock<IOpenLibraryService>();
        mockService2.Setup(s => s.GetRecordAsync(volumeId2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenLibraryBookCandidateDto
            {
                OpenLibraryId = volumeId2,
                Title = "ISBN Book 2",
                Authors = new List<string> { "Author 2" },
                Description = "Second ISBN book (same ISBN)",
                Isbn13 = testIsbn, // Same ISBN as book 1
                Isbn10 = null,
                PublicationDate = "2023-03-01",
                PublicationYear = 2023,
                CoverImageUrl = null,
                Subjects = new List<string>()
            });

        var client2 = CreateClientWithMockService(mockService2.Object);
        client2.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateToken("Admin"));

        // Act - should return existing book with ISBN match
        var response = await client2.PostAsJsonAsync("/api/admin/books/import",
            new ImportBookRequestDto { OpenLibraryId = volumeId2 });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("ISBN Book 1", body); // Returns the existing book
        Assert.Contains(testIsbn, body); // ExistingIsbnMatch
        Assert.Contains("false", body); // IsNewImport = false
    }

    [Fact]
    public async Task ImportBook_ImportsWithoutIsbn_WhenMissingIsbn()
    {
        // Arrange - import a book with no ISBN
        var volumeId = $"noisnb_{Guid.NewGuid():N}";
        var mockService = new Mock<IOpenLibraryService>();
        mockService.Setup(s => s.GetRecordAsync(volumeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenLibraryBookCandidateDto
            {
                OpenLibraryId = volumeId,
                Title = "No ISBN Book",
                Authors = new List<string> { "No Author" },
                Description = "A book with no ISBN",
                Isbn13 = null,
                Isbn10 = null,
                PublicationDate = null,
                PublicationYear = null,
                CoverImageUrl = null,
                Subjects = new List<string>()
            });

        var client = CreateClientWithMockService(mockService.Object);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateToken("Admin"));

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/books/import",
            new ImportBookRequestDto { OpenLibraryId = volumeId });

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("No ISBN Book", body);
        Assert.Contains("true", body); // IsNewImport
    }

    [Fact]
    public async Task ImportBook_ReusesExistingGenre_WithoutDuplicate()
    {
        // Arrange - import a book with a genre that already exists
        var volumeId = $"genrebook_{Guid.NewGuid():N}";
        var mockService = new Mock<IOpenLibraryService>();
        mockService.Setup(s => s.GetRecordAsync(volumeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenLibraryBookCandidateDto
            {
                OpenLibraryId = volumeId,
                Title = "Genre Reuse Book",
                Authors = new List<string> { "Genre Author" },
                Description = "Testing genre reuse",
                Isbn13 = null,
                Isbn10 = null,
                PublicationDate = null,
                PublicationYear = null,
                CoverImageUrl = null,
                Subjects = new List<string> { "Fiction" } // "Fiction" already exists in seed data
            });

        var client = CreateClientWithMockService(mockService.Object);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateToken("Admin"));

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/books/import",
            new ImportBookRequestDto { OpenLibraryId = volumeId });

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Verify no duplicate genre was created - count genres named "Fiction"
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var fictionCount = await context.Genres.CountAsync(g => g.Name.ToLower() == "fiction");
        Assert.Equal(1, fictionCount); // Only one "Fiction" genre should exist
    }

    private HttpClient CreateClientWithMockService(IOpenLibraryService mockService)
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Reconfigure the existing Bearer scheme with test key
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = TestIssuer,
                        ValidAudience = TestAudience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey))
                    };
                });
                // Register the mock service as a singleton to override HttpClient registration
                services.AddSingleton(mockService);
            });
        });
        return factory.CreateClient();
    }
}
