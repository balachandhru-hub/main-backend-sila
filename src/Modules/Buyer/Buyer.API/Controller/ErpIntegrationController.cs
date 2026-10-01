using Buyer.Application.Features.Commands.SaveErpIntegration;
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
        [SwaggerOperation("GetBuyerErpIntegration")]
        [SwaggerResponse(200, type: typeof(ErpIntegrationResponseDto))]
        public async Task<IActionResult> Get()
        {
            _logger.LogDebug("Fetching ERP API configuration.");
            ErpIntegrationResponseDto? result = await _mediator.Send(new GetErpIntegrationQuery
            {
                OrganizationId = GetOrganizationId()
            });
            return Ok(result);
        }

        [HttpPut]
        [Route("api/v1/buyer/erp-integration")]
        [ApiAuthorization(Name = "MANAGE_BUYER_ERP_INTEGRATION")]
        [SwaggerOperation("SaveBuyerErpIntegration")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> Save([FromBody] ErpIntegrationWriteDto request)
        {
            _logger.LogDebug($"Saving ERP API configuration. ApiName: {request.ApiName}, ErpType: {request.ErpType}");
            Guid id = await _mediator.Send(new SaveErpIntegrationCommand
            {
                OrganizationId = GetOrganizationId(),
                Request = request
            });
            _logger.LogDebug($"ERP API configuration saved. ConfigurationId: {id}");
            return Ok(new SuccessResponseDto
            {
                Id = id.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "API configuration saved."
            });
        }
    }
}
