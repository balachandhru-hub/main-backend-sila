using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Attributes;
using SharedKernel.Controllers;
using SharedKernel.Dto;
using Supplier.API.Attribute;
using Supplier.Application.Features.SupplierErp;
using Supplier.Domain.Dto;
using Swashbuckle.AspNetCore.Annotations;

namespace Supplier.API.Controller
{
    [ApiController]
    public class SupplierErpController : BaseController
    {
        private readonly IMediator _mediator;

        public SupplierErpController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        [Route("api/v1/supplier/internal/purchase-orders")]
        [InternalApiKeyAuthorization]
        [SwaggerOperation("CreateSupplierPurchaseOrder")]
        public async Task<IActionResult> CreatePurchaseOrder([FromBody] SupplierPurchaseOrderRequestDto request)
        {
            SupplierPurchaseOrderResponseDto result = await _mediator.Send(new CreateSupplierPurchaseOrderCommand(request));
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/supplier/erp-integration")]
        [ApiAuthorization(Name = "GET_SUPPLIER_ERP_INTEGRATION")]
        [SwaggerOperation("GetSupplierErpIntegration")]
        public async Task<IActionResult> GetConfiguration()
        {
            SupplierErpResponseDto? result = await _mediator.Send(new GetSupplierErpConfigurationQuery(GetOrganizationId()));
            return Ok(result);
        }

        [HttpPut]
        [Route("api/v1/supplier/erp-integration")]
        [ApiAuthorization(Name = "MANAGE_SUPPLIER_ERP_INTEGRATION")]
        [SwaggerOperation("SaveSupplierErpIntegration")]
        public async Task<IActionResult> SaveConfiguration([FromBody] SupplierErpWriteDto request)
        {
            Guid id = await _mediator.Send(new SaveSupplierErpConfigurationCommand(GetOrganizationId(), request));
            return Ok(new SuccessResponseDto
            {
                Id = id.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "Supplier ERP configuration saved."
            });
        }
    }
}
