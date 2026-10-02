using MediatR;
using Microsoft.AspNetCore.Mvc;
using Operations.Application.Features.Commands.RunOrganizationIntegrations;
using Operations.Application.Features.Commands.SendIntegrationRequest;
using Operations.Application.Features.Queries.GetLiveStock;
using Operations.Application.Features.Queries.ResolveIntegration;
using Operations.Domain.Dtos;
using SharedKernel.Attributes;
using SharedKernel.Controllers;
using SharedKernel.LoggerServices;
using SharedKernel.Security;

namespace Operations.API.Controllers
{
    /// <summary>
    /// The integration calls the other services make on their own behalf: the Buyer service sending a
    /// purchase order or asking for stock in hand, the Supplier service asking for a stock refresh.
    /// They carry the internal key, not a user's token, and are not offered to users.
    /// </summary>
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class InternalIntegrationController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;
        private readonly IConfiguration _configuration;

        public InternalIntegrationController(IMediator mediator, ILoggerManager logger, IConfiguration configuration)
        {
            _mediator = mediator;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost]
        [Route("api/v1/operations/internal/integrations/resolve")]
        [ValidateModelState]
        public async Task<IActionResult> ResolveIntegration([FromBody] ResolveIntegrationRequestDto request)
        {
            InternalServiceKey.Require(Request, _configuration);
            _logger.LogDebug($"Resolving integration. OrganizationId: {request.OrganizationId}, ProcessType: {request.ProcessType}");
            ResolvedIntegrationDto result = await _mediator.Send(new ResolveIntegrationQuery { Request = request });
            _logger.LogDebug($"Integration resolved. Configured: {result.Configured}");
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/operations/internal/integrations/send")]
        [ValidateModelState]
        public async Task<IActionResult> SendIntegrationRequest([FromBody] SendIntegrationRequestDto request)
        {
            InternalServiceKey.Require(Request, _configuration);
            _logger.LogDebug($"Sending document to integration. ConfigurationId: {request.ConfigurationId}");
            SendIntegrationResponseDto result = await _mediator.Send(new SendIntegrationRequestCommand { Request = request });
            _logger.LogDebug($"Document sent to integration. ConfigurationId: {request.ConfigurationId}, StatusCode: {result.StatusCode}");
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/operations/internal/integrations/stock")]
        [ValidateModelState]
        public async Task<IActionResult> GetLiveStock([FromBody] LiveStockRequestDto request)
        {
            InternalServiceKey.Require(Request, _configuration);
            _logger.LogDebug($"Reading live stock. OrganizationId: {request.OrganizationId}");
            LiveStockResponseDto result = await _mediator.Send(new GetLiveStockQuery { OrganizationId = request.OrganizationId });
            _logger.LogDebug($"Live stock read. Items: {result.Items.Count}");
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/operations/internal/integrations/run")]
        [ValidateModelState]
        public async Task<IActionResult> RunOrganizationIntegrations([FromBody] RunOrganizationIntegrationsRequestDto request)
        {
            InternalServiceKey.Require(Request, _configuration);
            _logger.LogDebug($"Running organization integrations. ProcessType: {request.ProcessType}, Organizations: {request.OrganizationIds.Count}");
            RunOrganizationIntegrationsResultDto result = await _mediator.Send(new RunOrganizationIntegrationsCommand { Request = request });
            _logger.LogDebug($"Organization integrations run. Succeeded: {result.Succeeded}, Failed: {result.Failed}");
            return Ok(result);
        }
    }
}
