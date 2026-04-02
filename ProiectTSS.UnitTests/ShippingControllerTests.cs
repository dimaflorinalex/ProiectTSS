using Microsoft.AspNetCore.Mvc;
using ProiectTSS.Controllers;
using ProiectTSS.Dtos;
using ProiectTSS.IServices;

namespace ProiectTSS.UnitTests;

/// <summary>
/// Unit tests for <see cref="ShippingController"/>.
/// </summary>
public class ShippingControllerTests
{
    /// <summary>
    /// Returns HTTP 200 with response payload when quote calculation succeeds.
    /// </summary>
    [Test]
    public void Quote_WhenServiceSucceeds_ReturnsOkObjectResult()
    {
        // Arrange
        var expected = new ShippingQuoteResponse
        {
            ShippingCost = 10m,
            Currency = "RON",
            Breakdown = new ShippingBreakdown { SubtotalAfterDiscounts = 10m },
            RuleApplied = "BRACKETS"
        };

        var controller = new ShippingController(new StubShippingCalculatorService(expected));
        var request = CreateValidRequest();

        // Act
        var result = controller.Quote(request);

        // Assert
        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        Assert.That(ok.Value, Is.SameAs(expected));
    }

    /// <summary>
    /// Returns HTTP 400 with error payload when quote calculation throws validation exception.
    /// </summary>
    [Test]
    public void Quote_WhenServiceThrowsArgumentException_ReturnsBadRequestObjectResult()
    {
        // Arrange
        var controller = new ShippingController(new ThrowingShippingCalculatorService("invalid request"));
        var request = CreateValidRequest();

        // Act
        var result = controller.Quote(request);

        // Assert
        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
        var badRequest = (BadRequestObjectResult)result.Result!;
        Assert.That(badRequest.Value, Is.Not.Null);
        Assert.That(badRequest.Value!.ToString(), Does.Contain("invalid request"));
    }

    /// <summary>
    /// Creates a valid baseline request used by controller tests.
    /// </summary>
    /// <returns>Valid quote request.</returns>
    private static ShippingQuoteRequest CreateValidRequest() =>
        new()
        {
            Zone = ShippingZone.Local,
            Subtotal = 100m,
            Options = new ShippingOptions { Rapid = false, Fragil = false },
            Parcels = [new ParcelInput { WeightKg = 1m, Size = ParcelSize.Small }],
            PricingModel = PricingModel.Brackets,
            RoundingRule = RoundingRule.None
        };

    private sealed class StubShippingCalculatorService(ShippingQuoteResponse response) : IShippingCalculatorService
    {
        public ShippingQuoteResponse Calculate(ShippingQuoteRequest request) => response;
    }

    private sealed class ThrowingShippingCalculatorService(string message) : IShippingCalculatorService
    {
        public ShippingQuoteResponse Calculate(ShippingQuoteRequest request) => throw new ArgumentException(message);
    }
}
