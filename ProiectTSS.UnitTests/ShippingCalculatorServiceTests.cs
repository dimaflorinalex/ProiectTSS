using ProiectTSS.Dtos;
using ProiectTSS.Services;

namespace ProiectTSS.UnitTests;

/// <summary>
/// Unit tests for <see cref="ShippingCalculatorService"/>.
/// </summary>
public class ShippingCalculatorServiceTests
{
    private ShippingCalculatorService _service = null!;

    /// <summary>
    /// Initializes test dependencies.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        _service = new ShippingCalculatorService();
    }

    /// <summary>
    /// Validates local bracket pricing for the 0-1 kg interval.
    /// </summary>
    [Test]
    public void Calculate_WhenBracketsLocalAndWeightBetween0And1_ReturnsExpectedCost()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Zone = ShippingZone.Local;
        request.PricingModel = PricingModel.Brackets;
        request.Parcels = [new ParcelInput { WeightKg = 0.8m, Size = ParcelSize.Small }];

        // Act
        var result = _service.Calculate(request);

        // Assert
        Assert.That(result.ShippingCost, Is.EqualTo(8m));
        Assert.That(result.RuleApplied, Is.EqualTo("BRACKETS"));
    }

    /// <summary>
    /// Validates national bracket pricing for the 1-5 kg interval.
    /// </summary>
    [Test]
    public void Calculate_WhenBracketsNationalAndWeightBetween1And5_ReturnsExpectedCost()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Zone = ShippingZone.National;
        request.PricingModel = PricingModel.Brackets;
        request.Parcels = [new ParcelInput { WeightKg = 3.2m, Size = ParcelSize.Medium }];

        // Act
        var result = _service.Calculate(request);

        // Assert
        Assert.That(result.ShippingCost, Is.EqualTo(25m));
    }

    /// <summary>
    /// Validates international bracket pricing for the 5-10 kg interval.
    /// </summary>
    [Test]
    public void Calculate_WhenBracketsInternationalAndWeightBetween5And10_ReturnsExpectedCost()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Zone = ShippingZone.International;
        request.PricingModel = PricingModel.Brackets;
        request.Parcels = [new ParcelInput { WeightKg = 7.5m, Size = ParcelSize.Large }];

        // Act
        var result = _service.Calculate(request);

        // Assert
        Assert.That(result.ShippingCost, Is.EqualTo(85m));
    }

    /// <summary>
    /// Validates bracket extra fee branch for weights above 10 kg.
    /// </summary>
    [Test]
    public void Calculate_WhenBracketsAndWeightAbove10_UsesExtraPerKgFee()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Zone = ShippingZone.National;
        request.PricingModel = PricingModel.Brackets;
        request.Parcels = [new ParcelInput { WeightKg = 12.5m, Size = ParcelSize.Small }];

        // Act
        var result = _service.Calculate(request);

        // Assert
        Assert.That(result.ShippingCost, Is.EqualTo(45m));
    }

    /// <summary>
    /// Validates base-plus-per-kg pricing model.
    /// </summary>
    [Test]
    public void Calculate_WhenBasePlusPerKgModel_ReturnsExpectedCost()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Zone = ShippingZone.Local;
        request.PricingModel = PricingModel.BasePlusPerKg;
        request.Parcels = [new ParcelInput { WeightKg = 4m, Size = ParcelSize.Medium }];

        // Act
        var result = _service.Calculate(request);

        // Assert
        Assert.That(result.ShippingCost, Is.EqualTo(20m));
        Assert.That(result.RuleApplied, Is.EqualTo("BASE_PLUS_PER_KG"));
    }

    /// <summary>
    /// Validates weight rounding with ceil to 1 kg.
    /// </summary>
    [Test]
    public void Calculate_WhenRoundingCeil1Kg_AppliesRoundedWeight()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Zone = ShippingZone.Local;
        request.PricingModel = PricingModel.BasePlusPerKg;
        request.RoundingRule = RoundingRule.Ceil1Kg;
        request.Parcels = [new ParcelInput { WeightKg = 1.2m, Size = ParcelSize.Small }];

        // Act
        var result = _service.Calculate(request);

        // Assert
        Assert.That(result.ShippingCost, Is.EqualTo(14m));
    }

    /// <summary>
    /// Validates weight rounding with ceil to 0.5 kg.
    /// </summary>
    [Test]
    public void Calculate_WhenRoundingCeilHalfKg_AppliesRoundedWeight()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Zone = ShippingZone.Local;
        request.PricingModel = PricingModel.BasePlusPerKg;
        request.RoundingRule = RoundingRule.Ceil0_5Kg;
        request.Parcels = [new ParcelInput { WeightKg = 1.2m, Size = ParcelSize.Small }];

        // Act
        var result = _service.Calculate(request);

        // Assert
        Assert.That(result.ShippingCost, Is.EqualTo(13m));
    }

    /// <summary>
    /// Validates fragile and rapid surcharges.
    /// </summary>
    [Test]
    public void Calculate_WhenFragileAndRapidEnabled_AppliesBothSurcharges()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Zone = ShippingZone.National;
        request.PricingModel = PricingModel.Brackets;
        request.Options = new ShippingOptions { Fragil = true, Rapid = true };
        request.Parcels = [new ParcelInput { WeightKg = 2.5m, Size = ParcelSize.Medium }];

        // Act
        var result = _service.Calculate(request);

        // Assert
        Assert.That(result.ShippingCost, Is.EqualTo(39m));
        Assert.That(result.Breakdown.FragileSurcharge, Is.EqualTo(5m));
        Assert.That(result.Breakdown.RapidSurcharge, Is.EqualTo(9m));
    }

    /// <summary>
    /// Validates cumulative pricing for multiple parcels.
    /// </summary>
    [Test]
    public void Calculate_WhenMultipleParcels_AggregatesAllParcelCosts()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Zone = ShippingZone.International;
        request.PricingModel = PricingModel.Brackets;
        request.Parcels =
        [
            new ParcelInput { WeightKg = 0.7m, Size = ParcelSize.Small },
            new ParcelInput { WeightKg = 4.8m, Size = ParcelSize.Medium },
            new ParcelInput { WeightKg = 9.3m, Size = ParcelSize.Large }
        ];

        // Act
        var result = _service.Calculate(request);

        // Assert
        Assert.That(result.ShippingCost, Is.EqualTo(161m));
    }

    /// <summary>
    /// Validates percentage coupon discount on shipping.
    /// </summary>
    [Test]
    public void Calculate_WhenPercentCouponProvided_AppliesPercentDiscount()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Zone = ShippingZone.Local;
        request.PricingModel = PricingModel.Brackets;
        request.Parcels = [new ParcelInput { WeightKg = 3m, Size = ParcelSize.Medium }];
        request.Coupon = new CouponInput { Type = CouponType.Percent, Value = 25m };

        // Act
        var result = _service.Calculate(request);

        // Assert
        Assert.That(result.ShippingCost, Is.EqualTo(12.75m));
        Assert.That(result.Breakdown.CouponDiscount, Is.EqualTo(4.25m));
    }

    /// <summary>
    /// Validates fixed coupon discount on shipping.
    /// </summary>
    [Test]
    public void Calculate_WhenFixedCouponProvided_AppliesFixedDiscount()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Zone = ShippingZone.Local;
        request.PricingModel = PricingModel.Brackets;
        request.Parcels = [new ParcelInput { WeightKg = 3m, Size = ParcelSize.Medium }];
        request.Coupon = new CouponInput { Type = CouponType.Fixed, Value = 8m };

        // Act
        var result = _service.Calculate(request);

        // Assert
        Assert.That(result.ShippingCost, Is.EqualTo(9m));
        Assert.That(result.Breakdown.CouponDiscount, Is.EqualTo(8m));
    }

    /// <summary>
    /// Validates coupon discount clamping to shipping value.
    /// </summary>
    [Test]
    public void Calculate_WhenFixedCouponExceedsShipping_ClampsDiscountToShippingCost()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Coupon = new CouponInput { Type = CouponType.Fixed, Value = 100m };

        // Act
        var result = _service.Calculate(request);

        // Assert
        Assert.That(result.ShippingCost, Is.EqualTo(0m));
        Assert.That(result.Breakdown.CouponDiscount, Is.EqualTo(15m));
    }

    /// <summary>
    /// Validates free shipping threshold branch.
    /// </summary>
    [Test]
    public void Calculate_WhenFreeShippingThresholdIsMet_ReturnsZeroShipping()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Zone = ShippingZone.National;
        request.PricingModel = PricingModel.Brackets;
        request.Subtotal = 250m;
        request.FreeShippingThreshold = 200m;
        request.Coupon = new CouponInput { Type = CouponType.Fixed, Value = 5m };
        request.Parcels = [new ParcelInput { WeightKg = 2.2m, Size = ParcelSize.Small }];

        // Act
        var result = _service.Calculate(request);

        // Assert
        Assert.That(result.ShippingCost, Is.EqualTo(0m));
        Assert.That(result.Breakdown.FreeShippingDiscount, Is.EqualTo(22m));
        Assert.That(result.Breakdown.CouponDiscount, Is.EqualTo(0m));
    }

    /// <summary>
    /// Validates max cap application.
    /// </summary>
    [Test]
    public void Calculate_WhenMaxCapIsSetAndExceeded_AppliesCapReduction()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Zone = ShippingZone.International;
        request.PricingModel = PricingModel.Brackets;
        request.Options = new ShippingOptions { Fragil = true, Rapid = true };
        request.Parcels = [new ParcelInput { WeightKg = 8.5m, Size = ParcelSize.Large }];
        request.MaxCap = 60m;

        // Act
        var result = _service.Calculate(request);

        // Assert
        Assert.That(result.ShippingCost, Is.EqualTo(60m));
        Assert.That(result.Breakdown.CapReduction, Is.EqualTo(57m));
    }

    /// <summary>
    /// Validates fallback pricing when zone is not specified.
    /// </summary>
    [Test]
    public void Calculate_WhenZoneIsNullAndFallbackProvided_UsesFallbackBaseFee()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Zone = null;
        request.FallbackZonePrice = 19.5m;

        // Act
        var result = _service.Calculate(request);

        // Assert
        Assert.That(result.ShippingCost, Is.EqualTo(19.5m));
        Assert.That(result.RuleApplied, Is.EqualTo("FALLBACK_ZONE_PRICE"));
    }

    /// <summary>
    /// Validates invalid enum fallback behavior for rounding rule default branch.
    /// </summary>
    [Test]
    public void Calculate_WhenRoundingRuleHasInvalidEnumValue_UsesNoRoundingDefaultBranch()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Zone = ShippingZone.Local;
        request.PricingModel = PricingModel.BasePlusPerKg;
        request.RoundingRule = (RoundingRule)999;
        request.Parcels = [new ParcelInput { WeightKg = 1.2m, Size = ParcelSize.Small }];

        // Act
        var result = _service.Calculate(request);

        // Assert
        Assert.That(result.ShippingCost, Is.EqualTo(12.4m));
    }

    /// <summary>
    /// Validates invalid enum fallback behavior for base fee/per-kg default branches.
    /// </summary>
    [Test]
    public void Calculate_WhenZoneHasInvalidEnumValueInBaseModel_UsesDefaultZoneRates()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Zone = (ShippingZone)999;
        request.PricingModel = PricingModel.BasePlusPerKg;
        request.Parcels = [new ParcelInput { WeightKg = 2m, Size = ParcelSize.Medium }];

        // Act
        var result = _service.Calculate(request);

        // Assert
        Assert.That(result.Breakdown.BaseFee, Is.EqualTo(20m));
        Assert.That(result.Breakdown.PerKgFee, Is.EqualTo(5m));
        Assert.That(result.Breakdown.SizeFee, Is.EqualTo(3m));
        Assert.That(result.ShippingCost, Is.EqualTo(28m));
    }

    /// <summary>
    /// Validates invalid enum fallback behavior for size default branch.
    /// </summary>
    [Test]
    public void Calculate_WhenParcelSizeHasInvalidEnumValue_UsesDefaultSizeFeeBranch()
    {
        // Arrange
        var request = CreateValidRequest();
        request.PricingModel = PricingModel.BasePlusPerKg;
        request.Parcels = [new ParcelInput { WeightKg = 1m, Size = (ParcelSize)999 }];

        // Act
        var result = _service.Calculate(request);

        // Assert
        Assert.That(result.Breakdown.SizeFee, Is.EqualTo(0m));
        Assert.That(result.ShippingCost, Is.EqualTo(12m));
    }

    /// <summary>
    /// Validates invalid enum fallback behavior for coupon default branch.
    /// </summary>
    [Test]
    public void Calculate_WhenCouponTypeHasInvalidEnumValue_UsesZeroDiscountDefaultBranch()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Coupon = new CouponInput { Type = (CouponType)999, Value = 10m };

        // Act
        var result = _service.Calculate(request);

        // Assert
        Assert.That(result.Breakdown.CouponDiscount, Is.EqualTo(0m));
        Assert.That(result.ShippingCost, Is.EqualTo(15m));
    }

    /// <summary>
    /// Validates decimal rounding behavior in monetary outputs.
    /// </summary>
    [Test]
    public void Calculate_WhenResultHasManyDecimals_RoundsCurrencyToTwoDecimals()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Zone = ShippingZone.Local;
        request.PricingModel = PricingModel.BasePlusPerKg;
        request.RoundingRule = RoundingRule.None;
        request.Options = new ShippingOptions { Rapid = true, Fragil = false };
        request.Parcels = [new ParcelInput { WeightKg = 1m / 3m, Size = ParcelSize.Small }];

        // Act
        var result = _service.Calculate(request);

        // Assert
        Assert.That(result.Breakdown.SubtotalBeforeDiscounts, Is.EqualTo(13.87m));
        Assert.That(result.ShippingCost, Is.EqualTo(13.87m));
    }

    /// <summary>
    /// Throws when parcels list is empty.
    /// </summary>
    [Test]
    public void Calculate_WhenParcelsAreEmpty_ThrowsArgumentException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Parcels = [];

        // Act
        var action = () => _service.Calculate(request);

        // Assert
        Assert.That(action, Throws.TypeOf<ArgumentException>().With.Message.EqualTo("parcels must contain at least one item."));
    }

    /// <summary>
    /// Throws when parcel weight is negative.
    /// </summary>
    [Test]
    public void Calculate_WhenParcelWeightIsNegative_ThrowsArgumentException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Parcels = [new ParcelInput { WeightKg = -1m, Size = ParcelSize.Small }];

        // Act
        var action = () => _service.Calculate(request);

        // Assert
        Assert.That(action, Throws.TypeOf<ArgumentException>().With.Message.EqualTo("weightKg must be >= 0."));
    }

    /// <summary>
    /// Throws when subtotal is negative.
    /// </summary>
    [Test]
    public void Calculate_WhenSubtotalIsNegative_ThrowsArgumentException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Subtotal = -1m;

        // Act
        var action = () => _service.Calculate(request);

        // Assert
        Assert.That(action, Throws.TypeOf<ArgumentException>().With.Message.EqualTo("subtotal must be >= 0."));
    }

    /// <summary>
    /// Throws when zone is null and fallback is missing.
    /// </summary>
    [Test]
    public void Calculate_WhenZoneIsNullAndFallbackMissing_ThrowsArgumentException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Zone = null;
        request.FallbackZonePrice = null;

        // Act
        var action = () => _service.Calculate(request);

        // Assert
        Assert.That(action, Throws.TypeOf<ArgumentException>().With.Message.EqualTo("zone is unknown and fallbackZonePrice is missing."));
    }

    /// <summary>
    /// Throws when pricing model is not set.
    /// </summary>
    [Test]
    public void Calculate_WhenPricingModelIsNull_ThrowsArgumentException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.PricingModel = null;

        // Act
        var action = () => _service.Calculate(request);

        // Assert
        Assert.That(action, Throws.TypeOf<ArgumentException>().With.Message.EqualTo("pricingModel must be explicitly set."));
    }

    /// <summary>
    /// Throws when rounding rule is not set.
    /// </summary>
    [Test]
    public void Calculate_WhenRoundingRuleIsNull_ThrowsArgumentException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.RoundingRule = null;

        // Act
        var action = () => _service.Calculate(request);

        // Assert
        Assert.That(action, Throws.TypeOf<ArgumentException>().With.Message.EqualTo("roundingRule must be explicitly set."));
    }

    /// <summary>
    /// Throws when parcel size is missing.
    /// </summary>
    [Test]
    public void Calculate_WhenParcelSizeIsNull_ThrowsArgumentException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Parcels = [new ParcelInput { WeightKg = 1m, Size = null }];

        // Act
        var action = () => _service.Calculate(request);

        // Assert
        Assert.That(action, Throws.TypeOf<ArgumentException>().With.Message.EqualTo("parcel size must be explicitly set."));
    }

    /// <summary>
    /// Throws when coupon type is missing.
    /// </summary>
    [Test]
    public void Calculate_WhenCouponTypeIsNull_ThrowsArgumentException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Coupon = new CouponInput { Type = null, Value = 10m };

        // Act
        var action = () => _service.Calculate(request);

        // Assert
        Assert.That(action, Throws.TypeOf<ArgumentException>().With.Message.EqualTo("coupon.type must be explicitly set."));
    }

    /// <summary>
    /// Throws when coupon value is negative.
    /// </summary>
    [Test]
    public void Calculate_WhenCouponValueIsNegative_ThrowsArgumentException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Coupon = new CouponInput { Type = CouponType.Percent, Value = -1m };

        // Act
        var action = () => _service.Calculate(request);

        // Assert
        Assert.That(action, Throws.TypeOf<ArgumentException>().With.Message.EqualTo("coupon.value must be >= 0."));
    }

    /// <summary>
    /// Throws when zone is null and fallback is negative.
    /// </summary>
    [Test]
    public void Calculate_WhenZoneIsNullAndFallbackIsNegative_ThrowsArgumentException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Zone = null;
        request.FallbackZonePrice = -1m;

        // Act
        var action = () => _service.Calculate(request);

        // Assert
        Assert.That(action, Throws.TypeOf<ArgumentException>().With.Message.EqualTo("Unknown zone requires fallbackZonePrice."));
    }

    /// <summary>
    /// Throws when bracket model receives an invalid zone enum value.
    /// </summary>
    [Test]
    public void Calculate_WhenBracketsWithInvalidZoneEnum_ThrowsArgumentException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Zone = (ShippingZone)999;
        request.PricingModel = PricingModel.Brackets;

        // Act
        var action = () => _service.Calculate(request);

        // Assert
        Assert.That(action, Throws.TypeOf<ArgumentException>().With.Message.EqualTo("Unknown zone."));
    }

    /// <summary>
    /// Covers national zone branch for base fee and per-kg rate in base-plus-per-kg model.
    /// </summary>
    [Test]
    public void Calculate_WhenBasePlusPerKgWithNationalZone_UsesNationalBaseAndRate()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Zone = ShippingZone.National;
        request.PricingModel = PricingModel.BasePlusPerKg;
        request.Parcels = [new ParcelInput { WeightKg = 2m, Size = ParcelSize.Small }];

        // Act
        var result = _service.Calculate(request);

        // Assert
        Assert.That(result.Breakdown.BaseFee, Is.EqualTo(15m));
        Assert.That(result.Breakdown.PerKgFee, Is.EqualTo(6m));
    }

    /// <summary>
    /// Covers international zone branch for base fee and per-kg rate in base-plus-per-kg model.
    /// </summary>
    [Test]
    public void Calculate_WhenBasePlusPerKgWithInternationalZone_UsesInternationalBaseAndRate()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Zone = ShippingZone.International;
        request.PricingModel = PricingModel.BasePlusPerKg;
        request.Parcels = [new ParcelInput { WeightKg = 2m, Size = ParcelSize.Small }];

        // Act
        var result = _service.Calculate(request);

        // Assert
        Assert.That(result.Breakdown.BaseFee, Is.EqualTo(30m));
        Assert.That(result.Breakdown.PerKgFee, Is.EqualTo(12m));
    }

    /// <summary>
    /// Covers national and international size-fee branches for large parcels.
    /// </summary>
    [Test]
    public void Calculate_WhenLargeParcelsAcrossZones_UsesZoneSpecificLargeSizeFees()
    {
        // Arrange
        var nationalRequest = CreateValidRequest();
        nationalRequest.Zone = ShippingZone.National;
        nationalRequest.PricingModel = PricingModel.BasePlusPerKg;
        nationalRequest.Parcels = [new ParcelInput { WeightKg = 1m, Size = ParcelSize.Large }];

        var internationalRequest = CreateValidRequest();
        internationalRequest.Zone = ShippingZone.International;
        internationalRequest.PricingModel = PricingModel.BasePlusPerKg;
        internationalRequest.Parcels = [new ParcelInput { WeightKg = 1m, Size = ParcelSize.Large }];

        // Act
        var nationalResult = _service.Calculate(nationalRequest);
        var internationalResult = _service.Calculate(internationalRequest);

        // Assert
        Assert.That(nationalResult.Breakdown.SizeFee, Is.EqualTo(8m));
        Assert.That(internationalResult.Breakdown.SizeFee, Is.EqualTo(15m));
    }

    /// <summary>
    /// Covers local zone branch for large parcel size fee.
    /// </summary>
    [Test]
    public void Calculate_WhenLargeParcelInLocalZone_UsesLocalLargeSizeFee()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Zone = ShippingZone.Local;
        request.PricingModel = PricingModel.BasePlusPerKg;
        request.Parcels = [new ParcelInput { WeightKg = 1m, Size = ParcelSize.Large }];

        // Act
        var result = _service.Calculate(request);

        // Assert
        Assert.That(result.Breakdown.SizeFee, Is.EqualTo(5m));
    }

    /// <summary>
    /// Covers default large-size branch for invalid zone enum values.
    /// </summary>
    [Test]
    public void Calculate_WhenLargeParcelAndInvalidZone_UsesDefaultLargeSizeFee()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Zone = (ShippingZone)999;
        request.PricingModel = PricingModel.BasePlusPerKg;
        request.Parcels = [new ParcelInput { WeightKg = 1m, Size = ParcelSize.Large }];

        // Act
        var result = _service.Calculate(request);

        // Assert
        Assert.That(result.Breakdown.SizeFee, Is.EqualTo(8m));
    }

    /// <summary>
    /// Creates a valid baseline request used by tests.
    /// </summary>
    /// <returns>Valid request instance with explicit mandatory values.</returns>
    private static ShippingQuoteRequest CreateValidRequest() =>
        new()
        {
            Zone = ShippingZone.Local,
            Subtotal = 100m,
            Options = new ShippingOptions { Rapid = false, Fragil = false },
            Parcels = [new ParcelInput { WeightKg = 2m, Size = ParcelSize.Small }],
            Coupon = null,
            PricingModel = PricingModel.Brackets,
            RoundingRule = RoundingRule.None,
            FreeShippingThreshold = null,
            MaxCap = null,
            FallbackZonePrice = null
        };
}
