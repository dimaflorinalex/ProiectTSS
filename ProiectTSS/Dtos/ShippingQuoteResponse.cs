namespace ProiectTSS.Dtos;

/// <summary>
/// Output payload returned by the shipping quote endpoint.
/// </summary>
public class ShippingQuoteResponse
{
    /// <summary>
    /// Final shipping cost after all adjustments.
    /// </summary>
    public decimal ShippingCost { get; set; }

    /// <summary>
    /// Currency code for monetary values.
    /// </summary>
    public string Currency { get; set; } = "RON";

    /// <summary>
    /// Detailed cost components used to compute <see cref="ShippingCost"/>.
    /// </summary>
    public ShippingBreakdown Breakdown { get; set; } = new();

    /// <summary>
    /// Pipe-separated list of applied pricing rules.
    /// </summary>
    public string RuleApplied { get; set; } = string.Empty;
}

/// <summary>
/// Detailed components used to derive the final shipping cost.
/// </summary>
public class ShippingBreakdown
{
    /// <summary>
    /// Fixed base fee component.
    /// </summary>
    public decimal BaseFee { get; set; }

    /// <summary>
    /// Weight-based fee component.
    /// </summary>
    public decimal PerKgFee { get; set; }

    /// <summary>
    /// Size-based fee component.
    /// </summary>
    public decimal SizeFee { get; set; }

    /// <summary>
    /// Surcharge added for fragile parcels.
    /// </summary>
    public decimal FragileSurcharge { get; set; }

    /// <summary>
    /// Surcharge added for rapid delivery.
    /// </summary>
    public decimal RapidSurcharge { get; set; }

    /// <summary>
    /// Discount amount applied from coupon.
    /// </summary>
    public decimal CouponDiscount { get; set; }

    /// <summary>
    /// Discount amount applied when free shipping threshold is met.
    /// </summary>
    public decimal FreeShippingDiscount { get; set; }

    /// <summary>
    /// Reduction applied because of maximum cap.
    /// </summary>
    public decimal CapReduction { get; set; }

    /// <summary>
    /// Shipping subtotal before discounts and cap.
    /// </summary>
    public decimal SubtotalBeforeDiscounts { get; set; }

    /// <summary>
    /// Shipping subtotal after discounts and cap.
    /// </summary>
    public decimal SubtotalAfterDiscounts { get; set; }
}
