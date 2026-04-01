using System.Text.Json.Serialization;

namespace ProiectTSS.Dtos;

/// <summary>
/// Input payload used to request a shipping quote.
/// </summary>
public class ShippingQuoteRequest
{
    /// <summary>
    /// Target shipping zone. When omitted, fallback pricing can be used.
    /// </summary>
    public ShippingZone? Zone { get; set; }

    /// <summary>
    /// Order subtotal used for free-shipping threshold checks.
    /// </summary>
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Optional delivery flags (rapid/fragile).
    /// </summary>
    public ShippingOptions Options { get; set; } = new();

    /// <summary>
    /// Parcels included in the shipment.
    /// </summary>
    public List<ParcelInput> Parcels { get; set; } = [];

    /// <summary>
    /// Optional coupon applied to shipping only.
    /// </summary>
    public CouponInput? Coupon { get; set; }

    /// <summary>
    /// Selected pricing model used by the calculator.
    /// </summary>
    public PricingModel? PricingModel { get; set; }

    /// <summary>
    /// Selected weight rounding behavior.
    /// </summary>
    public RoundingRule? RoundingRule { get; set; }

    /// <summary>
    /// Optional order subtotal threshold that enables free shipping.
    /// </summary>
    public decimal? FreeShippingThreshold { get; set; }

    /// <summary>
    /// Optional maximum shipping cap.
    /// </summary>
    public decimal? MaxCap { get; set; }

    /// <summary>
    /// Optional fallback base shipping fee when zone is not provided.
    /// </summary>
    public decimal? FallbackZonePrice { get; set; }
}

/// <summary>
/// Optional shipping modifiers applied on top of base pricing.
/// </summary>
public class ShippingOptions
{
    /// <summary>
    /// Indicates if rapid delivery surcharge should be applied.
    /// </summary>
    public bool Rapid { get; set; }

    /// <summary>
    /// Indicates if fragile handling surcharge should be applied.
    /// </summary>
    public bool Fragil { get; set; }
}

/// <summary>
/// Parcel input used in quote calculation.
/// </summary>
public class ParcelInput
{
    /// <summary>
    /// Parcel weight in kilograms.
    /// </summary>
    public decimal WeightKg { get; set; }

    /// <summary>
    /// Parcel size category used for size-based fees.
    /// </summary>
    public ParcelSize? Size { get; set; }
}

/// <summary>
/// Coupon data applied only to transport cost.
/// </summary>
public class CouponInput
{
    /// <summary>
    /// Coupon discount type.
    /// </summary>
    public CouponType? Type { get; set; }

    /// <summary>
    /// Coupon value (percentage or fixed amount, based on <see cref="Type"/>).
    /// </summary>
    public decimal Value { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
/// <summary>
/// Delivery area used for zone-based pricing.
/// </summary>
public enum ShippingZone
{
    Local = 1,
    National = 2,
    International = 3
}

[JsonConverter(typeof(JsonStringEnumConverter))]
/// <summary>
/// Pricing strategy used to compute shipping cost.
/// </summary>
public enum PricingModel
{
    Brackets = 1,
    BasePlusPerKg = 2
}

[JsonConverter(typeof(JsonStringEnumConverter))]
/// <summary>
/// Weight rounding strategy applied before pricing.
/// </summary>
public enum RoundingRule
{
    None = 1,
    Ceil1Kg = 2,
    Ceil0_5Kg = 3
}

[JsonConverter(typeof(JsonStringEnumConverter))]
/// <summary>
/// Parcel size category used for size-based surcharge.
/// </summary>
public enum ParcelSize
{
    Small = 1,
    Medium = 2,
    Large = 3
}

[JsonConverter(typeof(JsonStringEnumConverter))]
/// <summary>
/// Coupon discount model.
/// </summary>
public enum CouponType
{
    Percent = 1,
    Fixed = 2
}
