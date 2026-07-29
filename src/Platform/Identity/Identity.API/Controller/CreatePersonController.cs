using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

using SharedKernel.LoggerServices;

using SharedKernel.Dto;
using SharedKernel.Attributes;

using Identity.Domain.Dto;

using Identity.Application.Features.Commands.CreatePerson;
using Identity.Application.Features.Queries.GetAllModel;
using Identity.Application.Features.Commands.SaveOrganizationModelMapping;



namespace Identity.API.Controllers
{
    [ApiController]
    public class CreatePersonController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;



        public CreatePersonController(
            IMediator mediator,
            ILoggerManager logger,
            HttpClient httpClient,
            IConfiguration configuration

            )
        {
            _mediator = mediator;
            _logger = logger;
            _httpClient = httpClient;
            _configuration = configuration;

        }

        [HttpPost]
        [Route("api/v1/identity/person")]
        [ValidateModelState]
        [ApiAuthorization(Name = "CREATE_PERSON")]
        [SwaggerOperation("CreatePerson")]
        [SwaggerResponse(200, type: typeof(Guid), description: "Person created successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> CreatePerson(
            [FromBody] CreatePersonDto request)
        {
            _logger.LogInfo("Creating person.");

            Guid organizationId = GetOrganizationId();

            var result = await _mediator.Send(new CreatePersonCommand
            {
                OrganizationId = organizationId,
                Model = request
            });

            _logger.LogInfo("Person created successfully.");

            return Ok(result);
        }
        [HttpGet]
        [Route("api/v1/identity/model")]
        [ApiAuthorization(Name = "GET_MODEL")]
        [SwaggerOperation("GetAllModel")]
        [SwaggerResponse(200, type: typeof(List<ModelDto>), description: "Success")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetAllModel()
        {
            _logger.LogInfo("Fetching all model.");

            var result = await _mediator.Send(new GetAllModelQuery());

            _logger.LogInfo("Model fetched successfully.");

            return Ok(result);
        }
        [HttpPut]
        [Route("api/v1/identity/organization-model")]
        [ValidateModelState]
        [ApiAuthorization(Name = "SAVE_ORGANIZATION_MODEL")]
        [SwaggerOperation("SaveOrganizationModel")]
        [SwaggerResponse(200, type: typeof(bool), description: "Model saved successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> SaveOrganizationModel(
            [FromBody] SaveOrganizationModelDto request)
        {
            _logger.LogInfo("Saving organization model mappings.");

         

            var result = await _mediator.Send(
                new SaveOrganizationModelCommand
                {
                    
                    Model = request
                });

            _logger.LogInfo("Organization model mappings saved successfully.");

            return Ok(result);
        }
        [HttpGet]
        [Route("api/v1/identity/organization-model")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_ORGANIZATION_MODEL")]
        [SwaggerOperation("GetOrganizationModel")]
        [SwaggerResponse(200, type: typeof(List<ModelDto>), description: "Success")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetOrganizationModel(
            [FromQuery] Guid? organizationId)
        {
            _logger.LogInfo("Fetching organization model.");
            var result = await _mediator.Send(
                new GetOrganizationModelQuery
                {
                    OrganizationId = organizationId
                });
_logger.LogInfo("Organization model fetched successfully.");
            return Ok(result);
        }
            }
}