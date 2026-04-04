using ProiectTSS.Dtos;
using ProiectTSS.Services;

namespace ProiectTSS.UnitTests;

/// <summary>
/// White-box tests focused on statement/decision/condition coverage and independent paths.
/// </summary>
public class StrategyWhiteBoxPathTests
{
    private readonly ShippingCalculatorService _service = new();

    /// <summary>
    /// Independent path: fallback branch with zone null.
    /// </summary>
    [Test]
    [Category("WhiteBox.IndependentPath")]
    public void Calculate_WhenZoneIsNull_UsesFallbackRulePath()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Zone = null;
        request.FallbackZonePrice = 19m;

        // Act
        var result = _service.Calculate(request);

        // Assert
        Assert.That(result.RuleApplied, Does.Contain("FALLBACK_ZONE_PRICE"));
        Assert.That(result.ShippingCost, Is.EqualTo(19m));
    }

    /// <summary>
    /// Independent path: base plus per kg branch.
    /// </summary>
    [Test]
    [Category("WhiteBox.IndependentPath")]
    public void Calculate_WhenPricingModelIsBasePlusPerKg_UsesBasePlusPerKgRulePath()
    {
        // Arrange
        var request = CreateValidRequest();
        request.PricingModel = PricingModel.BasePlusPerKg;

        // Act
        var result = _service.Calculate(request);

        // Assert
        Assert.That(result.RuleApplied, Does.Contain("BASE_PLUS_PER_KG"));
    }

    /// <summary>
    /// Independent path: fragile surcharge + rapid surcharge + coupon branch.
    /// </summary>
    [Test]
    [Category("WhiteBox.IndependentPath")]
    public void Calculate_WhenFragileRapidAndCouponAreEnabled_ExecutesCompositeRulePath()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Options = new ShippingOptions { Fragil = true, Rapid = true };
        request.Coupon = new CouponInput { Type = CouponType.Percent, Value = 10m };

        // Act
        var result = _service.Calculate(request);

        // Assert
        Assert.That(result.RuleApplied, Does.Contain("FRAGILE_SURCHARGE"));
        Assert.That(result.RuleApplied, Does.Contain("RAPID_SURCHARGE"));
        Assert.That(result.RuleApplied, Does.Contain("COUPON_DISCOUNT"));
    }

    /// <summary>
    /// Condition coverage example for request validation branch (coupon type null).
    /// </summary>
    [Test]
    [Category("WhiteBox.Condition")]
    public void Calculate_WhenCouponExistsButTypeMissing_ThrowsValidationException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Coupon = new CouponInput { Type = null, Value = 5m };

        // Act
        var action = () => _service.Calculate(request);

        // Assert
        Assert.That(action, Throws.TypeOf<ArgumentException>());
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
