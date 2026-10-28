using BookCommunity.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BookCommunity.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
    
    public DbSet<Book> Books { get; set; }
    public DbSet<Genre> Genres { get; set; }
    public DbSet<BookGenre> BookGenres { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<ReviewLike> ReviewLikes { get; set; }
    public DbSet<ReadingListEntry> ReadingListEntries { get; set; }
    public DbSet<Report> Reports { get; set; }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // BookGenre many-to-many
        builder.Entity<BookGenre>()
            .HasKey(bg => new { bg.BookId, bg.GenreId });
        
        builder.Entity<BookGenre>()
            .HasOne(bg => bg.Book)
            .WithMany(b => b.BookGenres)
            .HasForeignKey(bg => bg.BookId);
        
        builder.Entity<BookGenre>()
            .HasOne(bg => bg.Genre)
            .WithMany(g => g.BookGenres)
            .HasForeignKey(bg => bg.GenreId);
        
        // Review - unique per user per book
        builder.Entity<Review>()
            .HasIndex(r => new { r.UserId, r.BookId })
            .IsUnique();
        
        builder.Entity<Review>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Entity<Review>()
            .HasOne(r => r.Book)
            .WithMany(b => b.Reviews)
            .HasForeignKey(r => r.BookId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // ReviewLike - unique per user per review
        builder.Entity<ReviewLike>()
            .HasIndex(rl => new { rl.UserId, rl.ReviewId })
            .IsUnique();
        
        builder.Entity<ReviewLike>()
            .HasOne(rl => rl.User)
            .WithMany()
            .HasForeignKey(rl => rl.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Entity<ReviewLike>()
            .HasOne(rl => rl.Review)
            .WithMany(r => r.ReviewLikes)
            .HasForeignKey(rl => rl.ReviewId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // ReadingListEntry - unique per user per book
        builder.Entity<ReadingListEntry>()
            .HasIndex(rle => new { rle.UserId, rle.BookId })
            .IsUnique();
        
        builder.Entity<ReadingListEntry>()
            .HasOne(rle => rle.User)
            .WithMany()
            .HasForeignKey(rle => rle.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Entity<ReadingListEntry>()
            .HasOne(rle => rle.Book)
            .WithMany(b => b.ReadingListEntries)
            .HasForeignKey(rle => rle.BookId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Report
        builder.Entity<Report>()
            .HasOne(r => r.Reporter)
            .WithMany()
            .HasForeignKey(r => r.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.Entity<Report>()
            .HasOne(r => r.Review)
            .WithMany(rev => rev.Reports)
            .HasForeignKey(r => r.ReviewId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Entity<Report>()
            .HasOne(r => r.ModeratedBy)
            .WithMany()
            .HasForeignKey(r => r.ModeratedById)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Indexes for common queries
        builder.Entity<Book>()
            .HasIndex(b => b.Title);
        builder.Entity<Book>()
            .HasIndex(b => b.Author);
        builder.Entity<Book>()
            .HasIndex(b => b.IsActive);
        builder.Entity<Genre>()
            .HasIndex(g => g.Name)
            .IsUnique();
    }
}
