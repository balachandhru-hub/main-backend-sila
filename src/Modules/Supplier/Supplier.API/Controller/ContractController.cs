using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Attributes;
using SharedKernel.Controllers;
using SharedKernel.Dto;
using SharedKernel.LoggerServices;
using Supplier.Application.Contracts;
using Supplier.Application.Features.Queries.GetSupplierInvitedRFQs;
using Supplier.Domain.Dto;
using Swashbuckle.AspNetCore.Annotations;

namespace Supplier.API.Controllers
{
    /// <summary>
    /// Supplier-facing contract endpoints. Contracts live in the Buyer service;
    /// these actions proxy to the Buyer internal contract APIs.
    /// </summary>
    [ApiController]
    public class ContractController : BaseController
    {
        private readonly IBuyerApiClient _buyerApiClient;
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public ContractController(
            IBuyerApiClient buyerApiClient,
            IMediator mediator,
            ILoggerManager logger)
        {
            _buyerApiClient = buyerApiClient;
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet]
        [Route("api/v1/supplier/contract/{contractId}")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_CONTRACT")]
        [SwaggerOperation("GetContract")]
        [SwaggerResponse(200, type: typeof(ContractResponseDto), description: "Success")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Contract Not Found")]
        public async Task<IActionResult> GetContract(
            [FromRoute] Guid contractId)
        {
            var result = await _buyerApiClient.GetContract(contractId);

            return Ok(result);
        }

        /// <summary>
        /// RFQs the logged-in supplier user has been invited to (matched by UserId in
        /// RFQOrganizationUserMapping), each paired with that supplier's own contract for
        /// the RFQ, if one exists. A contract belonging to another supplier invited to the
        /// same RFQ is never returned. Supplier admins bypass the invitation filter and see
        /// every RFQ/contract belonging to the supplier organization.
        /// </summary>
        [HttpPost]
        [Route("api/v1/supplier/contract/invited-rfqs")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_CONTRACT")]
        [SwaggerOperation("GetInvitedRFQsWithContract")]
        [SwaggerResponse(200, type: typeof(List<SupplierInvitedRFQContractDto>), description: "Success")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Supplier not found")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetInvitedRFQsWithContract(
            [FromBody] GetSupplierInvitedRFQsQuery query)
        {
            query.OrganizationId = GetOrganizationId();
            query.UserId = GetUserId();
            query.RoleId = GetRoleId();

            var result = await _mediator.Send(query);

            return Ok(result);
        }
    }
}
