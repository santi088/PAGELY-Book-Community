using BookCommunity.Api.DTOs.Auth;
using BookCommunity.Api.Services;
using BookCommunity.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace BookCommunity.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly TokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly IEmailVerificationService _verificationService;
    private readonly IConfiguration _config;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        TokenService tokenService,
        IEmailService emailService,
        IEmailVerificationService verificationService,
        IConfiguration config)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _emailService = emailService;
        _verificationService = verificationService;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
        {
            if (existingUser.EmailConfirmed)
                return Conflict(new { message = "User with this email already exists" });

            // Unverified account: regenerate code instead of blocking re-registration
            try { await _verificationService.GenerateAndSendAsync(existingUser, _emailService, _config); } catch { }
            return Ok(new { message = "Registration successful. Please verify your email.", email = existingUser.Email });
        }

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        if (!await _roleManager.RoleExistsAsync("User"))
            await _roleManager.CreateAsync(new IdentityRole("User"));
        await _userManager.AddToRoleAsync(user, "User");

        try
        {
            await _verificationService.GenerateAndSendAsync(user, _emailService, _config);
        }
        catch
        {
        }

        return Ok(new { message = "Registration successful. Please verify your email.", email = user.Email });
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var ok = await _verificationService.VerifyAsync(dto.Email, dto.Code, _userManager);
        if (!ok)
            return BadRequest(new { message = "Invalid or expired verification code" });

        return Ok(new { message = "Email verified successfully" });
    }

    [HttpPost("resend-verification")]
    [EnableRateLimiting("resend")]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user != null && !user.EmailConfirmed)
        {
            try { await _verificationService.GenerateAndSendAsync(user, _emailService, _config); } catch { }
        }

        return Ok(new { message = "If an account exists and is unverified, a verification code has been sent." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
            return Unauthorized(new { message = "Invalid credentials" });

        if (user.IsSuspended)
            return Forbid();

        if (!user.EmailConfirmed)
            return Unauthorized(new { message = "Email verification required" });

        var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
        if (!result.Succeeded)
            return Unauthorized(new { message = "Invalid credentials" });

        var roles = await _userManager.GetRolesAsync(user);
        var token = _tokenService.GenerateToken(user, roles);
        var config = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var expireMinutes = int.Parse(config["Jwt:ExpireMinutes"] ?? "60");

        return Ok(new AuthResponseDto
        {
            Token = token,
            Email = user.Email ?? "",
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expireMinutes)
        });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

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

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        return Ok(new { message = "Logged out successfully. Remove token from client." });
    }
}
