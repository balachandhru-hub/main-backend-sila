using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using SharedKernel.LoggerServices;
using SharedKernel.Dto;
using SharedKernel.Attributes;
using MasterData.Application.Features.Lookup.Queries;
using MasterData.Domain.Dto;

namespace MasterData.API.Controllers;

[ApiController]
public class LookupController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILoggerManager _logger;

    public LookupController(IMediator mediator, ILoggerManager logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get countries in alphabetical order with pagination and search.
    /// </summary>
    [HttpGet]
    [Route("api/v1/masterdata/countries")]
    [ApiAuthorization(Name = "GET_COUNTRIES")]
    [SwaggerOperation("GetCountries")]
    [SwaggerResponse(200, "Countries fetched successfully", typeof(PagedResultDto<CountryDto>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ErrorResponseDto))]
    [SwaggerResponse(500, "Internal Server Error", typeof(ErrorResponseDto))]
    public async Task<IActionResult> GetCountries(
        [FromQuery] int index = 0,
        [FromQuery] int limit = 20,
        [FromQuery] string? searchTerm = null)
    {
        _logger.LogInfo("Fetching countries");

        var result = await _mediator.Send(new GetCountriesQuery
        {
            Index = index,
            Limit = limit,
            SearchTerm = searchTerm
        });

        return Ok(result);
    }

    /// <summary>
    /// Get units in alphabetical order with pagination and search.
    /// </summary>
    [HttpGet]
    [Route("api/v1/masterdata/units")]
    [ApiAuthorization(Name = "GET_UNITS")]
    [SwaggerOperation("GetUnits")]
    [SwaggerResponse(200, "Units fetched successfully", typeof(PagedResultDto<UnitDto>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ErrorResponseDto))]
    [SwaggerResponse(500, "Internal Server Error", typeof(ErrorResponseDto))]
    public async Task<IActionResult> GetUnits(
        [FromQuery] int index = 0,
        [FromQuery] int limit = 20,
        [FromQuery] string? searchTerm = null)
    {
        _logger.LogInfo("Fetching units");

        var result = await _mediator.Send(new GetUnitsQuery
        {
            Index = index,
            Limit = limit,
            SearchTerm = searchTerm
        });

        return Ok(result);
    }

    /// <summary>
    /// Get all currencies.
    /// </summary>
    [HttpGet]
    [Route("api/v1/masterdata/currencies")]
    [ApiAuthorization(Name = "GET_CURRENCIES")]
    [SwaggerOperation("GetCurrencies")]
    [SwaggerResponse(200, "Currencies fetched successfully", typeof(List<CurrencyDto>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ErrorResponseDto))]
    [SwaggerResponse(500, "Internal Server Error", typeof(ErrorResponseDto))]
    public async Task<IActionResult> GetCurrencies()
    {
        _logger.LogInfo("Fetching currencies");

        var result = await _mediator.Send(new GetCurrenciesQuery());

        return Ok(result);
    }
}
