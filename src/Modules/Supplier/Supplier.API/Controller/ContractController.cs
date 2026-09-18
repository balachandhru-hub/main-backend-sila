using Microsoft.AspNetCore.Mvc;
using SharedKernel.Attributes;
using SharedKernel.Controllers;
using SharedKernel.Dto;
using SharedKernel.LoggerServices;
using Supplier.Application.Contracts;
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
        private readonly ILoggerManager _logger;

        public ContractController(
            IBuyerApiClient buyerApiClient,
            ILoggerManager logger)
        {
            _buyerApiClient = buyerApiClient;
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
    }
}
