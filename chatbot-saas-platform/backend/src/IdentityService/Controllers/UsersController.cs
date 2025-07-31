using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IdentityService.Services;
using IdentityService.Models;
using Shared.Application.Common.Interfaces;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserManagementService userManagementService,
        ICurrentUserService currentUserService,
        ILogger<UsersController> logger)
    {
        _userManagementService = userManagementService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
    {
        try
        {
            var result = await _userManagementService.GetUsersAsync(page, pageSize, search);
            return Ok(new
            {
                data = result.Data,
                totalCount = result.TotalCount,
                currentPage = result.CurrentPage,
                pageSize = result.PageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        try
        {
            var user = await _userManagementService.GetUserAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.Role,
                user.IsActive,
                user.CreatedAt,
                user.LastLoginAt,
                user.PreferredLanguage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            var user = await _userManagementService.CreateUserAsync(request);
            return Ok(new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                Role = user.Role,
                user.IsActive,
                user.CreatedAt
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            var result = await _userManagementService.UpdateUserAsync(id, request);
            if (!result)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new { message = "User updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        try
        {
            var result = await _userManagementService.DeleteUserAsync(id);
            if (!result)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new { message = "User deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("{userId}/roles")]
    public async Task<IActionResult> AssignRole(Guid userId, [FromBody] AssignRoleRequest request)
    {
        try
        {
            var result = await _userManagementService.AssignRoleAsync(userId, request.Role);
            if (!result)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new { message = "Role assigned successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role to user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

public class AssignRoleRequest
{
    public string Role { get; set; } = string.Empty;
}
