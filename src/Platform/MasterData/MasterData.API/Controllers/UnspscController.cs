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
    [ApiAuthorization(Name = "UPLOAD_UNSPSC")]
    [SwaggerOperation("UploadUnspsc")]
    [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Upload successful")]
    [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
    [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
    public async Task<IActionResult> Upload([Required] IFormFile file)
    {
        _logger.LogDebug("Starting UNSPSC upload process.");

        var recordsInserted = await _mediator.Send(new UploadUnspscCommand(file));

        _logger.LogDebug($"UNSPSC upload completed. Records inserted: {recordsInserted}");

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
    [ApiAuthorization(Name = "GET_UNSPSC")]
    [SwaggerOperation("GetUnspsc")]
    [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Data retrieved successfully")]
    [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
    public async Task<IActionResult> Get(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10)
    {
        _logger.LogDebug($"Received request to get UNSPSC data: PageIndex={pageIndex}, PageSize={pageSize}");

        var result = await _mediator.Send(
            new GetUnspscQuery(pageIndex, pageSize));

        _logger.LogDebug($"Retrieved {result.Count} segments.");

        return Ok(result);
    }
    /// <summary>
    /// Returns Classes and Commodities for a Segment and Family.
    /// </summary>
    [HttpGet]
    [Route("api/v1/masterdata/unspsc/class-commodity")]
    [ValidateModelState]
    [ApiAuthorization(Name = "GET_CLASS_COMMODITY")]
    [SwaggerOperation("GetUnspscByVersion")]
    [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Data retrieved successfully")]
    [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
    public async Task<IActionResult> GetByVersion(
        [FromQuery] long segment,
        [FromQuery] long family,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10)
    {
        _logger.LogDebug($"Received request. Segment={segment}, Family={family}");

        var result = await _mediator.Send(
            new GetUnspscByVersionQuery(
                segment,
                family,
                pageIndex,
                pageSize));

        _logger.LogDebug($"Retrieved {result.Count} classes.");

        return Ok(result);
    }

    /// <summary>
    /// Returns distinct UNSPSC Segments.
    /// </summary>
    [HttpGet]
    [Route("api/v1/masterdata/unspsc/segment")]
    [ValidateModelState]
    [ApiAuthorization(Name ="GET_SEGMENT")]
    [SwaggerOperation("GetUnspscSegment")]
    [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Data retrieved successfully")]
    [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
    public async Task<IActionResult> GetSegment(
    [FromQuery] int pageIndex = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string? searchTerm = null)
    {
        _logger.LogDebug($"Received request to get UNSPSC segments: PageIndex={pageIndex}, PageSize={pageSize}");

        var result = await _mediator.Send(
    new GetUnspscsegmentQuery(
        pageIndex,
        pageSize,
        searchTerm));

        _logger.LogDebug($"Retrieved {result.Count} segments.");

        return Ok(result);
    }

    /// <summary>
    /// Returns distinct Families for a Segment.
    /// </summary>
    [HttpGet]
    [Route("api/v1/masterdata/unspsc/family")]
    [ValidateModelState]
    [ApiAuthorization(Name ="GET_FAMILY")]
    [SwaggerOperation("GetUnspscFamily")]
    [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Data retrieved successfully")]
    [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
    public async Task<IActionResult> GetFamily(
        [FromQuery] long segment,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10)
    {
        _logger.LogDebug($"Received request to get families. Segment={segment}, PageIndex={pageIndex}, PageSize={pageSize}");

        var result = await _mediator.Send(
            new GetUnspscFamilyQuery(
                segment,
                pageIndex,
                pageSize));

        _logger.LogDebug($"Retrieved {result.Count} families.");

        return Ok(result);
    }

    /// <summary>
    /// Returns Classes for  Family.
    /// </summary>
    [HttpGet]
    [Route("api/v1/masterdata/unspsc/class")]
    [ValidateModelState]
    [ApiAuthorization(Name = "GET_CLASS")]
    [SwaggerOperation("GetUnspscClass")]
    [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Classes retrieved successfully")]
    [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
    public async Task<IActionResult> GetClass(
       
        [FromQuery] long family,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10)
    {
        _logger.LogDebug(
            $"Received request to get UNSPSC classes:Family={family}, PageIndex={pageIndex}, PageSize={pageSize}");

        var result = await _mediator.Send(
            new GetUnspscClassQuery(
                
                family,
                pageIndex,
                pageSize));

        _logger.LogDebug($"Retrieved {result.Count} classes.");

        return Ok(result);
    }
    /// <summary>
    /// Returns Commodities for Class.
    /// </summary>
    [HttpGet]
    [Route("api/v1/masterdata/unspsc/commodity")]
    [ValidateModelState]
    [ApiAuthorization(Name = "GET_COMMODITY")]
    [SwaggerOperation("GetUnspscCommodity")]
    [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Commodities retrieved successfully")]
    [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
    public async Task<IActionResult> GetCommodity(
    
        [FromQuery] long @class,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10)
    {
        _logger.LogDebug(
            $"Received request to get UNSPSC commodities:  Class={@class}, PageIndex={pageIndex}, PageSize={pageSize}");

        var result = await _mediator.Send(
            new GetUnspscCommodityQuery(
               
                @class,
                pageIndex,
                pageSize));

        _logger.LogDebug($"Retrieved {result.Count} commodities.");

        return Ok(result);
    }

}