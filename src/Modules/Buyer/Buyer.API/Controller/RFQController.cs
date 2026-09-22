using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using SharedKernel.LoggerServices;
using SharedKernel.Dto;
using SharedKernel.Attributes;
using Buyer.Domain.Dto;
using SharedKernel.Controllers;
using Buyer.Application.Features.Commands.CreateRFQ;
using Buyer.Domain.Dtos;
using Buyer.Application.Features.Queries.GetAllRFQ;
using Buyer.Application.Features.Queries.GetRFQAttachments;
using Buyer.Application.Features.Queries.GetRFQQuestions;
using Buyer.Application.Features.Queries.Invitation;
using Buyer.Application.Features.Commands.Invitation;
using Buyer.Application.Features.Queries.GetCostCenterById;
using Buyer.Application.Features.Commands.UpdateRFQ;
using Buyer.Application.Features.Queries.GetBidCompare;
using Buyer.Domain.Common;
using Buyer.Application.Features.Queries.CheckRFQUserAccess;
using Buyer.Application.Features.Commands.RFQAttachment;
using Buyer.Application.Features.Queries.GetRFQESign;
using Buyer.Application.Features.Queries.GetSupplierTermsConditionStatus;


namespace Buyer.API.Controllers
{
    [ApiController]
    public class RFQController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public RFQController(
            IMediator mediator,
            ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }
        [HttpPost]
        [Route("api/v1/buyer/createrfq")]
        [ValidateModelState]
        [ApiAuthorization(Name = "CREATE_RFQ")]
        [SwaggerOperation("CreateRFQ")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "RFQ created successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        public async Task<IActionResult> CreateRFQ(
            [FromBody] CreateRFQDto rfq)
        {
            Guid organizationId = GetOrganizationId();
            _logger.LogDebug("Creating RFQ");

            Guid rfqId = await _mediator.Send(
        new CreateRFQCommand(organizationId, rfq));

            _logger.LogDebug("RFQ created successfully");

            return Ok(new SuccessResponseDto
            {
                Id = rfqId.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "RFQ created successfully."
            });
        }
        [HttpPost]
        [Route("api/v1/buyer/rfq-master-data")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_RFQ_MASTER_DATA")]
        [SwaggerOperation("GetRFQList")]
        [SwaggerResponse(200, type: typeof(List<RFQListDto>), description: "Success")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetRFQList(
     [FromBody] GetRFQListQuery query)
        {
            query.OrganizationId = GetOrganizationId();
            query.UserId = GetUserId();
            query.RoleId = GetRoleId();

            var result = await _mediator.Send(query);

            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/rfq-by-id")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_RFQ_BY_ID")]
        [SwaggerOperation("GetRFQById")]
        [SwaggerResponse(200, type: typeof(GetRFQByIdDto), description: "Success")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetRFQById([FromQuery] Guid rfqId)
        {
            var result = await _mediator.Send(new GetRFQByIdQuery
            {
                RFQId = rfqId,
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                RoleId = GetRoleId()
            });

            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/bid-compare")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_BID_COMPARE")]
        [SwaggerOperation("GetBidCompare")]
        [SwaggerResponse(200, type: typeof(BidCompareResponseDto), description: "Success")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetBidCompare([FromQuery] Guid rfqId)
        {
            var result = await _mediator.Send(new GetBidCompareQuery
            {
                RFQId = rfqId,
               
            });

            return Ok(result);
        }
        [HttpPut]
        [Route("api/v1/buyer/supplier-terms-condition-status")]
        [ValidateModelState]
        [ApiAuthorization(Name = "UPDATE_SUPPLIER_TERMS_CONDITION_STATUS")]
        [SwaggerOperation("UpdateSupplierTermsConditionStatus")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Supplier Terms and Condition status updated successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "RFQ not found")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> UpdateSupplierTermsConditionStatus(
            [FromBody] UpdateSupplierTermsConditionStatusCommand command)
        {
            var result = await _mediator.Send(command);

            return Ok(new SuccessResponseDto
            {
                StatusCode = 200,
                Message = "Success",
                Description = "Supplier Terms and Condition status updated successfully.",
                Id = result.ToString()
            });
        }

        [HttpGet]
        [Route("api/v1/buyer/interal/supplier-terms-condition-status")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_SUPPLIER_TERMS_CONDITION_STATUS")]
        [SwaggerOperation("GetSupplierTermsConditionStatus")]
        [SwaggerResponse(200, type: typeof(SupplierTermsAndConditionStatusDto), description: "Success")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "RFQ not found")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetSupplierTermsConditionStatus(
            [FromQuery] Guid rfqId,
            [FromQuery] Guid supplierId)
        {
            var result = await _mediator.Send(new GetSupplierTermsConditionStatusQuery
            {
                RFQId = rfqId,
                SupplierId = supplierId
            });

            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/rfq-esign")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_RFQ_ESIGN")]
        [SwaggerOperation("GetRFQESign")]
        [SwaggerResponse(200, type: typeof(List<RFQESignDto>), description: "Success")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetRFQESign([FromQuery] Guid rfqId)
        {
            var result = await _mediator.Send(new GetRFQESignQuery
            {
                RFQId = rfqId
            });

            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/rfq-attachments")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_RFQ_ATTACHMENTS")]
        [SwaggerOperation("GetRFQAttachments")]
        [SwaggerResponse(200, type: typeof(GetRFQAttachmentsDto), description: "Success")]
        public async Task<IActionResult> GetRFQAttachments([FromQuery] Guid rfqId)
        {
            var result = await _mediator.Send(new GetRFQAttachmentsQuery
            {
                RFQId = rfqId
            });

            return Ok(result);
        }
        [HttpPost]
        [Route("api/v1/buyer/rfq-esign")]
        [ValidateModelState]
        [ApiAuthorization(Name = "UPLOAD_RFQ_ESIGN")]
        [SwaggerOperation("UploadRFQESign")]
        [SwaggerResponse(201, type: typeof(SuccessResponseDto), description: "E-Sign document uploaded successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "RFQ not found")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> UploadRFQESign(
            [FromQuery] Guid rfqId,
            [FromBody] AssetUploadDto document)
        {
            var result = await _mediator.Send(new UploadRFQESignCommand
            {
                RFQId = rfqId,
                Document = document
            });

            return StatusCode(201, new SuccessResponseDto
            {
                StatusCode = 201,
                Message = "Success",
                Description = "E-Sign document uploaded successfully.",
                Id = result.ToString()
            });
        }

        [HttpGet]
        [Route("api/v1/buyer/internal-rfq-questions")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_RFQ_QUESTIONS")]
        [SwaggerOperation("GetRFQQuestions")]
        [SwaggerResponse(200, type: typeof(List<RFQQuestionResponseDto>), description: "Fetched RFQ Questions successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetRFQQuestions(
            [FromQuery] Guid rfqId)
        {
            _logger.LogDebug($"Fetching RFQ Questions for RFQ Id: {rfqId}");

            var result = await _mediator.Send(
                new GetRFQQuestionsQuery(rfqId));

            _logger.LogDebug($"Fetched RFQ Questions successfully for RFQ Id: {rfqId}");

            return Ok(result);
        }
        [HttpGet]
        [Route("api/v1/buyer/supplier-invitation")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_SUPPLIER_INVITATION")]
        [SwaggerOperation("GetSupplierInvitations")]
        [SwaggerResponse(200, type: typeof(List<RFQListDto>), description: "Fetched Supplier Invitations successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetSupplierInvitations(
            [FromQuery] int index,
            [FromQuery] int limit,
            [FromQuery] string? search,
            [FromQuery] string? status)
        {

            _logger.LogDebug($"Fetching Supplier Invitations for Index: {index}, Limit: {limit}");
            Guid organizationId = GetOrganizationId();
            var result = await _mediator.Send(
                new SupplierInvitationQuery
                {
                    OrganizationId = organizationId,
                    Index = index,
                    Limit = limit,
                    Search = search,
                    Status = status
                });

            _logger.LogDebug($"Fetched Supplier Invitations successfully for Index: {index}, Limit: {limit}");

            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/buyer-invitation")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_BUYER_INVITATION")]
        [SwaggerOperation("GetBuyerInvitations")]
        [SwaggerResponse(200, type: typeof(List<RFQListDto>), description: "Fetched Buyer Invitations successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetBuyerInvitations(
            [FromQuery] int index,
            [FromQuery] int limit,
            [FromQuery] string? search,
            [FromQuery] string? status
            )
        {
            _logger.LogDebug($"Fetching Buyer Invitations for Index: {index}, Limit: {limit}");
            Guid organizationId = GetOrganizationId();
            var result = await _mediator.Send(
                new BuyerInvitationQuery
                {
                    OrganizationId = organizationId,
                    Index = index,
                    Limit = limit,
                    Search = search,
                    Status = status
                });

            _logger.LogDebug($"Fetched Buyer Invitations successfully for Index: {index}, Limit: {limit}");

            return Ok(result);
        }
        [HttpPut]
        [Route("api/v1/buyer/supplier-invitation-status")]
        [ValidateModelState]
        [ApiAuthorization(Name = "UPDATE_SUPPLIER_VERIFICATION_STATUS")]
        [SwaggerOperation("UpdateSupplierVerificationStatus")]
        [SwaggerResponse(200, type: typeof(bool), description: "Supplier verification status updated successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> UpdateSupplierVerificationStatus(
     [FromBody] UpdateSupplierVerificationStatusCommand command)
        {
            _logger.LogDebug(
                $"Updating Supplier Verification Status for RequestId: {command.RequestId}");

            var result = await _mediator.Send(command);

            return Ok(result);
        }
        [HttpGet]
        [Route("api/v1/buyer/cost-center/{costCenterId}")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_COST_CENTER")]
        [SwaggerOperation("GetCostCenterById")]
        [SwaggerResponse(200, type: typeof(CostCenterDto), description: "Success")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Cost Center Not Found")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetCostCenterById(
    Guid costCenterId)
        {
            _logger.LogDebug(
                $"Fetching Cost Center for CostCenterId: {costCenterId}");

            var result = await _mediator.Send(
                new GetCostCenterByIdQuery
                {
                    CostCenterId = costCenterId
                });

            _logger.LogDebug(
                $"Cost Center fetched successfully for CostCenterId: {costCenterId}");

            return Ok(result);
        }
        [HttpPut]
        [Route("api/v1/buyer/rfq/{rfqId}")]
        [ValidateModelState]
        [ApiAuthorization(Name = "UPDATE-RFQ")]
        [SwaggerOperation("UpdateRFQ")]
        [SwaggerResponse(200, type: typeof(Guid))]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "RFQ Not Found")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> UpdateRFQ(
    Guid rfqId,
    [FromBody] UpdateRFQDto dto)
        {
            var organizationId = GetOrganizationId();

            var result = await _mediator.Send(
                new UpdateRFQCommand(
                    rfqId,
                    organizationId,
                    dto));

            return Ok(new
            {
                success = true,
                message = "RFQ updated successfully.",
                rfqId = result
            });
        }
        [HttpPut]
        [Route("api/v1/buyer/rfq-status")]
        [ValidateModelState]
        [ApiAuthorization(Name = "UPDATE-BUYER_RFQ-STATUS")]
        [SwaggerOperation("UpdateBuyerRFQStatus")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "RFQ status updated successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "RFQ not found")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> UpdateRFQStatus(
   [FromBody] UpdateRFQStatusDto dto)
        {
            var result = await _mediator.Send(
                new UpdateRFQStatusCommand(dto));

            return Ok(new
            {
                success = true,
                message = "Supplier RFQ status updated successfully."
            });
        }
        [HttpGet]
        [Route("api/v1/buyer/internal/check-user-access/{rfqId}")]
        [ValidateModelState]
         [ApiAuthorization(Name = "CHECK-RFQ-USER-ACCESS")]
        [SwaggerOperation("CheckRFQUserAccess")]
        [SwaggerResponse(200, type: typeof(bool), description: "User is authorized for this RFQ")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(401, type: typeof(ErrorResponseDto), description: "Unauthorized")]
        [SwaggerResponse(403, type: typeof(ErrorResponseDto), description: "Access Denied")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> CheckRFQUserAccess(
            [FromRoute] Guid rfqId)
        {
            var query = new CheckRFQUserAccessQuery
            {
                RFQId = rfqId,
                UserId = GetUserId()
            };

            var result = await _mediator.Send(query);

            return Ok(result);
        }

    }
}