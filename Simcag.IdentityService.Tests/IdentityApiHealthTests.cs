using System.Net;
using FluentAssertions;

namespace Simcag.IdentityService.Tests;

public sealed class IdentityApiHealthTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public IdentityApiHealthTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Get_Health_Returns_200()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_Health_Live_Returns_200()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health/live");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_Health_Ready_Returns_200()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health/ready");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
