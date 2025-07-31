using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Entities;
using Shared.Domain.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IApplicationDbContext context,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest(new { message = "User with this email already exists" });
            }

            var user = new User
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = UserRole.User,
                PreferredLanguage = request.PreferredLanguage ?? "en"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);

            return Ok(new AuthResponse
            {
                Token = token,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role.ToString(),
                    PreferredLanguage = user.PreferredLanguage
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.UserTenants)
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user != null && (user.UserTenants == null || !user.UserTenants.Any()))
            {
                // Ensure UserTenants is loaded if not already
                await _context.Entry(user)
                    .Collection(u => u.UserTenants)
                    .Query()
                    .Where(x => x.UserId == user.Id)
                    .LoadAsync();
            }
           
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            if (!user.IsActive)
            {
                return Unauthorized(new { message = "Account is deactivated" });
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);

            return Ok(new AuthResponse
            {
                Token = token,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role.ToString(),
                    PreferredLanguage = user.PreferredLanguage
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user login");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("refresh")]
    [Authorize]
    public async Task<IActionResult> Refresh()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userGuid);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var token = GenerateJwtToken(user);

            return Ok(new AuthResponse
            {
                Token = token,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role.ToString(),
                    PreferredLanguage = user.PreferredLanguage
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        // For JWT, logout is handled client-side by removing the token.
        // Optionally, you can implement server-side token invalidation/blacklisting.
        return Ok(new { message = "Logged out successfully" });
    }

    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("preferred_language", user.PreferredLanguage)
        };

        // Add tenant roles if user is a tenant user
        if (user.UserTenants != null && user.UserTenants.Count > 0)
        {
            foreach (var userTenant in user.UserTenants)
            {
                claims.Add(new Claim("tenant_id", userTenant.TenantId.ToString()));
                claims.Add(new Claim("tenant_role", userTenant.Role.ToString()));
            }
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userGuid);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role.ToString(),
                PreferredLanguage = user.PreferredLanguage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? PreferredLanguage { get; set; }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string PreferredLanguage { get; set; } = string.Empty;
}
