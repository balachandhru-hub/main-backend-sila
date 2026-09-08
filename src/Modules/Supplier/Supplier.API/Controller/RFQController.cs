using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

using SharedKernel.Attributes;
using SharedKernel.Controllers;
using SharedKernel.Dto;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

using Swashbuckle.AspNetCore.Annotations;

using Supplier.API.Hubs;

using Supplier.Application.Features.Commands.CreateSupplierRFQ;
using Supplier.Application.Features.Commands.UpdateSupplierQuotation;
using Supplier.Application.Features.Commands.SupplierAnswers;

using Supplier.Application.Features.Queries.GetSupplier;
using Supplier.Application.Features.Queries.GetAllSupplierRFQ;
using Supplier.Application.Features.Queries.GetSupplierAllRFQ;
using Supplier.Application.Features.Queries.GetSupplierQuotation;
using Supplier.Application.Features.Queries.SupplierAnswers;
using Supplier.Application.Features.Queries.GetSupplierQuotationBySupplierId;

using Supplier.Domain.Dto;
using Supplier.Application.Features.Commands.RFQ;
using Supplier.Domain.Common;

namespace Supplier.API.Controllers
{
    [ApiController]
    public class RFQController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;
        private readonly IHubContext<NotificationHub> _hubContext;

        public RFQController(
            IMediator mediator,
            ILoggerManager logger,
            IHubContext<NotificationHub> hubContext)
        {
            _mediator = mediator;
            _logger = logger;
            _hubContext = hubContext;
        }
        [HttpPost]
        [Route("/api/v1/supplier/internal-rfq")]
        [ApiAuthorization(Name = "CREATE_SUPPLIER_RFQ")]
        [ValidateModelState]
        [SwaggerOperation("CreateSupplierRFQ")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Supplier RFQ created successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> CreateSupplierRFQ(
            [FromBody] CreateSupplierRFQDto dto)
        {
            Guid supplierRFQId = await _mediator.Send(
                new CreateSupplierRFQCommand(dto));

            await _hubContext.Clients.All.SendAsync(
     "SupplierRFQCreated",
     new
     {
         SupplierId = dto.SupplierId,
         BuyerId = dto.BuyerId,
         RFQId = supplierRFQId,
         RFQNumber = dto.RFQNumber,
         Message = "A new RFQ has been assigned to you."
     });

            return Ok(new SuccessResponseDto
            {
                StatusCode = 200,
                Message = "Success",
                Description = "Supplier RFQ created successfully.",
                Id = supplierRFQId.ToString()
            });
        }

        [HttpPost]
        [Route("api/v1/supplier/rfq-supplier")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_SUPPLIER_LIST")]
        [SwaggerOperation("GetSupplierList")]
        [SwaggerResponse(200, type: typeof(List<SupplierListDto>), description: "Supplier list fetched successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Supplier not found")]
        public async Task<IActionResult> GetSupplierList(
     [FromBody] GetSupplierListDto supplierListDto,
     CancellationToken cancellationToken)
        {
            var query = new GetSupplierListQuery(supplierListDto);

            var result = await _mediator.Send(query, cancellationToken);

            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/supplier/rfq-master-data")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_SUPPLIER_RFQ_MASTER_DATA")]
        [SwaggerOperation("GetSupplierRFQList")]
        [SwaggerResponse(200, type: typeof(List<SupplierRFQListDto>), description: "Success")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetRFQList(
     [FromBody] GetSupplierRFQListQuery query)
        {
            query.OrganizationId = GetOrganizationId();
            query.UserId = GetUserId();
            query.RoleId = GetRoleId();

            var result = await _mediator.Send(query);

            return Ok(result);
        }
        [HttpGet]
        [Route("api/v1/supplier/rfq-by-id")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_SUPPLIER_RFQ_BY_ID")]
        [SwaggerOperation("GetSupplierRFQById")]
        [SwaggerResponse(200, type: typeof(GetRFQByIdDto), description: "Success")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetRFQById([FromQuery] Guid rfqId)
        {
            var result = await _mediator.Send(new GetSupplierRFQByIdQuery
            {
                RFQId = rfqId,
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                RoleId = GetRoleId()
            });

            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/supplier/external-rfq/{rfqId}")]
        [ApiSessionAuthorization]
        [SwaggerOperation("GetExternalSupplierRFQById")]
        [SwaggerResponse(200, type: typeof(GetRFQByIdDto), description: "Success")]
        [SwaggerResponse(401, type: typeof(ErrorResponseDto), description: "Unauthorized")]
        [SwaggerResponse(403, type: typeof(ErrorResponseDto), description: "Session token expired")]
        public async Task<IActionResult> GetExternalRFQById(Guid rfqId)
        {
            var result = await _mediator.Send(new GetExternalSupplierRFQByIdQuery
            {
                RFQId = rfqId,
                SupplierId = GetSupplierId()
            });

            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/supplier/quotation-rfq-by-id")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_SUPPLIER_QUOTATION_RFQ_BY_ID")]
        [SwaggerOperation("GetSupplierQuotationRFQById")]
        [SwaggerResponse(200, type: typeof(GetAllSupplierQuotationDto), description: "Success")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetQuotationRFQById([FromQuery] Guid rfqId)
        {
            var result = await _mediator.Send(new GetSupplierQuotationByBuyerRFQIdQuery
            {
                RFQId = rfqId
            });

            return Ok(result);
        }
        [HttpGet]
        [Route("api/v1/supplier/quotation/by-supplier-id")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_SUPPLIER_QUOTATION_BY_SUPPLIER_ID")]
        [SwaggerOperation("GetSupplierQuotationBySupplierId")]
        [SwaggerResponse(200, type: typeof(GetAllSupplierQuotationBySupplierIdDto), description: "Success")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetQuotationBySupplierId([FromQuery] Guid rfqId)
        {
            var organizationId = GetOrganizationId();

            _logger.LogInfo(
                $"Fetching Supplier Quotation for RFQId: {rfqId} and OrganizationId: {organizationId}");

            var result = await _mediator.Send(
                new GetSupplierQuotationBySupplierIdQuery(
                    rfqId,
                    organizationId));

            return Ok(result);
        }
        [HttpPut]
        [Route("api/v1/supplier/rfq-answer")]
        [ValidateModelState]
        [ApiAuthorization(Name = "SAVE_SUPPLIER_RFQ_ANSWER")]
        [SwaggerOperation("SaveSupplierRFQAnswer")]
        [SwaggerResponse(200, type: typeof(bool), description: "Supplier answers saved successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> SaveSupplierRFQAnswer(
            [FromBody] SaveSupplierRFQAnswerDto request)
        {
            _logger.LogDebug(
                $"Saving supplier answers for SupplierRFQ : {request.SupplierRFQId}");

            var result = await _mediator.Send(
                new SaveSupplierRFQAnswerCommand(request));

            _logger.LogDebug(
                $"Supplier answers saved successfully for SupplierRFQ : {request.SupplierRFQId}");

            return Ok(result);
        }
        [HttpGet]
        [Route("api/v1/supplier/internal-rfq-answer/{buyerRFQId}")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_SUPPLIER_RFQ_ANSWER")]
        [SwaggerOperation("GetSupplierRFQAnswer")]
        [SwaggerResponse(200, type: typeof(SupplierRFQAnswerResponseDto), description: "Supplier answers fetched successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetSupplierRFQAnswer(Guid buyerRFQId)
        {
            _logger.LogDebug($"Fetching supplier answers for SupplierRFQ : {buyerRFQId}");

            var result = await _mediator.Send(
                new GetSupplierRFQAnswerQuery(buyerRFQId));

            _logger.LogDebug($"Supplier answers fetched successfully for SupplierRFQ : {buyerRFQId}");

            return Ok(result);
        }


        [HttpPut]
        [Route("api/v1/supplier/rfq-status")]
        [ValidateModelState]
        [ApiAuthorization(Name = "UPDATE-RFQ-STATUS")]
        [SwaggerOperation("UpdateRFQStatus")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "RFQ status updated successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "RFQ not found")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> UpdateSupplierRFQStatus([FromBody] UpdateSupplierRFQStatusDto dto)
        {
            var result = await _mediator.Send(
                new UpdateSupplierRFQStatusCommand(dto));

            return Ok(new
            {
                success = true,
                message = "Supplier RFQ status updated successfully."
            });
        }

    }
}