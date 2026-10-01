using Operations.Application.Features.Commands.CreateExtractionAgent;
using Operations.Application.Features.Commands.TestExtractionAgent;
using Operations.Application.Features.Commands.UpdateExtractionAgent;
using Operations.Application.Features.Commands.UpsertInvoiceOcrConfiguration;
using Operations.Application.Features.Queries.GetExtractionAgents;
using Operations.Application.Features.Queries.GetInvoiceOcrConfiguration;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Operations.Domain.Dtos;
using Operations.Domain.Enums;
using SharedKernel.Attributes;
using SharedKernel.Controllers;
using SharedKernel.Dto;
using SharedKernel.LoggerServices;
using Swashbuckle.AspNetCore.Annotations;

namespace Operations.API.Controllers
{
    /// <summary>
    /// Invoice OCR policy and external extraction agents of the organization.
    /// </summary>
    [ApiController]
    public class ConfigurationController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public ConfigurationController(IMediator mediator, ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet]
        [Route("api/v1/operations/configuration/invoice-ocr")]
        [ApiAuthorization(Name = "OPERATIONS_VIEW_OCR_CONFIG")]
        [SwaggerOperation("GetInvoiceOcrConfiguration")]
        [SwaggerResponse(200, type: typeof(InvoiceOcrConfigurationResponseDto))]
        public async Task<IActionResult> GetInvoiceOcrConfiguration()
        {
            _logger.LogDebug($"Fetching invoice OCR configuration.");
            InvoiceOcrConfigurationResponseDto result = await _mediator.Send(new GetInvoiceOcrConfigurationQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId()
            });
            _logger.LogDebug($"Invoice OCR configuration fetched. Version: {result.Version}");
            return Ok(result);
        }

        [HttpPut]
        [Route("api/v1/operations/configuration/invoice-ocr")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_OCR_CONFIG")]
        [SwaggerOperation("UpdateInvoiceOcrConfiguration")]
        [SwaggerResponse(200, type: typeof(InvoiceOcrConfigurationResponseDto))]
        public async Task<IActionResult> UpdateInvoiceOcrConfiguration([FromBody] UpsertInvoiceOcrConfigurationRequestDto request)
        {
            _logger.LogDebug($"Saving invoice OCR configuration. BackendProvider: {request.BackendProvider}");
            InvoiceOcrConfigurationResponseDto result = await _mediator.Send(new UpsertInvoiceOcrConfigurationCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                Request = request
            });
            _logger.LogDebug($"Invoice OCR configuration saved. Version: {result.Version}");
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/operations/extraction-agents")]
        [ApiAuthorization(Name = "OPERATIONS_VIEW_OCR_CONFIG")]
        [SwaggerOperation("GetExtractionAgents")]
        [SwaggerResponse(200, type: typeof(List<ExtractionAgentConfigResponseDto>))]
        public async Task<IActionResult> GetExtractionAgents()
        {
            _logger.LogDebug($"Fetching extraction agents.");
            List<ExtractionAgentConfigResponseDto> result = await _mediator.Send(new GetExtractionAgentsQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId()
            });
            _logger.LogDebug($"Extraction agents fetched. Count: {result.Count}");
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/operations/extraction-agents")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_OCR_CONFIG")]
        [SwaggerOperation("CreateExtractionAgent")]
        [SwaggerResponse(200, type: typeof(ExtractionAgentConfigResponseDto))]
        public async Task<IActionResult> CreateExtractionAgent([FromBody] UpsertExtractionAgentConfigRequestDto request)
        {
            _logger.LogDebug($"Creating extraction agent. Name: {request.Name}, ProviderType: {request.ProviderType}");
            ExtractionAgentConfigResponseDto result = await _mediator.Send(new CreateExtractionAgentCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                Request = request
            });
            _logger.LogDebug($"Extraction agent created. AgentId: {result.Id}");
            return Ok(result);
        }

        [HttpPut]
        [Route("api/v1/operations/extraction-agents/{agentId:guid}")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_OCR_CONFIG")]
        [SwaggerOperation("UpdateExtractionAgent")]
        [SwaggerResponse(200, type: typeof(ExtractionAgentConfigResponseDto))]
        public async Task<IActionResult> UpdateExtractionAgent([FromRoute] Guid agentId, [FromBody] UpsertExtractionAgentConfigRequestDto request)
        {
            _logger.LogDebug($"Updating extraction agent. AgentId: {agentId}");
            ExtractionAgentConfigResponseDto result = await _mediator.Send(new UpdateExtractionAgentCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                AgentId = agentId,
                Request = request
            });
            _logger.LogDebug($"Extraction agent updated. AgentId: {result.Id}");
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/operations/extraction-agents/{agentId:guid}/test")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_OCR_CONFIG")]
        [SwaggerOperation("TestExtractionAgent")]
        [SwaggerResponse(200, type: typeof(ExtractionAgentTestResponseDto))]
        public async Task<IActionResult> TestExtractionAgent([FromRoute] Guid agentId)
        {
            _logger.LogDebug($"Testing extraction agent. AgentId: {agentId}");
            ExtractionAgentTestResponseDto result = await _mediator.Send(new TestExtractionAgentCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                AgentId = agentId
            });
            _logger.LogDebug($"Extraction agent tested. AgentId: {agentId}, Success: {result.Success}");
            return Ok(result);
        }
    }
}
