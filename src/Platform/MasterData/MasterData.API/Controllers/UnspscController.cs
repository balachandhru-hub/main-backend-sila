using MediatR;
using Microsoft.AspNetCore.Mvc;
using MasterData.Application.Features.Unspsc.Commands;
using MasterData.Application.Features.Unspsc.Queries;
using SharedKernel.LoggerServices;
using System.ComponentModel.DataAnnotations;

namespace MasterData.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UnspscController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILoggerManager _logger;

    public UnspscController(IMediator mediator, ILoggerManager logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([Required] IFormFile file)
    {
        _logger.LogInfo($"Starting UNSPSC upload process.");
        var recordsInserted = await _mediator.Send(
            new UploadUnspscCommand(file));
_logger.LogInfo($"UNSPSC upload process completed. Records inserted: {recordsInserted}");
        return Ok(new
        {
            Message = "Upload successful.",
            RecordsInserted = recordsInserted
        });
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10)
    {
      _logger.LogInfo($"Received request to get UNSPSC data: PageIndex:{pageIndex}, PageSize:{pageSize}");
        var result = await _mediator.Send(
            new GetUnspscQuery(pageIndex, pageSize));
_logger.LogInfo($"Retrieved {result.Count} UNSPSC records.");
        return Ok(result);
    }
    [HttpGet("by-version")]
public async Task<IActionResult> GetByVersion(
    [FromQuery] string version,
    [FromQuery] int pageIndex = 1,
    [FromQuery] int pageSize = 10)
{
    _logger.LogInfo($"Received request to get UNSPSC data by version: Version={version}, PageIndex={pageIndex}, PageSize={pageSize}");
    var result = await _mediator.Send(
        new GetUnspscByVersionQuery(
            version,
            pageIndex,
            pageSize));
        _logger.LogInfo($"Retrieved {result.Count} UNSPSC records for version {version}.");
    return Ok(result);
}
}