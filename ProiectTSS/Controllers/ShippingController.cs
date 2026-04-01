using Microsoft.AspNetCore.Mvc;
using ProiectTSS.Dtos;
using ProiectTSS.IServices;

namespace ProiectTSS.Controllers;

[ApiController]
[Route("shipping")]
/// <summary>
/// Exposes shipping quote operations.
/// </summary>
public class ShippingController(IShippingCalculatorService calculatorService) : ControllerBase
{
    [HttpPost("quote")]
    /// <summary>
    /// Calculates a shipping quote using the request pricing configuration.
    /// </summary>
    /// <param name="request">Shipping quote input payload.</param>
    /// <returns>Calculated quote or validation error.</returns>
    public ActionResult<ShippingQuoteResponse> Quote([FromBody] ShippingQuoteRequest request)
    {
        try
        {
            var result = calculatorService.Calculate(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
