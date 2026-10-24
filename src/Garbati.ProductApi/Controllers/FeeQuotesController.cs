using Garbati.ProductApi.Authorization;
using Garbati.ProductApi.Contracts.Requests;
using Garbati.ProductApi.Contracts.Responses;
using Garbati.ProductApi.Services.Interfaces;
using Garbati.ProductApi.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Garbati.ProductApi.Controllers;

// DESIGN.md Section 3, endpoint 5. See Services/Implementations/FeeQuoteService.cs for why the only
// response any real caller gets today is NoFeeConfigured.
[ApiController]
[Route("v1/fee-quotes")]
public class FeeQuotesController(
    IFeeQuoteService feeQuoteService,
    FeeQuoteRequestValidator feeQuoteValidator) : ControllerBase
{
    [Authorize(Policy = ProductScopes.PricingRead)]
    [HttpPost]
    public async Task<ActionResult<FeeQuoteResponse>> GetFeeQuote(
        [FromBody] FeeQuoteRequest request, CancellationToken cancellationToken)
    {
        var errors = feeQuoteValidator.Validate(request);
        if (errors.Count > 0)
        {
            return BadRequest(errors);
        }

        var response = await feeQuoteService.GetQuoteAsync(request, cancellationToken);
        return Ok(response);
    }
}
