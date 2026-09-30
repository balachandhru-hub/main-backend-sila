using Buyer.Application.Features.Commands.CreateContractDetails;
using Buyer.Application.Features.Profile.Queries.GetBuyerId;
using Buyer.Application.Features.Queries.GetAllContractDetails;
using Buyer.Application.Features.Queries.GetContractDetails;
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
    public class ContractDetailsController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public ContractDetailsController(
            IMediator mediator,
            ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpPost]
        [Route("api/v1/buyer/contract-detail")]
        [ValidateModelState]
        [ApiAuthorization(Name = "CREATE_CONTRACT")]
        [SwaggerOperation("CreateContractDetails")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Contract details created successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Contract or Segment Not Found")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> CreateContractDetails(
            [FromBody] CreateContractDetailsDto request)
        {
            _logger.LogDebug($"Creating contract details for PredefinedContractId: {request.PredefinedContractId}");

            Guid buyerId = await _mediator.Send(
                new GetBuyerIdQuery(GetOrganizationId()));

            Guid contractDetailsId = await _mediator.Send(
                new CreateContractDetailsCommand(request, buyerId));

            return Ok(new SuccessResponseDto
            {
                Id = contractDetailsId.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "Contract details created successfully."
            });
        }

        [HttpGet]
        [Route("api/v1/buyer/contract-detail")]
        [ApiAuthorization(Name = "GET_CONTRACT")]
        [SwaggerOperation("GetAllContractDetails")]
        [SwaggerResponse(200, type: typeof(List<ContractDetailsResponseDto>), description: "Success")]
        public async Task<IActionResult> GetAllContractDetails(
            [FromQuery] int index = 0,
            [FromQuery] int limit = 10)
        {
            Guid buyerId = await _mediator.Send(
                new GetBuyerIdQuery(GetOrganizationId()));

            var result = await _mediator.Send(new GetAllContractDetailsQuery
            {
                BuyerId = buyerId,
                Index = index,
                Limit = limit
            });

            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/contract-detail/{id}")]
        [ApiAuthorization(Name = "GET_CONTRACT")]
        [SwaggerOperation("GetContractDetails")]
        [SwaggerResponse(200, type: typeof(ContractDetailsResponseDto), description: "Success")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Contract Details Not Found")]
        public async Task<IActionResult> GetContractDetails(
            [FromRoute] Guid id)
        {
            var result = await _mediator.Send(new GetContractDetailsQuery(id));

            return Ok(result);
        }
    }
}
