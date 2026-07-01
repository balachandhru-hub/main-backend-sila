using MediatR;
using Microsoft.AspNetCore.Mvc;
using MasterData.Application.Features.Unspsc.Commands;
using MasterData.Application.Features.Unspsc.Queries;

namespace MasterData.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UnspscController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UnspscController> _logger;

    public UnspscController(IMediator mediator, ILogger<UnspscController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] IFormFile file)
    {
        _logger.LogInformation("Received file upload request: {FileName}", file.FileName);
        var recordsInserted = await _mediator.Send(
            new UploadUnspscCommand(file));

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
        _logger.LogInformation("Received request to get UNSPSC data: PageIndex={PageIndex}, PageSize={PageSize}", pageIndex, pageSize);
        var result = await _mediator.Send(
            new GetUnspscQuery(pageIndex, pageSize));

        return Ok(result);
    }
    [HttpGet("by-version")]
public async Task<IActionResult> GetByVersion(
    [FromQuery] string version,
    [FromQuery] int pageIndex = 1,
    [FromQuery] int pageSize = 10)
{
    _logger.LogInformation("Received request to get UNSPSC data by version: Version={Version}, PageIndex={PageIndex}, PageSize={PageSize}", version, pageIndex, pageSize);
    var result = await _mediator.Send(
        new GetUnspscByVersionQuery(
            version,
            pageIndex,
            pageSize));

    return Ok(result);
}
}