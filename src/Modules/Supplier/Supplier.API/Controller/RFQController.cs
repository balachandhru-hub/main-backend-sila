using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

using SharedKernel.Attributes;
using SharedKernel.Contracts;
using SharedKernel.Controllers;
using SharedKernel.Dto;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

using Swashbuckle.AspNetCore.Annotations;

using Supplier.API.Hubs;

using Supplier.Application.Features.Commands.CreateSupplierRFQ;
using Supplier.Application.Features.Commands.RFQAttachment;
using Supplier.Application.Features.Commands.UpdateSupplierQuotation;
using Supplier.Application.Features.Queries.GetRFQTermsCondition;
using Supplier.Application.Features.Commands.SupplierAnswers;

using Supplier.Application.Features.Queries.GetSupplier;
using Supplier.Application.Features.Queries.GetAllSupplierRFQ;
using Supplier.Application.Features.Queries.GetSupplierAllRFQ;
using Supplier.Application.Features.Queries.GetSupplierQuotation;
using Supplier.Application.Features.Queries.SupplierAnswers;
using Supplier.Application.Features.Queries.GetSupplierQuotationBySupplierId;

using Supplier.Domain.Dto;
using Supplier.Application.Features.Commands.RFQ;
using Supplier.Application.Features.Commands.SaveSupplierRFQAward;
using Supplier.Domain.Common;

namespace Supplier.API.Controllers
{
    [ApiController]
    public class RFQController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ISessionTokenValidator _sessionTokenValidator;

        public RFQController(
            IMediator mediator,
            ILoggerManager logger,
            IHubContext<NotificationHub> hubContext,
            ISessionTokenValidator sessionTokenValidator)
        {
            _mediator = mediator;
            _logger = logger;
            _hubContext = hubContext;
            _sessionTokenValidator = sessionTokenValidator;
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
        [Route("api/v1/supplier/supplier-terms-condition")]
        [ApiAuthorization(Name = "UPLOAD_SUPPLIER_RFQ_TERMS_CONDITION")]
        [ValidateModelState]
        [SwaggerOperation("UploadSupplierRFQTermsCondition")]
        [SwaggerResponse(201, type: typeof(SuccessResponseDto), description: "Terms and Condition document uploaded successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "RFQ not found")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> UploadSupplierRFQTermsCondition(
            [FromQuery] Guid rfqId,
            [FromQuery] bool termsAndCondition,
            [FromBody] AssetUploadDto? document)
        {
            var organizationId = GetOrganizationId();

            var result = await _mediator.Send(new UploadRFQTermsConditionCommand
            {
                RFQId = rfqId,
                OrganizationId = organizationId,
                TermsAndCondition = termsAndCondition,
                Document = document
            });

            return StatusCode(201, new SuccessResponseDto
            {
                StatusCode = 201,
                Message = "Success",
                Description = result.HasValue
                    ? "Terms and Condition document uploaded successfully."
                    : "Terms and Condition flag updated. No document uploaded.",
                Id = result?.ToString()
            });
        }

        [HttpGet]
        [Route("api/v1/supplier/internal/rfq-terms-condition")]
        [ApiAuthorization(Name = "GET_SUPPLIER_RFQ_TERMS_CONDITION")]
        [ValidateModelState]
        [SwaggerOperation("GetSupplierRFQTermsCondition")]
        [SwaggerResponse(200, type: typeof(List<RFQTermsConditionDto>), description: "Success")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "RFQ not found")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetSupplierRFQTermsCondition(
            [FromQuery] Guid rfqId)
        {
            var result = await _mediator.Send(new GetRFQTermsConditionQuery
            {
                RFQId = rfqId
            });

            return Ok(result);
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
        [Route("api/v1/supplier/internal-session-token/validate")]
        [ValidateModelState]
        [SwaggerOperation("ValidateExternalSessionToken")]
        [SwaggerResponse(200, description: "Session token is valid")]
        [SwaggerResponse(401, type: typeof(ErrorResponseDto), description: "Unauthorized")]
        [SwaggerResponse(403, type: typeof(ErrorResponseDto), description: "Session token expired")]
        public async Task<IActionResult> ValidateExternalSessionToken(
            [FromQuery] string sessionToken,
            [FromQuery] Guid rfqId,
            CancellationToken cancellationToken)
        {
            SessionTokenValidationResult result = await _sessionTokenValidator.ValidateAsync(
                sessionToken,
                rfqId,
                cancellationToken);

            return Ok(new
            {
                RFQId = result.RFQId,
                ExternalSupplierId = result.SupplierId,
                ExternalSupplierRFQId = result.SupplierRFQId
            });
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

        [HttpPost]
        [Route("api/v1/supplier/internal/rfq-award")]
        [ValidateModelState]
        [SwaggerOperation("SaveSupplierRFQAward")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Supplier RFQ award saved successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Supplier RFQ not found")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> SaveSupplierRFQAward(
            [FromBody] SaveSupplierRFQAwardDto dto)
        {
            _logger.LogDebug(
                $"Saving supplier RFQ award for BuyerRFQId: {dto.BuyerRFQId}");

            var result = await _mediator.Send(
                new SaveSupplierRFQAwardCommand(dto));

            return Ok(new
            {
                success = true,
                message = "Supplier RFQ award saved successfully."
            });
        }

    }
}