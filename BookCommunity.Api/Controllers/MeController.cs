using BookCommunity.Api.DTOs.Auth;
using BookCommunity.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BookCommunity.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    
    public MeController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }
    
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
        
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();
        
        if (user.IsSuspended)
            return Forbid();
        
        var roles = await _userManager.GetRolesAsync(user);
        
        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? "",
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsSuspended = user.IsSuspended,
            Roles = roles
        });
    }
}
