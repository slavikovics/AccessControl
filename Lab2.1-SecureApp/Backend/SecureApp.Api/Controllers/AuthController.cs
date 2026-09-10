using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureApp.Api.Data;
using SecureApp.Api.Dtos;
using SecureApp.Api.Models;
using SecureApp.Api.Services;

namespace SecureApp.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, TokenService tokens, LoginAttemptTracker attempts) : ControllerBase
{
    private const string CookieName = "access_token";

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var normalized = request.Username.Trim();
        var exists = await db.Users.AnyAsync(u => u.Username == normalized);
        if (exists)
        {
            return Conflict(new { message = "Username is not available." });
        }

        var (hash, salt) = PasswordHasher.Hash(request.Password);
        var user = new User { Username = normalized, PasswordHash = hash, PasswordSalt = salt };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return Created(string.Empty, new MeResponse(user.Id, user.Username));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var normalized = request.Username.Trim();

        if (attempts.IsLockedOut(normalized))
        {
            return StatusCode(StatusCodes.Status429TooManyRequests,
                new { message = "Too many failed attempts. Try again later." });
        }

        var user = await db.Users.SingleOrDefaultAsync(u => u.Username == normalized);
        var passwordOk = user is not null && PasswordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt);

        if (!passwordOk)
        {
            attempts.RecordFailure(normalized);
            return Unauthorized(new { message = "Invalid username or password." });
        }

        attempts.RecordSuccess(normalized);

        var token = tokens.CreateToken(user!, out var expiresUtc);
        Response.Cookies.Append(CookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = expiresUtc,
        });

        return Ok(new MeResponse(user!.Id, user.Username));
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(CookieName);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        var username = User.Identity!.Name ?? User.FindFirstValue(ClaimTypes.Name)!;
        return Ok(new MeResponse(id, username));
    }
}
