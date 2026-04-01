using ProiectTSS.Dtos;

namespace ProiectTSS.IServices;

/// <summary>
/// Provides shipping quote calculation based on request rules and options.
/// </summary>
public interface IShippingCalculatorService
{
    /// <summary>
    /// Calculates the shipping quote for the provided input.
    /// </summary>
    /// <param name="request">Shipping quote request payload.</param>
    /// <returns>Calculated shipping quote with cost breakdown.</returns>
    ShippingQuoteResponse Calculate(ShippingQuoteRequest request);
}
