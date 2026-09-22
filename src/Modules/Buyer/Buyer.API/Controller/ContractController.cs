using Buyer.Application.Features.Commands.ApproveRejectContract;
using Buyer.Application.Features.Commands.CreateContract;
using Buyer.Application.Features.Profile.Queries.GetBuyerId;
using Buyer.Application.Features.Queries.GetAllContracts;
using Buyer.Application.Features.Queries.GetContract;
using Buyer.Application.Features.Queries.GetSupplierContractStatus;
using Buyer.Domain.Dto;
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
    public class ContractController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public ContractController(
            IMediator mediator,
            ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpPost]
        [Route("api/v1/buyer/contract")]
        [ValidateModelState]
        [ApiAuthorization(Name = "CREATE_CONTRACT")]
        [SwaggerOperation("CreateContract")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Contract created successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "RFQ Not Found")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> CreateContract(
            [FromBody] CreateContractDto request)
        {
            _logger.LogDebug($"Creating contract for RFQ Id: {request.RFQId}");

            // The login token carries no BuyerId claim, so resolve the buyer from the
            // caller's organization. The supplier is resolved from the RFQ award.
            Guid buyerId = await _mediator.Send(
                new GetBuyerIdQuery(GetOrganizationId()));

            Guid contractId = await _mediator.Send(
                new CreateContractCommand(request, buyerId));

            return Ok(new SuccessResponseDto
            {
                Id = contractId.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "Contract created successfully."
            });
        }

        [HttpGet]
        [Route("api/v1/buyer/contract/{contractId}")]
        [ApiAuthorization(Name = "GET_CONTRACT")]
        [SwaggerOperation("GetContract")]
        [SwaggerResponse(200, type: typeof(ContractResponseDto), description: "Success")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Contract Not Found")]
        public async Task<IActionResult> GetContract(
            [FromRoute] Guid contractId)
        {
            var result = await _mediator.Send(new GetContractQuery(contractId));

            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/contract")]
        [ApiAuthorization(Name = "GET_CONTRACT")]
        [SwaggerOperation("GetAllContracts")]
        [SwaggerResponse(200, type: typeof(List<ContractResponseDto>), description: "Success")]
        public async Task<IActionResult> GetAllContracts(
            [FromQuery] int index = 0,
            [FromQuery] int limit = 10)
        {
            Guid buyerId = await _mediator.Send(
                new GetBuyerIdQuery(GetOrganizationId()));

            var result = await _mediator.Send(new GetAllContractsQuery
            {
                Index = index,
                Limit = limit,
                BuyerId = buyerId,
                UserId = GetUserId(),
                RoleId = GetRoleId()
            });

            return Ok(result);
        }

        /// <summary>
        /// Approve or Reject Contract
        /// </summary>
        [HttpPut]
        [Route("api/v1/buyer/contract/approval/{contractId}")]
        [ValidateModelState]
        [ApiAuthorization(Name = "APPROVE_CONTRACT")]
        [SwaggerOperation("ApproveOrRejectContract")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto),
            description: "Contract approval status updated successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Not Found")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> ApproveOrRejectContract(
            Guid contractId,
            [FromBody] ApproveRejectContractDto dto)
        {
            Guid userId = GetUserId();

            _logger.LogDebug(
                $"Processing contract approval for ContractId: {contractId}, UserId: {userId}");

            Guid result = await _mediator.Send(
                new ApproveRejectContractCommand(
                    contractId,
                    userId,
                    dto));

            return Ok(new SuccessResponseDto
            {
                Id = result.ToString(),
                Message = "Contract approval status updated successfully",
                Description = "Contract approval status updated successfully",
                StatusCode = 200
            });
        }

        // ---- Internal routes called by the Supplier service ----

        [HttpGet]
        [Route("api/v1/buyer/internal-contract-status")]
        [ApiAuthorization(Name = "GET_CONTRACT")]
        [SwaggerOperation("InternalGetSupplierContractStatus")]
        [SwaggerResponse(200, type: typeof(SupplierContractStatusDto), description: "Success")]
        public async Task<IActionResult> InternalGetSupplierContractStatus(
            [FromQuery] Guid rfqId,
            [FromQuery] Guid supplierId)
        {
            var result = await _mediator.Send(new GetSupplierContractStatusQuery
            {
                RFQId = rfqId,
                SupplierId = supplierId
            });

            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/internal-contract/{contractId}")]
        [ApiAuthorization(Name = "GET_CONTRACT")]
        [SwaggerOperation("InternalGetContract")]
        [SwaggerResponse(200, type: typeof(ContractResponseDto), description: "Success")]
        public async Task<IActionResult> InternalGetContract(
            [FromRoute] Guid contractId)
        {
            var result = await _mediator.Send(new GetContractQuery(contractId));

            return Ok(result);
        }
    }
}
