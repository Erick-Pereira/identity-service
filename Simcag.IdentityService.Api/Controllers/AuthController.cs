using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Simcag.IdentityService.Application.DTOs;
using Simcag.IdentityService.Application.Interfaces;
using Simcag.Shared.Contracts;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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

    [HttpGet("validate")]
    [AllowAnonymous]
    public async Task<IActionResult> Validate(CancellationToken ct)
    {
        var header = Request.Headers.Authorization.ToString();
        var body = await _authService.ValidateAccessTokenAsync(header, ct);
        if (!body.IsValid)
            return Unauthorized(body);
        return Ok(body);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ??
                         User.FindFirst(JwtRegisteredClaimNames.Sub);

        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(ApiResponse<string>.Fail("Invalid token"));
        }

        var tenantClaim = User.FindFirst("tenant_id");
        if (tenantClaim is null || !Guid.TryParse(tenantClaim.Value, out var tenantId))
        {
            return Unauthorized(ApiResponse<string>.Fail("Invalid token (tenant)"));
        }

        var userProfile = await _authService.GetUserProfileAsync(userId, tenantId, ct);

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