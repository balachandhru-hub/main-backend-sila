using MediatR;
using Microsoft.AspNetCore.Mvc;

using SharedKernel.LoggerServices;

using SharedKernel.Controllers;
using Buyer.Application.Features.Queries.Invitation;
using SharedKernel.Attributes;
using Swashbuckle.AspNetCore.Annotations;
using SharedKernel.Dto;
using Buyer.Domain.Dto;
using Buyer.Application.Features.Queries.GetApprovalKpi;
using Buyer.Application.Features.Queries.GetBuyerDashboardAnalytics;




namespace Buyer.API.Controllers
{
    [ApiController]
    public class KPIController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;



        public KPIController(IMediator mediator, ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;

        }
        [HttpGet]
        [Route("api/v1/buyer/invitation-summary")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_SUPPLIER_INVITATION")]
        [SwaggerOperation("GetInvitationSummary")]
        [SwaggerResponse(200, type: typeof(SupplierInvitationSummaryDto), description: "Fetched Invitation Summary successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetInvitationSummary()
        {
            Guid organizationId = GetOrganizationId();

            string organizationType = GetOrganizationType();

            _logger.LogDebug(
                $"Fetching Invitation Summary for OrganizationId: {organizationId}, OrganizationType: {organizationType}");

            var result = await _mediator.Send(
                new InvitationSummaryQuery
                {
                    OrganizationId = organizationId,
                    OrganizationType = organizationType
                });

            _logger.LogDebug(
                $"Fetched Invitation Summary successfully for OrganizationId: {organizationId}");

            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/invitation-count")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_INVITATION_COUNT")]
        [SwaggerOperation("GetInvitationCount")]
        [SwaggerResponse(200, type: typeof(int), description: "Fetched invitation count successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetInvitationCount()
        {
            Guid organizationId = GetOrganizationId();

            string organizationType = GetOrganizationType();

            _logger.LogDebug(
                $"Fetching invitation count for OrganizationId: {organizationId}, OrganizationType: {organizationType}");

            var result = await _mediator.Send(
                new InvitationSummaryQuery
                {
                    OrganizationId = organizationId,
                    OrganizationType = organizationType
                });

            return Ok(result);
        }
        [HttpGet]
        [Route("api/v1/buyer/item-master/approval-kpi")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_APPROVAL_KPI")]
        [SwaggerOperation("GetApprovalKpi")]
        [SwaggerResponse(200,type: typeof(ApprovalKpiDto),description: "Approval KPI counts retrieved successfully")]
        [SwaggerResponse(400,type: typeof(ErrorResponseDto),description: "Bad Request")]
        [SwaggerResponse(500,type: typeof(ErrorResponseDto),description: "Internal Server Error")]
        public async Task<IActionResult> GetApprovalKpi()
        {
            Guid userId = GetUserId();

            _logger.LogDebug(
                $"Fetching approval KPI counts for UserId: {userId}");

            var result = await _mediator.Send(
                new GetApprovalKpiQuery(userId));

            return Ok(result);
        }

        /// <summary>
        /// Aggregated RFQ, supplier and contract figures for the buyer dashboard.
        /// Uses the RFQ list permission: anyone who can list RFQs can see their analytics.
        /// </summary>
        [HttpGet]
        [Route("api/v1/buyer/dashboard-analytics")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_RFQ_MASTER_DATA")]
        [SwaggerOperation("GetBuyerDashboardAnalytics")]
        [SwaggerResponse(200, type: typeof(BuyerDashboardAnalyticsDto), description: "Dashboard analytics retrieved successfully")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Buyer not found")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetDashboardAnalytics(CancellationToken cancellationToken)
        {
            Guid organizationId = GetOrganizationId();

            _logger.LogDebug($"Fetching buyer dashboard analytics for OrganizationId: {organizationId}");

            var result = await _mediator.Send(
                new GetBuyerDashboardAnalyticsQuery(organizationId, GetUserId(), GetRoleId()),
                cancellationToken);

            return Ok(result);
        }
    }
}
