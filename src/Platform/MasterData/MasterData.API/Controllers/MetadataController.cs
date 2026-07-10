using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using SharedKernel.LoggerServices;
using SharedKernel.Dto;
using SharedKernel.ExceptionHandler;
using SharedKernel.Attributes;
using MasterData.Application.Features.Metadata.Queries;
using MasterData.Domain.Dto;


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
    [Route("api/v1/masterdata/metadata/by-keys")]
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
}