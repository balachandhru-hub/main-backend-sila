using Buyer.Application.Features.Commands.CreateErpIntegration;
using Buyer.Application.Features.Commands.UpdateErpIntegration;
using Buyer.Application.Features.Queries.GetErpIntegration;
using Buyer.Domain.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Attributes;
using SharedKernel.Controllers;
using SharedKernel.Dto;
using SharedKernel.LoggerServices;
using Swashbuckle.AspNetCore.Annotations;

namespace Buyer.API.Controllers
{
    [ApiController]
    public class ErpIntegrationController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public ErpIntegrationController(IMediator mediator, ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet]
        [Route("api/v1/buyer/erp-integration")]
        [ApiAuthorization(Name = "MANAGE_BUYER_ERP_INTEGRATION")]
        [SwaggerOperation("GetBuyerErpIntegrations")]
        [SwaggerResponse(200, type: typeof(List<ErpIntegrationResponseDto>))]
        public async Task<IActionResult> Get()
        {
            _logger.LogDebug("Fetching ERP API configurations.");
            List<ErpIntegrationResponseDto> result = await _mediator.Send(new GetErpIntegrationQuery
            {
                OrganizationId = GetOrganizationId()
            });
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/erp-integration")]
        [ApiAuthorization(Name = "MANAGE_BUYER_ERP_INTEGRATION")]
        [SwaggerOperation("CreateBuyerErpIntegration")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> Create([FromBody] ErpIntegrationWriteDto request)
        {
            _logger.LogDebug($"Creating ERP API configuration. ApiName: {request.ApiName}, Process: {request.Process}, ErpType: {request.ErpType}");
            Guid id = await _mediator.Send(new CreateErpIntegrationCommand
            {
                OrganizationId = GetOrganizationId(),
                Request = request
            });
            _logger.LogDebug($"ERP API configuration created. ConfigurationId: {id}");
            return Ok(new SuccessResponseDto
            {
                Id = id.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "API configuration created."
            });
        }

        [HttpPut]
        [Route("api/v1/buyer/erp-integration/{configurationId}")]
        [ApiAuthorization(Name = "MANAGE_BUYER_ERP_INTEGRATION")]
        [SwaggerOperation("UpdateBuyerErpIntegration")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> Update([FromRoute] Guid configurationId, [FromBody] ErpIntegrationWriteDto request)
        {
            _logger.LogDebug($"Updating ERP API configuration. ConfigurationId: {configurationId}");
            Guid id = await _mediator.Send(new UpdateErpIntegrationCommand
            {
                OrganizationId = GetOrganizationId(),
                ConfigurationId = configurationId,
                Request = request
            });
            _logger.LogDebug($"ERP API configuration updated. ConfigurationId: {id}");
            return Ok(new SuccessResponseDto
            {
                Id = id.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "API configuration updated."
            });
        }
    }
}
