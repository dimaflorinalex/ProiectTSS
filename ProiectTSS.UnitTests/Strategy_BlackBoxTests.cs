using ProiectTSS.Dtos;
using ProiectTSS.Services;

namespace ProiectTSS.UnitTests;

/// <summary>
/// Black-box tests that illustrate equivalence partitioning and boundary value analysis.
/// </summary>
public class StrategyBlackBoxTests
{
    private readonly ShippingCalculatorService _service = new();

    /// <summary>
    /// Equivalence classes for valid zones using bracket model.
    /// </summary>
    [TestCase(ShippingZone.Local)]
    [TestCase(ShippingZone.National)]
    [TestCase(ShippingZone.International)]
    [Category("BlackBox.Equivalence")]
    public void Calculate_WhenZoneIsValidEquivalenceClass_ReturnsNonNegativeCost(ShippingZone zone)
    {
        // Arrange
        var request = CreateValidRequest();
        request.Zone = zone;
        request.PricingModel = PricingModel.Brackets;

        // Act
        var result = _service.Calculate(request);

        // Assert
        Assert.That(result.ShippingCost, Is.GreaterThanOrEqualTo(0m));
    }

    /// <summary>
    /// Boundary value analysis for bracket weight thresholds.
    /// </summary>
    [TestCase(1.0, 8)]
    [TestCase(1.01, 15)]
    [TestCase(5.0, 15)]
    [TestCase(5.01, 25)]
    [TestCase(10.0, 25)]
    [TestCase(10.01, 25.03)]
    [Category("BlackBox.Boundary")]
    public void Calculate_WhenWeightAtBoundary_ReturnsExpectedBracketTransition(decimal weightKg, decimal expectedCost)
    {
        // Arrange
        var request = CreateValidRequest();
        request.Zone = ShippingZone.Local;
        request.PricingModel = PricingModel.Brackets;
        request.Parcels = [new ParcelInput { WeightKg = weightKg, Size = ParcelSize.Small }];

        // Act
        var result = _service.Calculate(request);

        // Assert
        Assert.That(result.ShippingCost, Is.EqualTo(expectedCost));
    }

    /// <summary>
    /// Boundary test for free-shipping threshold condition (strict greater-than).
    /// </summary>
    [Test]
    [Category("BlackBox.Boundary")]
    public void Calculate_WhenSubtotalEqualsFreeShippingThreshold_DoesNotApplyFreeShipping()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Subtotal = 200m;
        request.FreeShippingThreshold = 200m;

        // Act
        var result = _service.Calculate(request);

        // Assert
        Assert.That(result.ShippingCost, Is.GreaterThan(0m));
        Assert.That(result.Breakdown.FreeShippingDiscount, Is.EqualTo(0m));
    }

    /// <summary>
    /// Boundary test for max cap condition (strict greater-than).
    /// </summary>
    [Test]
    [Category("BlackBox.Boundary")]
    public void Calculate_WhenShippingEqualsCap_DoesNotApplyCapReduction()
    {
        // Arrange
        var request = CreateValidRequest();
        request.MaxCap = 15m;

        // Act
        var result = _service.Calculate(request);

        // Assert
        Assert.That(result.ShippingCost, Is.EqualTo(15m));
        Assert.That(result.Breakdown.CapReduction, Is.EqualTo(0m));
    }

    private static ShippingQuoteRequest CreateValidRequest() =>
        new()
        {
            Zone = ShippingZone.Local,
            Subtotal = 100m,
            Options = new ShippingOptions { Rapid = false, Fragil = false },
            Parcels = [new ParcelInput { WeightKg = 2m, Size = ParcelSize.Small }],
            PricingModel = PricingModel.Brackets,
            RoundingRule = RoundingRule.None
        };
}
