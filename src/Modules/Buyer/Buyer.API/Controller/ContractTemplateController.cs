using Buyer.Application.Features.Commands.CreateContractTemplate;
using Buyer.Application.Features.Commands.UpdateContractTemplate;
using Buyer.Application.Features.Profile.Queries.GetBuyerId;
using Buyer.Application.Features.Queries.GetAllContractTemplates;
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
    public class ContractTemplateController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public ContractTemplateController(
            IMediator mediator,
            ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpPost]
        [Route("api/v1/buyer/contract-template")]
        [ValidateModelState]
        [ApiAuthorization(Name = "CREATE_CONTRACT")]
        [SwaggerOperation("CreateContractTemplate")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Contract template attachment uploaded successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(409, type: typeof(ErrorResponseDto), description: "An attachment already exists for this segment")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> CreateContractTemplate(
            [FromBody] CreateContractTemplateDto request)
        {
            _logger.LogDebug($"Creating contract template for SegmentId: {request.SegmentId}");

            Guid buyerId = await _mediator.Send(
                new GetBuyerIdQuery(GetOrganizationId()));

            Guid contractTemplateId = await _mediator.Send(
                new CreateContractTemplateCommand(request, buyerId));

            return Ok(new SuccessResponseDto
            {
                Id = contractTemplateId.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "Contract template attachment uploaded successfully."
            });
        }

        [HttpPut]
        [Route("api/v1/buyer/contract-template/{contractTemplateId}")]
        [ValidateModelState]
        [ApiAuthorization(Name = "CREATE_CONTRACT")]
        [SwaggerOperation("UpdateContractTemplate")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Contract template updated successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Contract template not found")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> UpdateContractTemplate(
            [FromRoute] Guid contractTemplateId,
            [FromBody] UpdateContractTemplateDto request)
        {
            _logger.LogDebug($"Updating contract template Id: {contractTemplateId}");

            Guid buyerId = await _mediator.Send(
                new GetBuyerIdQuery(GetOrganizationId()));

            Guid id = await _mediator.Send(
                new UpdateContractTemplateCommand(contractTemplateId, request, buyerId));

            return Ok(new SuccessResponseDto
            {
                Id = id.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "Contract template updated successfully."
            });
        }

        [HttpGet]
        [Route("api/v1/buyer/contract-template")]
        [ApiAuthorization(Name = "GET_CONTRACT")]
        [SwaggerOperation("GetAllContractTemplates")]
        [SwaggerResponse(200, type: typeof(List<ContractTemplateResponseDto>), description: "Success")]
        public async Task<IActionResult> GetAllContractTemplates(
            [FromQuery] int index = 0,
            [FromQuery] int limit = 10)
        {
            Guid buyerId = await _mediator.Send(
                new GetBuyerIdQuery(GetOrganizationId()));

            var result = await _mediator.Send(new GetAllContractTemplatesQuery
            {
                BuyerId = buyerId,
                Index = index,
                Limit = limit
            });

            return Ok(result);
        }
    }
}
