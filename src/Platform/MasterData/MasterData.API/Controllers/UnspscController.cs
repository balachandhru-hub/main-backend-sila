using MediatR;
using Microsoft.AspNetCore.Mvc;
using MasterData.Application.Features.Unspsc.Commands;
using MasterData.Application.Features.Unspsc.Queries;
using SharedKernel.LoggerServices;
using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;
using SharedKernel.Dto;
using SharedKernel.Attributes;



namespace MasterData.API.Controllers;

[ApiController]

public class UnspscController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILoggerManager _logger;

    public UnspscController(IMediator mediator, ILoggerManager logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Uploads UNSPSC Excel file.
    /// </summary>
    [HttpPost]
    [Route("api/v1/masterdata/unspsc/upload")]
    [ValidateModelState]
    [SwaggerOperation("UploadUnspsc")]
    [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Upload successful")]
    [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
    [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
    public async Task<IActionResult> Upload([Required] IFormFile file)
    {
        _logger.LogInfo("Starting UNSPSC upload process.");

        var recordsInserted = await _mediator.Send(new UploadUnspscCommand(file));

        _logger.LogInfo($"UNSPSC upload completed. Records inserted: {recordsInserted}");

        return Ok(new
        {
            Message = "Upload successful.",
            RecordsInserted = recordsInserted
        });
    }

    /// <summary>
    /// Returns Segment and Family hierarchy.
    /// </summary>
    [HttpGet]
    [Route("api/v1/masterdata/unspsc")]
    [ValidateModelState]
    [SwaggerOperation("GetUnspsc")]
    [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Data retrieved successfully")]
    [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
    public async Task<IActionResult> Get(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10)
    {
        _logger.LogInfo($"Received request to get UNSPSC data: PageIndex={pageIndex}, PageSize={pageSize}");

        var result = await _mediator.Send(
            new GetUnspscQuery(pageIndex, pageSize));

        _logger.LogInfo($"Retrieved {result.Count} segments.");

        return Ok(result);
    }
    /// <summary>
    /// Returns Classes and Commodities for a Segment and Family.
    /// </summary>
    [HttpGet]
    [Route("api/v1/masterdata/unspsc/class-commodity")]
    [ValidateModelState]
    [SwaggerOperation("GetUnspscByVersion")]
    [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Data retrieved successfully")]
    [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
    public async Task<IActionResult> GetByVersion(
        [FromQuery] long segment,
        [FromQuery] long family,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10)
    {
        _logger.LogInfo($"Received request. Segment={segment}, Family={family}");

        var result = await _mediator.Send(
            new GetUnspscByVersionQuery(
                segment,
                family,
                pageIndex,
                pageSize));

        _logger.LogInfo($"Retrieved {result.Count} classes.");

        return Ok(result);
    }
}