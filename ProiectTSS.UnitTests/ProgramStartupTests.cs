using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using ProiectTSS.Dtos;

namespace ProiectTSS.UnitTests;

/// <summary>
/// Integration tests that cover application startup pipeline branches.
/// </summary>
public class ProgramStartupTests
{
    /// <summary>
    /// Exposes OpenAPI endpoint in Development environment.
    /// </summary>
    [Test]
    public async Task Startup_WhenEnvironmentIsDevelopment_MapsOpenApiEndpoint()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory("Development");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        // Act
        var response = await client.GetAsync("/openapi/v1.json");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    /// <summary>
    /// Does not expose OpenAPI endpoint in non-Development environment.
    /// </summary>
    [Test]
    public async Task Startup_WhenEnvironmentIsProduction_DoesNotMapOpenApiEndpoint()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory("Production");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        // Act
        var response = await client.GetAsync("/openapi/v1.json");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// Covers controller route and middleware pipeline through an end-to-end quote request.
    /// </summary>
    [Test]
    public async Task Startup_WhenPostingShippingQuote_ReturnsSuccessfulResponse()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory("Production");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var request = new ShippingQuoteRequest
        {
            Zone = ShippingZone.Local,
            Subtotal = 100m,
            Options = new ShippingOptions { Rapid = false, Fragil = false },
            Parcels = [new ParcelInput { WeightKg = 1m, Size = ParcelSize.Small }],
            PricingModel = PricingModel.Brackets,
            RoundingRule = RoundingRule.None
        };

        // Act
        var response = await client.PostAsJsonAsync("/shipping/quote", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var payload = await response.Content.ReadFromJsonAsync<ShippingQuoteResponse>();
        Assert.That(payload, Is.Not.Null);
        Assert.That(payload!.ShippingCost, Is.GreaterThanOrEqualTo(0m));
    }

    private sealed class CustomWebApplicationFactory(string environment) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(environment);
        }
    }
}
