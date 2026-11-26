using BookCommunity.Core.Entities;
using BookCommunity.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace BookCommunity.Api.Services;

public interface IEmailVerificationService
{
    Task<string> GenerateAndSendAsync(ApplicationUser user, IEmailService emailService, IConfiguration config);
    Task<bool> VerifyAsync(string email, string code, UserManager<ApplicationUser> userManager);
    Task ResendAsync(string email, IEmailService emailService, IConfiguration config);
}

public class EmailVerificationService : IEmailVerificationService
{
    private readonly ApplicationDbContext _db;
    public EmailVerificationService(ApplicationDbContext db) { _db = db; }

    public async Task<string> GenerateAndSendAsync(ApplicationUser user, IEmailService emailService, IConfiguration config)
    {
        // invalidate previous
        var prev = await _db.EmailVerificationCodes.Where(c => c.UserId == user.Id && !c.IsUsed).ToListAsync();
        foreach (var p in prev) p.IsUsed = true;
        await _db.SaveChangesAsync();

        var code = GenerateCode();
        var hash = Hash(code, user.Id);
        var entity = new EmailVerificationCode
        {
            UserId = user.Id,
            CodeHash = hash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };
        _db.EmailVerificationCodes.Add(entity);
        await _db.SaveChangesAsync();

        var html = $@"<!DOCTYPE html><html><body>
<h1>Welcome to Pagely</h1>
<p>Your verification code is:</p>
<h2>{code}</h2>
<p>This code expires in 10 minutes. Do not share it with anyone.</p>
</body></html>";
        await emailService.SendEmailAsync(user.Email!, "Your Pagely verification code", html);
        return code;
    }

    public async Task<bool> VerifyAsync(string email, string code, UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null || user.EmailConfirmed) return false;

        var record = await _db.EmailVerificationCodes
            .Where(c => c.UserId == user.Id && !c.IsUsed)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();

        if (record == null || record.ExpiresAt < DateTime.UtcNow) return false;
        if (record.FailedAttempts >= 5) return false;

        var hash = Hash(code, user.Id);
        // constant-time compare
        if (!CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(hash), Convert.FromHexString(record.CodeHash)))
        {
            record.FailedAttempts++;
            await _db.SaveChangesAsync();
            return false;
        }

        record.IsUsed = true;
        user.EmailConfirmed = true;
        await userManager.UpdateAsync(user);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task ResendAsync(string email, IEmailService emailService, IConfiguration config)
    {
        // handled in controller via GenerateAndSendAsync if unverified
    }

    private static string GenerateCode()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var num = BitConverter.ToUInt32(bytes) % 1000000;
        return num.ToString("D6");
    }

    private static string Hash(string code, string salt)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(code + salt));
        return Convert.ToHexString(bytes);
    }
}
