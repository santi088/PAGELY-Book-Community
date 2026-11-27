using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using BookCommunity.Core.Entities;
using BookCommunity.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace BookCommunity.Tests;

public class DashboardControllerTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private const string TestJwtKey = "test-key-that-is-at-least-32-characters-long-for-testing";
    private const string TestIssuer = "TestIssuer";
    private const string TestAudience = "TestAudience";

    public DashboardControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
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
                foreach (var d in services.Where(d => d.ServiceType == typeof(ApplicationDbContext) || d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)).ToList())
                    services.Remove(d);
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase("DashboardTestDb"));
            });
        });
    }

    public Task InitializeAsync() => CleanupDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private async Task CleanupDatabaseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        ctx.Reports.RemoveRange(ctx.Reports);
        ctx.ReadingListEntries.RemoveRange(ctx.ReadingListEntries);
        ctx.BookGenres.RemoveRange(ctx.BookGenres);
        ctx.Reviews.RemoveRange(ctx.Reviews);
        ctx.Books.RemoveRange(ctx.Books);
        ctx.Genres.RemoveRange(ctx.Genres);
        await ctx.SaveChangesAsync();

        var um = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var users = um.Users.ToList();
        foreach (var u in users)
            await um.DeleteAsync(u);
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

    private async Task<string> CreateUserAsync(string email, string role = "User", bool isSuspended = false, DateTime? createdAt = null)
    {
        using var scope = _factory.Services.CreateScope();
        var um = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var rm = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await rm.RoleExistsAsync(role))
            await rm.CreateAsync(new IdentityRole(role));

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "Test",
            LastName = "User",
            IsSuspended = isSuspended,
            CreatedAt = createdAt ?? DateTime.UtcNow.AddDays(-10)
        };
        await um.CreateAsync(user, "TestPass123!");
        await um.AddToRoleAsync(user, role);
        return user.Id; // Return the GUID user ID, not email
    }

    private async Task<Guid> CreateBookAsync(string title, string author, bool isActive = true, double avgRating = 0, int reviewCount = 0, DateTime? createdAt = null, string? coverImageUrl = null, string? description = null)
    {
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var book = new Book
        {
            Title = title,
            Author = author,
            IsActive = isActive,
            AverageRating = avgRating,
            ReviewCount = reviewCount,
            CoverImageUrl = coverImageUrl,
            Description = description,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        ctx.Books.Add(book);
        await ctx.SaveChangesAsync();
        return book.Id;
    }

    private async Task<Guid> CreateGenreAsync(string name)
    {
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var genre = new Genre { Name = name, CreatedAt = DateTime.UtcNow };
        ctx.Genres.Add(genre);
        await ctx.SaveChangesAsync();
        return genre.Id;
    }

    private async Task LinkBookGenreAsync(Guid bookId, Guid genreId)
    {
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        ctx.BookGenres.Add(new BookGenre { BookId = bookId, GenreId = genreId });
        await ctx.SaveChangesAsync();
    }

    private async Task<Guid> CreateReviewAsync(Guid bookId, string userId, int rating = 5, bool isHidden = false, bool isDeleted = false, DateTime? createdAt = null)
    {
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var review = new Review
        {
            UserId = userId,
            BookId = bookId,
            Rating = rating,
            ReviewText = "Test review",
            IsHidden = isHidden,
            IsDeleted = isDeleted,
            LikeCount = 0,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        ctx.Reviews.Add(review);
        await ctx.SaveChangesAsync();
        return review.Id;
    }

    private async Task<Guid> CreateReportAsync(Guid reviewId, string reporterId, string reason = "Spam", ReportStatus status = ReportStatus.Pending, DateTime? createdAt = null, DateTime? moderatedAt = null)
    {
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var report = new Report
        {
            ReporterId = reporterId,
            ReviewId = reviewId,
            Reason = reason,
            Status = status,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            ModeratedAt = moderatedAt
        };
        ctx.Reports.Add(report);
        await ctx.SaveChangesAsync();
        return report.Id;
    }

    private async Task<Guid> CreateReadingListEntryAsync(string userId, Guid bookId)
    {
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var entry = new ReadingListEntry
        {
            UserId = userId,
            BookId = bookId,
            Status = ReadingStatus.WantToRead,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        ctx.ReadingListEntries.Add(entry);
        await ctx.SaveChangesAsync();
        return entry.Id;
    }

    private static async Task<HttpResponseMessage> GetWithToken(HttpClient client, string path, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
    }

    private static async Task<JsonElement> GetJsonElement(HttpResponseMessage response, string propertyName)
    {
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty(propertyName);
    }

    private static int GetInt(JsonElement element) => element.GetInt32();

    [Fact]
    public async Task Dashboard_ReturnsCounts_WithSampleData()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = GenerateToken("Admin");

        await CreateUserAsync("admin@test.com", "Admin");
        await CreateUserAsync("user1@test.com", "User", isSuspended: false);
        await CreateUserAsync("user2@test.com", "User", isSuspended: true);

        var book1 = await CreateBookAsync("Active Book 1", "Author A", isActive: true, avgRating: 4.5, reviewCount: 10, coverImageUrl: "http://example.com/cover.jpg", description: "A great book");
        var book2 = await CreateBookAsync("Hidden Book", "Author B", isActive: false, avgRating: 3.0, reviewCount: 2);
        var book3 = await CreateBookAsync("No Cover Book", "Author C", isActive: true, avgRating: 0, reviewCount: 0, coverImageUrl: null, description: "Has description");
        var book4 = await CreateBookAsync("No Desc Book", "Author D", isActive: true, avgRating: 0, reviewCount: 0, coverImageUrl: "http://example.com/cover.jpg", description: null);

        var genre1 = await CreateGenreAsync("Fiction");
        var genre2 = await CreateGenreAsync("Mystery");
        await LinkBookGenreAsync(book1, genre1);
        await LinkBookGenreAsync(book2, genre2);

        // Create reviewer1 user first
        await CreateUserAsync("reviewer1@test.com", "User");
        string user1Id;
        using (var scope = _factory.Services.CreateScope())
        {
            var um = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var u = await um.FindByEmailAsync("reviewer1@test.com");
            user1Id = u!.Id;
        }

        var review1 = await CreateReviewAsync(book1, user1Id, rating: 5, isHidden: false, isDeleted: false, createdAt: DateTime.UtcNow.AddHours(-1));
        var review2 = await CreateReviewAsync(book1, user1Id, rating: 3, isHidden: true, isDeleted: false, createdAt: DateTime.UtcNow.AddHours(-2));
        var review3 = await CreateReviewAsync(book2, user1Id, rating: 1, isHidden: false, isDeleted: true, createdAt: DateTime.UtcNow.AddHours(-3));

        var report1 = await CreateReportAsync(review1, user1Id, reason: "Spam", status: ReportStatus.Pending, createdAt: DateTime.UtcNow.AddHours(-4));
        var report2 = await CreateReportAsync(review2, user1Id, reason: "Harassment", status: ReportStatus.Resolved, createdAt: DateTime.UtcNow.AddHours(-5), moderatedAt: DateTime.UtcNow.AddHours(-4));
        var report3 = await CreateReportAsync(review3, user1Id, reason: "Spam", status: ReportStatus.Dismissed, createdAt: DateTime.UtcNow.AddDays(-10));

        await CreateReadingListEntryAsync(user1Id, book1);
        await CreateReadingListEntryAsync(user1Id, book1);
        await CreateReadingListEntryAsync(user1Id, book2);

        // Act
        var response = await GetWithToken(client, $"/api/admin/dashboard?days=7", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        // Assert context strip
        Assert.Equal(3, GetInt(root.GetProperty("visibleBooks")));
        Assert.Equal(1, GetInt(root.GetProperty("hiddenBooks")));
        Assert.True(GetInt(root.GetProperty("totalGenres")) >= 2);
        Assert.Equal(3, GetInt(root.GetProperty("activeUsers")));
        Assert.Equal(1, GetInt(root.GetProperty("suspendedUsers")));
        Assert.True(GetInt(root.GetProperty("visibleReviews")) >= 1);

        // Assert needs attention
        Assert.Equal(1, GetInt(root.GetProperty("pendingReports")));
        Assert.Equal(2, GetInt(root.GetProperty("newReportsLast7Days")));
        Assert.Equal(2, GetInt(root.GetProperty("hiddenRemovedReviewsLast7Days")));

        // Assert trends has reportsByReason array
        Assert.True(root.TryGetProperty("reportsByReason", out var reasons));
        Assert.True(reasons.ValueKind == JsonValueKind.Array);
    }

    [Fact]
    public async Task Dashboard_ReturnsEmptyState_WhenDatabaseIsEmpty()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = GenerateToken("Admin");

        // Act
        var response = await GetWithToken(client, $"/api/admin/dashboard?days=7", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        // Assert all zeros
        Assert.Equal(0, GetInt(root.GetProperty("visibleBooks")));
        Assert.Equal(0, GetInt(root.GetProperty("hiddenBooks")));
        Assert.Equal(0, GetInt(root.GetProperty("totalGenres")));
        Assert.Equal(0, GetInt(root.GetProperty("activeUsers")));
        Assert.Equal(0, GetInt(root.GetProperty("suspendedUsers")));
        Assert.Equal(0, GetInt(root.GetProperty("visibleReviews")));
        Assert.Equal(0, GetInt(root.GetProperty("pendingReports")));
        Assert.Equal(0, GetInt(root.GetProperty("oldestPendingReportDays")));
        Assert.Equal(0, GetInt(root.GetProperty("newReportsLast7Days")));
        Assert.Equal(0, GetInt(root.GetProperty("hiddenRemovedReviewsLast7Days")));
    }

    [Fact]
    public async Task Dashboard_Returns403_ForNonAdmin()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = GenerateToken("User");

        // Act
        var response = await GetWithToken(client, $"/api/admin/dashboard?days=7", token);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_AcceptsDays30_Parameter()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = GenerateToken("Admin");

        await CreateUserAsync("user@test.com", "User");

        // Act - days=30 should work
        var response = await GetWithToken(client, $"/api/admin/dashboard?days=30", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Act - invalid days should default to 7
        response = await GetWithToken(client, $"/api/admin/dashboard?days=999", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_ReturnsZeroFilledDayRanges_7Days()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = GenerateToken("Admin");

        // No data - should still return 7 days with 0 counts
        var response = await GetWithToken(client, $"/api/admin/dashboard?days=7", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var reviews = root.GetProperty("newReviewsPerDay");
        Assert.Equal(7, reviews.GetArrayLength());
        // All counts should be 0 with no data
        foreach (var item in reviews.EnumerateArray())
        {
            Assert.Equal(0, item.GetProperty("count").GetInt32());
        }

        var users = root.GetProperty("newUsersPerDay");
        Assert.Equal(7, users.GetArrayLength());
    }

    [Fact]
    public async Task Dashboard_ReturnsZeroFilledDayRanges_30Days()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = GenerateToken("Admin");

        var response = await GetWithToken(client, $"/api/admin/dashboard?days=30", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var reviews = root.GetProperty("newReviewsPerDay");
        Assert.Equal(30, reviews.GetArrayLength());
        foreach (var item in reviews.EnumerateArray())
        {
            Assert.Equal(0, item.GetProperty("count").GetInt32());
        }
    }

    [Fact]
    public async Task Dashboard_HasPreviousPeriodComparisons()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = GenerateToken("Admin");

        await CreateUserAsync("admin@test.com", "Admin");
        await CreateUserAsync("user1@test.com", "User");

        var book1 = await CreateBookAsync("Book 1", "Author A", isActive: true, reviewCount: 5);
        var book2 = await CreateBookAsync("Book 2", "Author B", isActive: true, reviewCount: 2);

        var userId = await CreateUserAsync("reviewer@test.com", "User");

        // Create reviews in current period (within last 7 days)
        await CreateReviewAsync(book1, userId, createdAt: DateTime.UtcNow.AddHours(-1));
        await CreateReviewAsync(book1, userId, createdAt: DateTime.UtcNow.AddHours(-2));

        // Create reports in current period
        var reviewId = await CreateReviewAsync(book2, userId, createdAt: DateTime.UtcNow.AddHours(-3));
        await CreateReportAsync(reviewId, userId, reason: "Spam", status: ReportStatus.Pending, createdAt: DateTime.UtcNow.AddHours(-4));

        // Create reports in previous period (8-14 days ago)
        var oldReviewId = await CreateReviewAsync(book1, userId, createdAt: DateTime.UtcNow.AddDays(-10));
        await CreateReportAsync(oldReviewId, userId, reason: "Spam", status: ReportStatus.Pending, createdAt: DateTime.UtcNow.AddDays(-10));
        await CreateReportAsync(oldReviewId, userId, reason: "Spam", status: ReportStatus.Resolved, createdAt: DateTime.UtcNow.AddDays(-9));

        // Act
        var response = await GetWithToken(client, $"/api/admin/dashboard?days=7", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        // Assert previous period comparisons exist
        Assert.True(root.TryGetProperty("previousPeriodPendingReports", out var prevPending));
        Assert.True(root.TryGetProperty("previousPeriodNewReports", out var prevNewReports));
        Assert.True(root.TryGetProperty("previousPeriodHiddenRemovedReviews", out var prevHiddenRemoved));
    }

    [Fact]
    public async Task Dashboard_HighestRated_RequiresMin3Reviews()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = GenerateToken("Admin");

        // Book with only 2 reviews (below threshold of 3)
        var bookLow = await CreateBookAsync("Low Review Count Book", "Author A", isActive: true, reviewCount: 2, avgRating: 5.0);
        // Book with 3 reviews (meets threshold)
        var bookHigh = await CreateBookAsync("High Review Count Book", "Author B", isActive: true, reviewCount: 5, avgRating: 4.0);

        var userId = await CreateUserAsync("reviewer@test.com", "User");

        // Act
        var response = await GetWithToken(client, $"/api/admin/dashboard?days=7", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var highestRated = root.GetProperty("top5HighestRated");
        var titles = highestRated.EnumerateArray().Select(i => i.GetProperty("title").GetString()!).ToList();

        // "Low Review Count Book" should NOT appear (only 2 reviews < 3 threshold)
        Assert.DoesNotContain("Low Review Count Book", titles);
        // "High Review Count Book" should appear (5 reviews >= 3 threshold)
        Assert.Contains("High Review Count Book", titles);
    }

    [Fact]
    public async Task Dashboard_MostReportedBooks_RequiresMin2Reports()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = GenerateToken("Admin");

        var book1 = await CreateBookAsync("Reported Book 1", "Author A", isActive: true);
        var book2 = await CreateBookAsync("Reported Book 2", "Author B", isActive: true);

        var userId = await CreateUserAsync("reviewer@test.com", "User");

        // Book 1: 2 reports (meets threshold)
        var review1a = await CreateReviewAsync(book1, userId, createdAt: DateTime.UtcNow.AddHours(-1));
        var review1b = await CreateReviewAsync(book1, userId, createdAt: DateTime.UtcNow.AddHours(-2));
        await CreateReportAsync(review1a, userId, reason: "Spam", status: ReportStatus.Pending);
        await CreateReportAsync(review1b, userId, reason: "Spam", status: ReportStatus.Pending);

        // Book 2: 1 report (below threshold)
        var review2a = await CreateReviewAsync(book2, userId, createdAt: DateTime.UtcNow.AddHours(-3));
        await CreateReportAsync(review2a, userId, reason: "Spam", status: ReportStatus.Pending);

        // Act
        var response = await GetWithToken(client, $"/api/admin/dashboard?days=7", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var mostReported = root.GetProperty("top5MostReportedBooks");
        var titles = mostReported.EnumerateArray().Select(i => i.GetProperty("name").GetString()!).ToList();

        Assert.Contains("Reported Book 1", titles);
        Assert.DoesNotContain("Reported Book 2", titles);
    }

    [Fact]
    public async Task Dashboard_MostReportedReviewers_RequiresMin2Reports()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = GenerateToken("Admin");

        var book1 = await CreateBookAsync("Book 1", "Author A", isActive: true);

        // Reviewer 1: 2 reports (meets threshold)
        var reviewer1Email = "reviewer1@test.com";
        var reviewer1Id = await CreateUserAsync(reviewer1Email, "User");
        var review1a = await CreateReviewAsync(book1, reviewer1Id, createdAt: DateTime.UtcNow.AddHours(-1));
        var review1b = await CreateReviewAsync(book1, reviewer1Id, createdAt: DateTime.UtcNow.AddHours(-2));
        await CreateReportAsync(review1a, reviewer1Id, reason: "Spam", status: ReportStatus.Pending);
        await CreateReportAsync(review1b, reviewer1Id, reason: "Spam", status: ReportStatus.Pending);

        // Reviewer 2: 1 report (below threshold)
        var reviewer2Email = "reviewer2@test.com";
        var reviewer2Id = await CreateUserAsync(reviewer2Email, "User");
        var review2a = await CreateReviewAsync(book1, reviewer2Id, createdAt: DateTime.UtcNow.AddHours(-3));
        await CreateReportAsync(review2a, reviewer2Id, reason: "Spam", status: ReportStatus.Pending);

        // Act
        var response = await GetWithToken(client, $"/api/admin/dashboard?days=7", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var mostReported = root.GetProperty("top5MostReportedReviewers");
        var names = mostReported.EnumerateArray().Select(i => i.GetProperty("name").GetString()!).ToList();

        Assert.Contains(reviewer1Email, names);
        Assert.DoesNotContain(reviewer2Email, names);
    }

    [Fact]
    public async Task Dashboard_StatusBreakdown_HasPercentages()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = GenerateToken("Admin");

        await CreateUserAsync("admin@test.com", "Admin");
        await CreateUserAsync("user1@test.com", "User");

        var book1 = await CreateBookAsync("Book 1", "Author A", isActive: true);
        var userId = await CreateUserAsync("reviewer@test.com", "User");

        var reviewId = await CreateReviewAsync(book1, userId, createdAt: DateTime.UtcNow.AddHours(-1));
        await CreateReportAsync(reviewId, userId, reason: "Spam", status: ReportStatus.Pending);
        await CreateReportAsync(reviewId, userId, reason: "Spam", status: ReportStatus.Resolved);
        await CreateReportAsync(reviewId, userId, reason: "Spam", status: ReportStatus.Dismissed);

        // Act
        var response = await GetWithToken(client, $"/api/admin/dashboard?days=7", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var breakdown = root.GetProperty("statusBreakdown");
        Assert.True(breakdown.TryGetProperty("pendingPercent", out var pendingPct));
        Assert.True(breakdown.TryGetProperty("dismissedPercent", out var dismissedPct));
        Assert.True(breakdown.TryGetProperty("resolvedPercent", out var resolvedPct));

        // 3 reports total, 1 each = 33.3% each
        Assert.Equal(33.3, pendingPct.GetDouble());
        Assert.Equal(33.3, dismissedPct.GetDouble());
        Assert.Equal(33.3, resolvedPct.GetDouble());
    }
}
