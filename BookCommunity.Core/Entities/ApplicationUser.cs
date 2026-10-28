using Microsoft.AspNetCore.Identity;

namespace BookCommunity.Core.Entities;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsSuspended { get; set; } = false;
    public DateTime? SuspendedAt { get; set; }
}
