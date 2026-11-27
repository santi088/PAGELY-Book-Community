using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
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

public class AdminListFilterTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private const string TestJwtKey = "test-key-that-is-at-least-32-characters-long-for-testing";
    private const string TestIssuer = "TestIssuer";
    private const string TestAudience = "TestAudience";

    public AdminListFilterTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("AdminListFilterTestDb"));
            });
        });
    }

    public Task InitializeAsync() => CleanupDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

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

    private async Task CleanupDatabaseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        ctx.Reviews.RemoveRange(ctx.Reviews);
        ctx.BookGenres.RemoveRange(ctx.BookGenres);
        ctx.Books.RemoveRange(ctx.Books);
        ctx.Genres.RemoveRange(ctx.Genres);
        ctx.Reports.RemoveRange(ctx.Reports);
        ctx.ReadingListEntries.RemoveRange(ctx.ReadingListEntries);
        await ctx.SaveChangesAsync();

        var um = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var users = um.Users.ToList();
        foreach (var u in users)
            await um.DeleteAsync(u);
    }

    private async Task<string> CreateRegularUserAsync(string email = null)
    {
        email ??= $"user-{Guid.NewGuid():N}@example.com";
        using var scope = _factory.Services.CreateScope();
        var um = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "Regular",
            LastName = "User",
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };
        await um.CreateAsync(user, "TestPass123!");
        await um.AddToRoleAsync(user, "User");
        return email;
    }

    private static async Task<HttpResponseMessage> GetWithToken(HttpClient client, string path, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
    }

    private static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<T>();
    }

    private static int GetBodyInt(Dictionary<string, object> body, string key)
    {
        var val = body[key];
        return val switch
        {
            System.Text.Json.JsonElement je => je.GetInt32(),
            int i => i,
            _ => Convert.ToInt32(val)
        };
    }

    [Fact]
    public async Task Users_Endpoint_ReturnsPagedResult_WithSearchFilter()
    {
        var client = _factory.CreateClient();
        var token = GenerateToken("Admin");

        await CreateRegularUserAsync("alice.smith@example.com");
        await CreateRegularUserAsync("bob.jones@example.com");
        await CreateRegularUserAsync("charlie@example.com");

        // Search by name
        var response = await GetWithToken(client, "/api/admin/users?search=alice&pageSize=10", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync<Dictionary<string, object>>(response);
        var items = ((System.Text.Json.JsonElement)body!["items"]).EnumerateArray().ToList();
        Assert.Single(items);
        Assert.Equal("alice.smith@example.com", items[0].GetProperty("email").GetString());

        // Verify PagedResult shape
        Assert.Equal(1, GetBodyInt(body!, "page"));
        Assert.Equal(10, GetBodyInt(body!, "pageSize"));
        Assert.Equal(1, GetBodyInt(body!, "totalCount"));
        Assert.Equal(1, GetBodyInt(body!, "totalPages"));
    }

    [Fact]
    public async Task Users_Endpoint_FiltersByRole()
    {
        var client = _factory.CreateClient();
        var token = GenerateToken("Admin");

        await CreateRegularUserAsync("role-admin@example.com");
        await CreateRegularUserAsync("role-user1@example.com");
        await CreateRegularUserAsync("role-user2@example.com");

        // Make first user an admin
        using (var scope = _factory.Services.CreateScope())
        {
            var um = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var rm = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            if (!await rm.RoleExistsAsync("Admin"))
                await rm.CreateAsync(new IdentityRole("Admin"));
            var user = await um.FindByEmailAsync("role-admin@example.com");
            await um.AddToRoleAsync(user!, "Admin");
        }

        // Filter by User role (admin user also has User role)
        var response = await GetWithToken(client, "/api/admin/users?role=User&pageSize=10", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync<Dictionary<string, object>>(response);
        var items = ((System.Text.Json.JsonElement)body!["items"]).EnumerateArray().ToList();
        Assert.Equal(3, items.Count);
        Assert.Equal(3, GetBodyInt(body!, "totalCount"));

        // Filter by Admin role
        response = await GetWithToken(client, "/api/admin/users?role=Admin&pageSize=10", token);
        body = await ReadJsonAsync<Dictionary<string, object>>(response);
        items = ((System.Text.Json.JsonElement)body!["items"]).EnumerateArray().ToList();
        Assert.Single(items);
        Assert.Equal(1, GetBodyInt(body!, "totalCount"));
    }

    [Fact]
    public async Task Users_Endpoint_FiltersBySuspended()
    {
        var client = _factory.CreateClient();
        var token = GenerateToken("Admin");

        await CreateRegularUserAsync("suspended-user@example.com");
        await CreateRegularUserAsync("active-user@example.com");

        // Suspend one user
        using (var scope = _factory.Services.CreateScope())
        {
            var um = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await um.FindByEmailAsync("suspended-user@example.com");
            user!.IsSuspended = true;
            await um.UpdateAsync(user);
        }

        // Filter suspended = true
        var response = await GetWithToken(client, "/api/admin/users?suspended=true&pageSize=10", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync<Dictionary<string, object>>(response);
        var items = ((System.Text.Json.JsonElement)body!["items"]).EnumerateArray().ToList();
        Assert.Single(items);
        Assert.Equal("suspended-user@example.com", items[0].GetProperty("email").GetString());
        Assert.True(items[0].GetProperty("isSuspended").GetBoolean());

        // Filter suspended = false
        response = await GetWithToken(client, "/api/admin/users?suspended=false&pageSize=10", token);
        body = await ReadJsonAsync<Dictionary<string, object>>(response);
        items = ((System.Text.Json.JsonElement)body!["items"]).EnumerateArray().ToList();
        Assert.Single(items);
        Assert.Equal("active-user@example.com", items[0].GetProperty("email").GetString());
    }

    [Fact]
    public async Task Users_Roles_Endpoint_ReturnsDistinctRoles()
    {
        var client = _factory.CreateClient();
        var token = GenerateToken("Admin");

        await CreateRegularUserAsync("roles-admin@example.com");
        await CreateRegularUserAsync("roles-user@example.com");

        using (var scope = _factory.Services.CreateScope())
        {
            var um = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var rm = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            if (!await rm.RoleExistsAsync("Admin"))
                await rm.CreateAsync(new IdentityRole("Admin"));
            var user = await um.FindByEmailAsync("roles-admin@example.com");
            await um.AddToRoleAsync(user!, "Admin");
        }

        var response = await GetWithToken(client, "/api/admin/roles", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var roles = await ReadJsonAsync<List<string>>(response);
        Assert.Contains("Admin", roles);
        Assert.Contains("User", roles);
    }

    [Fact]
    public async Task Books_Endpoint_FiltersByIsActive()
    {
        var client = _factory.CreateClient();
        var token = GenerateToken("Admin");

        using (var scope = _factory.Services.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            ctx.Books.Add(new Book
            {
                Title = "Active Book",
                Author = "Author A",
                IsActive = true,
                AverageRating = 0,
                ReviewCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            ctx.Books.Add(new Book
            {
                Title = "Inactive Book",
                Author = "Author B",
                IsActive = false,
                AverageRating = 0,
                ReviewCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            ctx.SaveChanges();
        }

        // Filter isActive = true
        var response = await GetWithToken(client, "/api/books?isActive=true&pageSize=10", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync<Dictionary<string, object>>(response);
        var items = ((System.Text.Json.JsonElement)body!["items"]).EnumerateArray().ToList();
        Assert.Single(items);
        Assert.Equal("Active Book", items[0].GetProperty("title").GetString());

        // Filter isActive = false
        response = await GetWithToken(client, "/api/books?isActive=false&pageSize=10", token);
        body = await ReadJsonAsync<Dictionary<string, object>>(response);
        items = ((System.Text.Json.JsonElement)body!["items"]).EnumerateArray().ToList();
        Assert.Single(items);
        Assert.Equal("Inactive Book", items[0].GetProperty("title").GetString());
    }

    [Fact]
    public async Task Books_Endpoint_SortsByReviewCount()
    {
        var client = _factory.CreateClient();
        var token = GenerateToken("Admin");

        using (var scope = _factory.Services.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            ctx.Books.Add(new Book
            {
                Title = "Low Review Book",
                Author = "Author A",
                IsActive = true,
                AverageRating = 4.0,
                ReviewCount = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            ctx.Books.Add(new Book
            {
                Title = "High Review Book",
                Author = "Author B",
                IsActive = true,
                AverageRating = 3.0,
                ReviewCount = 10,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            ctx.SaveChanges();
        }

        // Sort by reviewcount desc (most reviewed first)
        var response = await GetWithToken(client, "/api/books?sortBy=reviewcount&sortOrder=desc&pageSize=10", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync<Dictionary<string, object>>(response);
        var items = ((System.Text.Json.JsonElement)body!["items"]).EnumerateArray().ToList();
        Assert.Equal(2, items.Count);
        Assert.Equal("High Review Book", items[0].GetProperty("title").GetString());
        Assert.Equal(10, items[0].GetProperty("reviewCount").GetInt32());
        Assert.Equal("Low Review Book", items[1].GetProperty("title").GetString());
        Assert.Equal(1, items[1].GetProperty("reviewCount").GetInt32());
    }

    [Fact]
    public async Task Genres_Endpoint_ReturnsPagedResult_WithSearchAndSort()
    {
        var client = _factory.CreateClient();
        var token = GenerateToken("Admin");

        using (var scope = _factory.Services.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            ctx.Genres.Add(new Genre { Name = "Fiction", Description = "Novels and stories", CreatedAt = DateTime.UtcNow });
            ctx.Genres.Add(new Genre { Name = "Non-Fiction", Description = "Factual books", CreatedAt = DateTime.UtcNow });
            ctx.Genres.Add(new Genre { Name = "Mystery", Description = "Whodunits", CreatedAt = DateTime.UtcNow });
            ctx.SaveChanges();
        }

        // Search
        var response = await GetWithToken(client, "/api/genres?search=Fiction&page=1&pageSize=10", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync<Dictionary<string, object>>(response);
        var items = ((System.Text.Json.JsonElement)body!["items"]).EnumerateArray().ToList();
        Assert.Equal(2, items.Count);
        Assert.Equal(2, GetBodyInt(body!, "totalCount"));
        Assert.Equal(1, GetBodyInt(body!, "page"));
        Assert.Equal(10, GetBodyInt(body!, "pageSize"));
        Assert.Equal(1, GetBodyInt(body!, "totalPages"));

        // Sort name desc
        response = await GetWithToken(client, "/api/genres?sort=namedesc&page=1&pageSize=10", token);
        body = await ReadJsonAsync<Dictionary<string, object>>(response);
        items = ((System.Text.Json.JsonElement)body!["items"]).EnumerateArray().ToList();
        Assert.Equal(3, items.Count);
        Assert.Equal("Non-Fiction", items[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task Reviews_Admin_Endpoint_FiltersByStatusAndRating()
    {
        var client = _factory.CreateClient();
        var token = GenerateToken("Admin");

        Guid bookId;
        string reviewerId;
        using (var scope = _factory.Services.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var um = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var book = new Book { Title = "Review Test Book", Author = "Author", IsActive = true, AverageRating = 0, ReviewCount = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            ctx.Books.Add(book);
            await ctx.SaveChangesAsync();
            bookId = book.Id;

            var reviewerEmail = await CreateRegularUserAsync("reviewer-filter@example.com");
            var reviewer = await um.FindByEmailAsync(reviewerEmail);
            reviewerId = reviewer!.Id;

            ctx.Reviews.Add(new Review
            {
                UserId = reviewerId,
                BookId = book.Id,
                Rating = 5,
                ReviewText = "Great book!",
                IsHidden = false,
                IsDeleted = false,
                LikeCount = 3,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = DateTime.UtcNow.AddDays(-2)
            });
            ctx.Reviews.Add(new Review
            {
                UserId = reviewerId,
                BookId = book.Id,
                Rating = 2,
                ReviewText = "Not great",
                IsHidden = true,
                IsDeleted = false,
                LikeCount = 0,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            });
            ctx.Reviews.Add(new Review
            {
                UserId = reviewerId,
                BookId = book.Id,
                Rating = 1,
                ReviewText = "Terrible",
                IsHidden = false,
                IsDeleted = true,
                LikeCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();
        }

        // Filter by hidden status
        var response = await GetWithToken(client, "/api/admin/reviews?status=hidden&pageSize=10", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync<Dictionary<string, object>>(response);
        var items = ((System.Text.Json.JsonElement)body!["items"]).EnumerateArray().ToList();
        Assert.Single(items);
        Assert.Equal(2, items[0].GetProperty("rating").GetInt32());

        // Filter by rating = 5
        response = await GetWithToken(client, "/api/admin/reviews?rating=5&pageSize=10", token);
        body = await ReadJsonAsync<Dictionary<string, object>>(response);
        items = ((System.Text.Json.JsonElement)body!["items"]).EnumerateArray().ToList();
        Assert.Single(items);
        Assert.Equal(5, items[0].GetProperty("rating").GetInt32());
    }

    [Fact]
    public async Task Reviews_Admin_Endpoint_SortsByRating()
    {
        var client = _factory.CreateClient();
        var token = GenerateToken("Admin");

        Guid bookId;
        string reviewerId;
        using (var scope = _factory.Services.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var um = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var book = new Book { Title = "Sort Test Book", Author = "Author", IsActive = true, AverageRating = 0, ReviewCount = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            ctx.Books.Add(book);
            await ctx.SaveChangesAsync();
            bookId = book.Id;

            var reviewerEmail = await CreateRegularUserAsync("reviewer-sort@example.com");
            var reviewer = await um.FindByEmailAsync(reviewerEmail);
            reviewerId = reviewer!.Id;

            ctx.Reviews.Add(new Review { UserId = reviewerId, BookId = book.Id, Rating = 1, ReviewText = "Bad", IsHidden = false, IsDeleted = false, LikeCount = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            ctx.Reviews.Add(new Review { UserId = reviewerId, BookId = book.Id, Rating = 5, ReviewText = "Great", IsHidden = false, IsDeleted = false, LikeCount = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            ctx.Reviews.Add(new Review { UserId = reviewerId, BookId = book.Id, Rating = 3, ReviewText = "OK", IsHidden = false, IsDeleted = false, LikeCount = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            await ctx.SaveChangesAsync();
        }

        // Sort by highest rating first
        var response = await GetWithToken(client, "/api/admin/reviews?sort=highest&pageSize=10", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync<Dictionary<string, object>>(response);
        var items = ((System.Text.Json.JsonElement)body!["items"]).EnumerateArray().ToList();
        Assert.Equal(3, items.Count);
        Assert.Equal(5, items[0].GetProperty("rating").GetInt32());
        Assert.Equal(3, items[1].GetProperty("rating").GetInt32());
        Assert.Equal(1, items[2].GetProperty("rating").GetInt32());
    }
}
