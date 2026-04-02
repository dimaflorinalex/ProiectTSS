using ProiectTSS.Dtos;
using ProiectTSS.IServices;

namespace ProiectTSS.Services;

/// <summary>
/// Default implementation of shipping quote calculation.
/// </summary>
public class ShippingCalculatorService : IShippingCalculatorService
{
    private const string RuleFallbackZonePrice = "FALLBACK_ZONE_PRICE";
    private const string RuleBrackets = "BRACKETS";
    private const string RuleBasePlusPerKg = "BASE_PLUS_PER_KG";
    private const string RuleFragileSurcharge = "FRAGILE_SURCHARGE";
    private const string RuleRapidSurcharge = "RAPID_SURCHARGE";
    private const string RuleFreeShippingThreshold = "FREE_SHIPPING_THRESHOLD";
    private const string RuleCouponDiscount = "COUPON_DISCOUNT";
    private const string RuleMaxCap = "MAX_CAP";

    /// <summary>
    /// Calculates total shipping cost and breakdown for a quote request.
    /// </summary>
    /// <param name="request">Quote request containing parcels, zone and pricing options.</param>
    /// <returns>Calculated shipping quote.</returns>
    public ShippingQuoteResponse Calculate(ShippingQuoteRequest request)
    {
        var zone = request.Zone;
        var model = request.PricingModel;
        var roundingRule = request.RoundingRule;

        ValidateRequest(request, zone, model, roundingRule);

        var breakdown = new ShippingBreakdown();
        var rules = new List<string>();

        if (zone is null)
        {
            breakdown.BaseFee = request.FallbackZonePrice!.Value;
            rules.Add(RuleFallbackZonePrice);
        }
        else
        {
            if (model == PricingModel.Brackets)
            {
                rules.Add(RuleBrackets);
                foreach (var parcel in request.Parcels)
                {
                    var roundedWeight = ApplyRounding(parcel.WeightKg, roundingRule!.Value);
                    breakdown.PerKgFee += GetBracketFee(zone!.Value, roundedWeight);
                    breakdown.SizeFee += GetSizeFee(zone.Value, parcel.Size!.Value);
                }
            }
            else
            {
                rules.Add(RuleBasePlusPerKg);
                breakdown.BaseFee = GetBaseFee(zone!.Value);
                var perKgRate = GetPerKgRate(zone.Value);
                foreach (var parcel in request.Parcels)
                {
                    var roundedWeight = ApplyRounding(parcel.WeightKg, roundingRule!.Value);
                    breakdown.PerKgFee += roundedWeight * perKgRate;
                    breakdown.SizeFee += GetSizeFee(zone.Value, parcel.Size!.Value);
                }
            }
        }

        if (request.Options.Fragil)
        {
            breakdown.FragileSurcharge = request.Parcels.Count * 5m;
            rules.Add(RuleFragileSurcharge);
        }

        var preRapid = breakdown.BaseFee + breakdown.PerKgFee + breakdown.SizeFee + breakdown.FragileSurcharge;
        if (request.Options.Rapid)
        {
            breakdown.RapidSurcharge = preRapid * 0.30m;
            rules.Add(RuleRapidSurcharge);
        }

        var gross = preRapid + breakdown.RapidSurcharge;
        breakdown.SubtotalBeforeDiscounts = RoundCurrency(gross);

        var net = gross;
        if (request.FreeShippingThreshold is not null && request.Subtotal > request.FreeShippingThreshold)
        {
            breakdown.FreeShippingDiscount = net;
            net = 0m;
            rules.Add(RuleFreeShippingThreshold);
        }
        else if (request.Coupon is not null)
        {
            breakdown.CouponDiscount = CalculateCouponDiscount(net, request.Coupon);
            net -= breakdown.CouponDiscount;
            rules.Add(RuleCouponDiscount);
        }

        if (request.MaxCap is not null && request.MaxCap >= 0 && net > request.MaxCap)
        {
            breakdown.CapReduction = net - request.MaxCap.Value;
            net = request.MaxCap.Value;
            rules.Add(RuleMaxCap);
        }

        breakdown.BaseFee = RoundCurrency(breakdown.BaseFee);
        breakdown.PerKgFee = RoundCurrency(breakdown.PerKgFee);
        breakdown.SizeFee = RoundCurrency(breakdown.SizeFee);
        breakdown.FragileSurcharge = RoundCurrency(breakdown.FragileSurcharge);
        breakdown.RapidSurcharge = RoundCurrency(breakdown.RapidSurcharge);
        breakdown.CouponDiscount = RoundCurrency(breakdown.CouponDiscount);
        breakdown.FreeShippingDiscount = RoundCurrency(breakdown.FreeShippingDiscount);
        breakdown.CapReduction = RoundCurrency(breakdown.CapReduction);
        breakdown.SubtotalAfterDiscounts = RoundCurrency(net);

        return new ShippingQuoteResponse
        {
            ShippingCost = breakdown.SubtotalAfterDiscounts,
            Currency = "RON",
            Breakdown = breakdown,
            RuleApplied = string.Join(" | ", rules)
        };
    }

    /// <summary>
    /// Validates mandatory request fields and pricing prerequisites before calculation.
    /// </summary>
    /// <param name="request">Quote request payload to validate.</param>
    /// <param name="zone">Selected delivery zone, or <c>null</c> for fallback mode.</param>
    /// <param name="model">Selected pricing model.</param>
    /// <param name="roundingRule">Selected weight rounding rule.</param>
    /// <exception cref="ArgumentException">Thrown when request data is missing or invalid.</exception>
    private static void ValidateRequest(
        ShippingQuoteRequest request,
        ShippingZone? zone,
        PricingModel? model,
        RoundingRule? roundingRule)
    {
        if (request.Parcels.Count == 0)
        {
            throw new ArgumentException("parcels must contain at least one item.");
        }

        if (request.Parcels.Any(p => p.WeightKg < 0))
        {
            throw new ArgumentException("weightKg must be >= 0.");
        }

        if (request.Subtotal < 0)
        {
            throw new ArgumentException("subtotal must be >= 0.");
        }

        if (zone is null && request.FallbackZonePrice is null)
        {
            throw new ArgumentException("zone is unknown and fallbackZonePrice is missing.");
        }

        if (zone is null && request.FallbackZonePrice < 0)
        {
            throw new ArgumentException("Unknown zone requires fallbackZonePrice.");
        }

        if (model is null)
        {
            throw new ArgumentException("pricingModel must be explicitly set.");
        }

        if (roundingRule is null)
        {
            throw new ArgumentException("roundingRule must be explicitly set.");
        }

        if (request.Parcels.Any(p => p.Size is null))
        {
            throw new ArgumentException("parcel size must be explicitly set.");
        }

        if (request.Coupon is not null)
        {
            if (request.Coupon.Type is null)
            {
                throw new ArgumentException("coupon.type must be explicitly set.");
            }

            if (request.Coupon.Value < 0)
            {
                throw new ArgumentException("coupon.value must be >= 0.");
            }
        }
    }

    /// <summary>
    /// Computes coupon discount value for the current shipping subtotal.
    /// </summary>
    /// <param name="shippingBeforeDiscount">Shipping total before coupon application.</param>
    /// <param name="coupon">Coupon details used for discount calculation.</param>
    /// <returns>Discount amount clamped to the current shipping total.</returns>
    private static decimal CalculateCouponDiscount(decimal shippingBeforeDiscount, CouponInput coupon)
    {
        decimal discount = coupon.Type switch
        {
            CouponType.Percent => shippingBeforeDiscount * (coupon.Value / 100m),
            CouponType.Fixed => coupon.Value,
            _ => 0m
        };

        return Math.Min(shippingBeforeDiscount, Math.Max(0m, discount));
    }

    /// <summary>
    /// Applies the selected rounding rule to parcel weight.
    /// </summary>
    /// <param name="weightKg">Original parcel weight in kilograms.</param>
    /// <param name="roundingRule">Rounding rule to apply.</param>
    /// <returns>Rounded parcel weight.</returns>
    private static decimal ApplyRounding(decimal weightKg, RoundingRule roundingRule)
    {
        return roundingRule switch
        {
            RoundingRule.Ceil1Kg => Math.Ceiling(weightKg),
            RoundingRule.Ceil0_5Kg => Math.Ceiling(weightKg * 2m) / 2m,
            _ => weightKg
        };
    }

    /// <summary>
    /// Gets bracket-based fee for a parcel based on zone and weight interval.
    /// </summary>
    /// <param name="zone">Delivery zone used for tariff selection.</param>
    /// <param name="weightKg">Rounded parcel weight in kilograms.</param>
    /// <returns>Bracket fee for the parcel.</returns>
    private static decimal GetBracketFee(ShippingZone zone, decimal weightKg)
    {
        var rates = zone switch
        {
            ShippingZone.Local => new[] { 8m, 15m, 25m, 3m },
            ShippingZone.National => new[] { 12m, 22m, 35m, 4m },
            ShippingZone.International => new[] { 25m, 45m, 70m, 8m },
            _ => throw new ArgumentException("Unknown zone.")
        };

        if (weightKg <= 1m)
        {
            return rates[0];
        }

        if (weightKg <= 5m)
        {
            return rates[1];
        }

        if (weightKg <= 10m)
        {
            return rates[2];
        }

        return rates[2] + ((weightKg - 10m) * rates[3]);
    }

    /// <summary>
    /// Gets base fixed fee for the base-plus-per-kg pricing model.
    /// </summary>
    /// <param name="zone">Delivery zone used for tariff selection.</param>
    /// <returns>Base fee for the specified zone.</returns>
    private static decimal GetBaseFee(ShippingZone zone)
    {
        return zone switch
        {
            ShippingZone.Local => 10m,
            ShippingZone.National => 15m,
            ShippingZone.International => 30m,
            _ => 20m
        };
    }

    /// <summary>
    /// Gets variable per-kilogram rate for the base-plus-per-kg model.
    /// </summary>
    /// <param name="zone">Delivery zone used for tariff selection.</param>
    /// <returns>Per-kilogram rate for the specified zone.</returns>
    private static decimal GetPerKgRate(ShippingZone zone)
    {
        return zone switch
        {
            ShippingZone.Local => 2m,
            ShippingZone.National => 3m,
            ShippingZone.International => 6m,
            _ => 2.5m
        };
    }

    /// <summary>
    /// Gets size-based surcharge for a parcel in the specified zone.
    /// </summary>
    /// <param name="zone">Delivery zone used for tariff selection.</param>
    /// <param name="size">Parcel size category.</param>
    /// <returns>Additional fee based on parcel size and zone.</returns>
    private static decimal GetSizeFee(ShippingZone zone, ParcelSize size)
    {
        return size switch
        {
            ParcelSize.Small => 0m,
            ParcelSize.Medium => zone switch
            {
                ShippingZone.Local => 2m,
                ShippingZone.National => 3m,
                ShippingZone.International => 6m,
                _ => 3m
            },
            ParcelSize.Large => zone switch
            {
                ShippingZone.Local => 5m,
                ShippingZone.National => 8m,
                ShippingZone.International => 15m,
                _ => 8m
            },
            _ => 0m
        };
    }

    /// <summary>
    /// Rounds a monetary amount to two decimals using away-from-zero midpoint rule.
    /// </summary>
    /// <param name="value">Amount to round.</param>
    /// <returns>Rounded currency amount.</returns>
    private static decimal RoundCurrency(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
