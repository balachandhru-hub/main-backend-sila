using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using SharedKernel.LoggerServices;
using SharedKernel.Dto;
using SharedKernel.ExceptionHandler;
using SharedKernel.Attributes;
using MasterData.Application.Features.Metadata.Queries;
using MasterData.Domain.Dto;
using MasterData.Application.Features.Metadata.Queries.GetRefTermKeyById;


namespace MasterData.API.Controllers;

[ApiController]
public class MetadataController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILoggerManager _logger;

    public MetadataController(
        IMediator mediator,
        ILoggerManager logger)
    {
        _mediator = mediator;
        _logger = logger;
    }


    /// <summary>
    /// Get metadata reference list.
    /// </summary>
    [HttpPost]
    [Route("api/v1/masterdata/metadata/reference-list")]
    [ValidateModelState]
    [SwaggerOperation("GetReferenceList")]
    [SwaggerResponse(200, "Fetched Metadata", typeof(List<MetadataDto>))]
    [SwaggerResponse(400, "Bad Request", typeof(ErrorResponseDto))]
    [SwaggerResponse(404, "Not Found", typeof(ErrorResponseDto))]
    public async Task<IActionResult> GetReferenceList(
        [FromBody] List<string> type)
    {
        _logger.LogInfo("Fetching metadata reference list");

        var result = await _mediator.Send(
            new GetMetadataByTypeQuery(type));

        _logger.LogInfo("Metadata reference list fetched");

        return Ok(result);
    }
    /// <summary>
    /// Get metadata by keys.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    [Route("api/v1/masterdata/metadata/key")]
    [ValidateModelState]
    [SwaggerOperation("GetMetadataByKeys")]
    [SwaggerResponse(200, "Fetched Metadata", typeof(List<GetMetadataByKeysRequestDto>))]
    [SwaggerResponse(400, "Bad Request", typeof(ErrorResponseDto))]
    [SwaggerResponse(404, "Not Found", typeof(ErrorResponseDto))]
    public async Task<IActionResult> GetMetadataByKeys(
            [FromBody] GetMetadataByKeysRequestDto request)
    {
        _logger.LogInfo("Fetching metadata by keys");

        var result = await _mediator.Send(
            new GetMetadataByKeysQuery(request.Type, request.Keys));

        return Ok(result);
    }

    [HttpGet]
    [Route("api/v1/masterdata/metadata/{id}")]
    [ValidateModelState]
    [SwaggerOperation("GetRefTermKeyById")]
    [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Reference term key retrieved successfully")]
    [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Reference term not found")]
    [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
    public async Task<IActionResult> GetRefTermKeyById(Guid id)
    {
        _logger.LogInfo($"Fetching reference term key for Id: {id}");

        var result = await _mediator.Send(new GetRefTermKeyByIdQuery(id));

        return Ok(result);
    }
}