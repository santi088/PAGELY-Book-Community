using BookCommunity.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookCommunity.Infrastructure.Data;

public class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        await context.Database.EnsureCreatedAsync();
        
        // Create roles
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }
        if (!await roleManager.RoleExistsAsync("User"))
        {
            await roleManager.CreateAsync(new IdentityRole("User"));
        }
        
        // Create admin user if not exists (from config/env)
        var adminEmail = Environment.GetEnvironmentVariable("Admin__Email") ?? "admin@bookcommunity.local";
        var adminPassword = Environment.GetEnvironmentVariable("Admin__Password");
        
        if (!string.IsNullOrEmpty(adminPassword))
        {
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FirstName = "Admin",
                    LastName = "User"
                };
                await userManager.CreateAsync(adminUser, adminPassword);
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
        
        // Seed genres
        if (!await context.Genres.AnyAsync())
        {
            var genres = new List<Genre>
            {
                new Genre { Name = "Fiction", Description = "Fiction books" },
                new Genre { Name = "Fantasy", Description = "Fantasy books" },
                new Genre { Name = "Mystery", Description = "Mystery and thriller" },
                new Genre { Name = "Romance", Description = "Romance novels" },
                new Genre { Name = "Science Fiction", Description = "Sci-fi books" },
                new Genre { Name = "Non-Fiction", Description = "Non-fiction books" }
            };
            await context.Genres.AddRangeAsync(genres);
            await context.SaveChangesAsync();
        }
        
        // Seed books
        if (!await context.Books.AnyAsync())
        {
            var fictionGenre = await context.Genres.FirstAsync(g => g.Name == "Fiction");
            var fantasyGenre = await context.Genres.FirstAsync(g => g.Name == "Fantasy");
            
            var books = new List<Book>
            {
                new Book
                {
                    Title = "Sample Book 1",
                    Author = "Author One",
                    Description = "A sample book for testing",
                    PublicationYear = 2023,
                    PublicationDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    IsActive = true
                },
                new Book
                {
                    Title = "Sample Book 2",
                    Author = "Author Two",
                    Description = "Another sample book",
                    PublicationYear = 2024,
                    PublicationDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    IsActive = true
                }
            };
            await context.Books.AddRangeAsync(books);
            await context.SaveChangesAsync();
            
            // Add genres to books
            await context.BookGenres.AddAsync(new BookGenre { BookId = books[0].Id, GenreId = fictionGenre.Id });
            await context.BookGenres.AddAsync(new BookGenre { BookId = books[1].Id, GenreId = fantasyGenre.Id });
            await context.SaveChangesAsync();
        }
    }
}
