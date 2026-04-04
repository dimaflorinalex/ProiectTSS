using ProiectTSS.Dtos;
using ProiectTSS.Services;

namespace ProiectTSS.UnitTests;

/// <summary>
/// Randomized input tests (light fuzzing) for robustness demonstration.
/// </summary>
public class StrategyRandomizedFuzzingTests
{
    private readonly ShippingCalculatorService _service = new();

    /// <summary>
    /// Generates deterministic random valid requests and verifies safety invariants.
    /// </summary>
    [Test]
    [Category("Randomized.Fuzzing")]
    public void Calculate_WhenRandomValidInputsAreGenerated_PreservesBasicInvariants()
    {
        // Arrange
        var random = new Random(2026);

        // Act + Assert
        for (var i = 0; i < 100; i++)
        {
            var request = CreateRandomValidRequest(random);
            var result = _service.Calculate(request);

            Assert.Multiple(() =>
            {
                Assert.That(result.ShippingCost, Is.GreaterThanOrEqualTo(0m));
                Assert.That(result.Currency, Is.EqualTo("RON"));
                Assert.That(result.Breakdown.SubtotalAfterDiscounts, Is.EqualTo(result.ShippingCost));
            });
        }
    }

    private static ShippingQuoteRequest CreateRandomValidRequest(Random random)
    {
        var zone = random.Next(0, 4) switch
        {
            0 => ShippingZone.Local,
            1 => ShippingZone.National,
            2 => ShippingZone.International,
            _ => (ShippingZone?)null
        };

        var model = random.Next(0, 2) == 0 ? PricingModel.Brackets : PricingModel.BasePlusPerKg;
        var rounding = random.Next(0, 3) switch
        {
            0 => RoundingRule.None,
            1 => RoundingRule.Ceil1Kg,
            _ => RoundingRule.Ceil0_5Kg
        };

        var parcels = Enumerable.Range(0, random.Next(1, 4))
            .Select(_ => new ParcelInput
            {
                WeightKg = Math.Round((decimal)(random.NextDouble() * 15), 2),
                Size = random.Next(0, 3) switch
                {
                    0 => ParcelSize.Small,
                    1 => ParcelSize.Medium,
                    _ => ParcelSize.Large
                }
            })
            .ToList();

        var useCoupon = random.Next(0, 2) == 0;
        var coupon = useCoupon
            ? new CouponInput
            {
                Type = random.Next(0, 2) == 0 ? CouponType.Percent : CouponType.Fixed,
                Value = Math.Round((decimal)(random.NextDouble() * 30), 2)
            }
            : null;

        var subtotal = Math.Round((decimal)(random.NextDouble() * 400), 2);

        return new ShippingQuoteRequest
        {
            Zone = zone,
            Subtotal = subtotal,
            Options = new ShippingOptions
            {
                Rapid = random.Next(0, 2) == 0,
                Fragil = random.Next(0, 2) == 0
            },
            Parcels = parcels,
            Coupon = coupon,
            PricingModel = model,
            RoundingRule = rounding,
            FreeShippingThreshold = random.Next(0, 2) == 0 ? 200m : null,
            MaxCap = random.Next(0, 2) == 0 ? 120m : null,
            FallbackZonePrice = zone is null ? 20m : null
        };
    }
}
