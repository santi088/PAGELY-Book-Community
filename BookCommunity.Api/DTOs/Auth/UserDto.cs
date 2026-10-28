namespace BookCommunity.Api.DTOs.Auth;

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsSuspended { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();
}
