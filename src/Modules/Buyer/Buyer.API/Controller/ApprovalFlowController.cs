using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Attributes;
using SharedKernel.Dto;
using SharedKernel.LoggerServices;
using Swashbuckle.AspNetCore.Annotations;
using Buyer.Domain.Dtos;
using SharedKernel.Controllers;
using Buyer.Application.Features.Commands.MasterApprovalFlows;
using Buyer.Application.Features.Queries.MasterApprovalFlow;
using Buyer.Application.Features.Queries.ApprovalFlowUserMapping;
using Buyer.Application.Features.Commands.ApproveRejectMaster;
using Buyer.Application.Features.Queries.GetPendingApprovals;
using Buyer.Application.Features.Queries.GetPredefinedMaterialDetail;

namespace Buyer.API.Controller
{
    [ApiController]
    public class ApprovalFlowController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public ApprovalFlowController(
            IMediator mediator,
            ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Create Master Approval Flow
        /// </summary>
        [HttpPost]
        [Route("api/v1/buyer/master-approval-flow")]
        [ValidateModelState]
        [ApiAuthorization(Name = "CREATE_MASTER_APPROVAL_FLOW")]
        [SwaggerOperation("CreateMasterApprovalFlow")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Master Approval Flow created successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> CreateMasterApprovalFlow(
            [FromBody] CreateMasterApprovalFlowDto dto)
        {
            _logger.LogDebug(
                $"Creating Master Approval Flow. " +
                $"ApprovalCode: {dto.ApprovalCode}");

            var result = await _mediator.Send(
                new CreateMasterApprovalFlowCommand(
                    dto,
                    GetOrganizationId()));

            _logger.LogDebug(
                $"Master Approval Flow created successfully: {result}");

            return Ok(new SuccessResponseDto
            {
                Id = result.ToString(),
                Message = "Master Approval Flow created successfully",
                Description = "Master Approval Flow created successfully",
                StatusCode = 201
            });
        }

        /// <summary>
        /// Get Master Approval Flow
        /// </summary>
        [HttpGet]
        [Route("api/v1/buyer/master-approval-flow")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_MASTER_APPROVAL_FLOW")]
        [SwaggerOperation("GetMasterApprovalFlow")]
        [SwaggerResponse(200, type: typeof(MasterApprovalFlowDto), description: "Master Approval Flow retrieved successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetMasterApprovalFlow(
            [FromQuery] int index = 0,
            [FromQuery] int limit = 10,
            [FromQuery] Guid? buyerId = null)
        {
            _logger.LogDebug("Fetching Master Approval Flow.");

            var result = await _mediator.Send(
                new GetMasterApprovalFlowQuery
                {
                    Index = index,
                    Limit = limit,
                    BuyerId = buyerId
                });

            _logger.LogDebug("Master Approval Flow retrieved successfully.");

            return Ok(result);
        }

        /// <summary>
        /// Get Approval Flow User Mapping
        /// </summary>
        [HttpGet]
        [Route("api/v1/buyer/master-approval-flow/{approvalId}")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_APPROVAL_FLOW_USER_MAPPING")]
        [SwaggerOperation("GetApprovalFlowUserMapping")]
        [SwaggerResponse(200, type: typeof(List<Guid>), description: "Approval Flow Users retrieved successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetApprovalFlowUserMapping(Guid approvalId)
        {
            _logger.LogDebug($"Fetching Approval Flow Users. ApprovalId: {approvalId}");

            var result = await _mediator.Send(
                new GetApprovalFlowUserMappingQuery(approvalId));

            _logger.LogDebug("Approval Flow Users retrieved successfully.");

            return Ok(result);
        }

        /// <summary>
        /// Update Master Approval Flow By Approval Flow User Mapping Id
        /// </summary>
        [HttpPut]
        [Route("api/v1/buyer/approval-flow-user-mapping/{approvalFlowUserMappingId}")]
        [ValidateModelState]
        [ApiAuthorization(Name = "UPDATE_MASTER_APPROVAL_FLOW")]
        [SwaggerOperation("UpdateMasterApprovalFlowByUserMapping")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Master Approval Flow updated successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Not Found")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> UpdateMasterApprovalFlowByUserMapping(
            Guid approvalFlowUserMappingId,
            [FromBody] UpdateMasterApprovalFlowDto dto)
        {
            _logger.LogDebug(
                $"Updating Master Approval Flow via ApprovalFlowUserMappingId: {approvalFlowUserMappingId}");

            var result = await _mediator.Send(
                new UpdateMasterApprovalFlowByUserMappingCommand(
                    approvalFlowUserMappingId,
                    dto));

            _logger.LogDebug(
                $"Master Approval Flow updated successfully: {result}");

            return Ok(new SuccessResponseDto
            {
                Id = result.ToString(),
                Message = "Master Approval Flow updated successfully",
                Description = "Master Approval Flow updated successfully",
                StatusCode = 200
            });
        }
        [HttpPut]
        [Route("api/v1/buyer/item-master/approval/{predefinedMaterialId}")]
        [ValidateModelState]
        [ApiAuthorization(Name = "APPROVE_ITEM_MASTER")]
        [SwaggerOperation("ApproveOrRejectItemMaster")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto),
            description: "Material approval status updated successfully")]
        public async Task<IActionResult> ApproveOrRejectItemMaster(
            Guid predefinedMaterialId,
            [FromBody] ApproveRejectItemMasterDto dto)
        {
            Guid userId = GetUserId();

            _logger.LogDebug(
                $"Processing material approval for PredefinedMaterialId: {predefinedMaterialId}, UserId: {userId}");

            Guid result = await _mediator.Send(
                new ApproveRejectMasterCommand(
                    predefinedMaterialId,
                    userId,
                    dto));

            return Ok(new SuccessResponseDto
            {
                Id = result.ToString(),
                Message = "Material approval status updated successfully",
                Description = "Material approval status updated successfully",
                StatusCode = 200
            });
        }
        /// <summary>
        /// Get Predefined Material Detail (including approval user ids)
        /// </summary>
        [HttpGet]
        [Route("api/v1/buyer/item-master/approval/{predefinedMaterialId}")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_ITEM_MASTER_APPROVAL_DETAIL")]
        [SwaggerOperation("GetPredefinedMaterialDetail")]
        [SwaggerResponse(200, type: typeof(PredefinedMaterialDetailDto), description: "Predefined Material detail retrieved successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Predefined Material not found")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetPredefinedMaterialDetail(Guid predefinedMaterialId)
        {
            _logger.LogDebug(
                $"Fetching Predefined Material detail. PredefinedMaterialId: {predefinedMaterialId}");

            var result = await _mediator.Send(
                new GetPredefinedMaterialDetailQuery(predefinedMaterialId));

            _logger.LogDebug(
                $"Predefined Material detail retrieved successfully. PredefinedMaterialId: {predefinedMaterialId}");

            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/item-master/pending-approvals")]
        [ValidateModelState]
        [ApiAuthorization(Name = "VIEW_ITEM_MASTER_APPROVAL")]
        [SwaggerOperation("GetPendingApprovals")]
        [SwaggerResponse(200,type: typeof(List<PendingApprovalDto>),description: "Pending approvals retrieved successfully")]
        [SwaggerResponse(400,type: typeof(ErrorResponseDto),description: "Bad Request")]
        [SwaggerResponse(500,type: typeof(ErrorResponseDto),description: "Internal Server Error")]
        public async Task<IActionResult> GetPendingApprovals(
            [FromQuery] string? status ,
            [FromQuery] string? searchTerm,
            [FromQuery] int index = 0,
            [FromQuery] int limit = 10,
            [FromQuery] string? type = null)
        {
            Guid userId = GetUserId();

            _logger.LogDebug(
                $"Getting approvals for UserId: {userId}, " +
                $"Status: {status}, SearchTerm: {searchTerm}, " +
                $"Index: {index}, Limit: {limit}, Type: {type}");

            var result =
                await _mediator.Send(
                    new GetPendingApprovalsQuery(userId, status, searchTerm, index, limit, type));

            return Ok(result);
        }
    }
}
