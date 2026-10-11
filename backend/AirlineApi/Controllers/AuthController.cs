using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared;

[ApiController]
[Route("api/auth")]
public class AuthController(
    AirlineDbContext db,
    JwtTokenService tokens) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(AuthDto x)
    {
        if (string.IsNullOrWhiteSpace(x.Email) || x.Password.Length < 8)
            return BadRequest(new
            {
                message = "Valid email and an 8 character password are required"
            });

        var email = x.Email.Trim().ToLowerInvariant();

        if (await db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new { message = "Email already registered" });

        var u = new User
        {
            Email = email,
            DisplayName = string.IsNullOrWhiteSpace(x.DisplayName)
                ? email.Split('@')[0]
                : x.DisplayName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(x.Password)
        };

        db.Users.Add(u);
        await db.SaveChangesAsync();

        return Created("/api/users/me", new
        {
            token = tokens.Create(u),
            user = ApiView.User(u)
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(AuthDto x)
    {
        var email = x.Email.Trim().ToLowerInvariant();
        var u = await db.Users.SingleOrDefaultAsync(u => u.Email == email);

        return u is null || !BCrypt.Net.BCrypt.Verify(x.Password, u.PasswordHash)
            ? Unauthorized(new { message = "Invalid email or password" })
            : Ok(new
            {
                token = tokens.Create(u),
                user = ApiView.User(u)
            });
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout() =>
        Ok(new { message = "Logged out" });

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me() =>
        await db.Users.FindAsync(ApiView.UserId(User)) is { } u
            ? Ok(ApiView.User(u))
            : NotFound();
}