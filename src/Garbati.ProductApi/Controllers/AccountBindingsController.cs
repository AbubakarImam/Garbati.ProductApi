using Garbati.ProductApi.Authorization;
using Garbati.ProductApi.Contracts.Requests;
using Garbati.ProductApi.Contracts.Responses;
using Garbati.ProductApi.Services.Interfaces;
using Garbati.ProductApi.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Garbati.ProductApi.Controllers;

// DESIGN.md Section 3.
[ApiController]
[Route("v1/account-bindings")]
public class AccountBindingsController(
    IAccountProductBindingService bindingService,
    CreateAccountProductBindingRequestValidator createBindingValidator) : ControllerBase
{
    [Authorize(Policy = ProductScopes.CatalogWrite)]
    [HttpPost]
    public async Task<ActionResult<AccountProductBindingResponse>> CreateAccountBinding(
        [FromBody] CreateAccountProductBindingRequest request, CancellationToken cancellationToken)
    {
        var errors = createBindingValidator.Validate(request);
        if (errors.Count > 0)
        {
            return BadRequest(errors);
        }

        var response = await bindingService.CreateAsync(request, cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = ProductScopes.CatalogRead)]
    [HttpGet("{accountNumber}")]
    public async Task<ActionResult<AccountProductBindingResponse>> GetAccountBinding(string accountNumber, CancellationToken cancellationToken)
    {
        var response = await bindingService.GetAsync(accountNumber, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }
}
