using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Simcag.IdentityService.Application.DTOs;
using Simcag.Shared.Contracts;

namespace Simcag.IdentityService.Tests;

public sealed class AuthValidateIntegrationTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public AuthValidateIntegrationTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Register_login_validate_returns_ok_and_claims()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var tenantId = Guid.NewGuid();
        var email = $"user_{Guid.NewGuid():N}@example.test";
        const string password = "password12";

        var register = new RegisterRequest
        {
            TenantId = tenantId,
            Email = email,
            Password = password,
            Name = "Test User",
            Role = "Sindico"
        };

        using var regResp = await client.PostAsJsonAsync("/api/auth/register", register);
        Assert.Equal(HttpStatusCode.Created, regResp.StatusCode);

        var regWrap = await regResp.Content.ReadFromJsonAsync<ApiResponse<AuthResult>>();
        var regAuth = regWrap?.Data;
        Assert.NotNull(regAuth?.AccessToken);

        using var loginResp = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest { TenantId = tenantId, Email = email, Password = password });
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);

        var loginWrap = await loginResp.Content.ReadFromJsonAsync<ApiResponse<AuthResult>>();
        var loginAuth = loginWrap?.Data;
        Assert.NotNull(loginAuth?.AccessToken);

        using var validateReq = new HttpRequestMessage(HttpMethod.Get, "/api/auth/validate");
        validateReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginAuth.AccessToken);

        using var validateResp = await client.SendAsync(validateReq);
        Assert.Equal(HttpStatusCode.OK, validateResp.StatusCode);

        var body = await validateResp.Content.ReadFromJsonAsync<TokenValidationResponse>();
        Assert.NotNull(body);
        Assert.True(body.IsValid);
        Assert.Equal("Sindico", body.Role);
        Assert.False(string.IsNullOrWhiteSpace(body.UserId));
        Assert.Equal(tenantId.ToString(), body.TenantId);
    }

    [Fact]
    public async Task Login_wrong_tenant_returns_specific_message()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var email = $"user_{Guid.NewGuid():N}@example.test";
        const string password = "password12";

        var register = new RegisterRequest
        {
            TenantId = tenantA,
            Email = email,
            Password = password,
            Name = "Test User",
            Role = "Morador"
        };

        using var regResp = await client.PostAsJsonAsync("/api/auth/register", register);
        Assert.Equal(HttpStatusCode.Created, regResp.StatusCode);

        using var loginResp = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest { TenantId = tenantB, Email = email, Password = password });
        Assert.Equal(HttpStatusCode.Unauthorized, loginResp.StatusCode);

        var loginWrap = await loginResp.Content.ReadFromJsonAsync<ApiResponse<string>>();
        Assert.NotNull(loginWrap?.Error);
        Assert.Contains("vinculado ao condomínio", loginWrap.Error, StringComparison.OrdinalIgnoreCase);
    }
}
