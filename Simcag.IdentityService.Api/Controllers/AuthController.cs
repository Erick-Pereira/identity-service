using Microsoft.AspNetCore.Mvc;
using Simcag.IdentityService.Application.DTOs;
using Simcag.IdentityService.Application.Interfaces;
using Simcag.Shared.Contracts;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Simcag.IdentityService.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToArray();
            return BadRequest(ApiResponse<string>.Fail(string.Join(", ", errors)));
        }

        var result = await _authService.RegisterAsync(request, ct);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<string>.Fail(result.Error!));
        }

        return CreatedAtAction(nameof(Me), new { id = result.User!.Id },
            ApiResponse<AuthResult>.Ok(result));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<string>.Fail("Invalid request data"));
        }

        var result = await _authService.LoginAsync(request, ct);

        if (!result.Success)
        {
            return Unauthorized(ApiResponse<string>.Fail(result.Error!));
        }

        return Ok(ApiResponse<AuthResult>.Ok(result));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<string>.Fail("Invalid request data"));
        }

        var result = await _authService.RefreshTokenAsync(request, ct);

        if (!result.Success)
        {
            return Unauthorized(ApiResponse<string>.Fail(result.Error!));
        }

        return Ok(ApiResponse<AuthResult>.Ok(result));
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        // Get user ID from JWT token claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ??
                         User.FindFirst(JwtRegisteredClaimNames.Sub);

        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(ApiResponse<string>.Fail("Invalid token"));
        }

        var userProfile = await _authService.GetUserProfileAsync(userId, ct);

        if (userProfile == null)
        {
            return NotFound(ApiResponse<string>.Fail("User not found"));
        }

        return Ok(ApiResponse<UserProfileDto>.Ok(userProfile));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        // In a real implementation, you might want to revoke the refresh token
        // For now, we'll just return success since the client should discard the tokens
        return Ok(ApiResponse<string>.Ok("Logged out successfully"));
    }
}