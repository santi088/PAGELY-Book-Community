using System.Net;
using System.Net.Http.Json;
using BookCommunity.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BookCommunity.Tests;

public class SuspendedUserAccessTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SuspendedUserAccessTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UpdateReview_ReturnsForbidden_WhenUserSuspended()
    {
        var client = _factory.CreateClient();
        var email = $"suspendedreview_{Guid.NewGuid():N}@example.com";
        const string password = "TestPass123!";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new { email, password });
        registerResponse.EnsureSuccessStatusCode();
        var auth = await registerResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var token = auth!["token"].ToString()!;

        var booksResponse = await client.GetAsync("/api/books?page=1&pageSize=1");
        var books = await booksResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var items = (System.Text.Json.JsonElement)books!["items"];
        var bookId = items[0].GetProperty("id").GetGuid();

        var reviewResponse = await PostWithToken(client, $"/api/books/{bookId}/reviews", token, new { rating = 5, reviewText = "suspended access test" });
        reviewResponse.EnsureSuccessStatusCode();
        var review = await reviewResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var reviewId = Guid.Parse(review!["id"].ToString()!);

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email);
            Assert.NotNull(user);
            user!.IsSuspended = true;
            await userManager.UpdateAsync(user);
        }

        var updateResponse = await PutWithToken(client, $"/api/reviews/{reviewId}", token, new { rating = 1, reviewText = "should fail" });
        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email);
            user!.IsSuspended = false;
            await userManager.UpdateAsync(user);
            await userManager.DeleteAsync(user);
        }
    }

    private static async Task<HttpResponseMessage> PostWithToken(HttpClient client, string path, string token, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> PutWithToken(HttpClient client, string path, string token, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, path)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
    }
}
