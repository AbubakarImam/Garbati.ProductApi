using Garbati.ProductApi.Authorization;
using Garbati.ProductApi.Contracts.Requests;
using Garbati.ProductApi.Contracts.Responses;
using Garbati.ProductApi.Services.Interfaces;
using Garbati.ProductApi.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Garbati.ProductApi.Controllers;

// DESIGN.md Section 3. Route literals (not [controller]) match the versioned paths the design doc
// spells out exactly (ARCHITECTURE.md Section 5: "/v1/ in the path").
[ApiController]
[Route("v1/products")]
public class ProductsController(
    IProductService productService,
    CreateProductRequestValidator createProductValidator) : ControllerBase
{
    [Authorize(Policy = ProductScopes.CatalogWrite)]
    [HttpPost]
    public async Task<ActionResult<ProductResponse>> CreateProduct(
        [FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var errors = createProductValidator.Validate(request);
        if (errors.Count > 0)
        {
            return BadRequest(errors);
        }

        var response = await productService.CreateAsync(request, cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = ProductScopes.CatalogRead)]
    [HttpGet("{productCode}")]
    public async Task<ActionResult<ProductResponse>> GetProduct(string productCode, CancellationToken cancellationToken)
    {
        var response = await productService.GetAsync(productCode, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }
}
