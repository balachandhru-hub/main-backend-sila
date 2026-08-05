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
using Identity.Application.Features.Queries.GetOrganizationUser;
using System.Security.Claims;
using Identity.Application.Features.Queries.GetPersonDetail;
using Identity.Application.Features.Commands.DeletePerson;



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
            _logger.LogDebug("Creating person.");

            Guid organizationId = GetOrganizationId();

            var result = await _mediator.Send(new CreatePersonCommand
            {
                OrganizationId = organizationId,
                Model = request
            });

            _logger.LogDebug("Person created successfully.");

            return Ok(result);
        }
        [HttpGet]
        [Route("api/v1/identity/model")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_MODEL")]
        [SwaggerOperation("GetAllModel")]
        [SwaggerResponse(200, type: typeof(List<ModelDto>), description: "Success")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetAllModel()
        {
            _logger.LogDebug("Fetching all model.");

            var result = await _mediator.Send(new GetAllModelQuery());

            _logger.LogDebug("Model fetched successfully.");

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
            _logger.LogDebug("Saving organization model mappings.");



            var result = await _mediator.Send(
                new SaveOrganizationModelCommand
                {

                    Model = request
                });

            _logger.LogDebug("Organization model mappings saved successfully.");

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
            _logger.LogDebug("Fetching organization model.");
            var result = await _mediator.Send(
                new GetOrganizationModelQuery
                {
                    OrganizationId = organizationId
                });
            _logger.LogDebug("Organization model fetched successfully.");
            return Ok(result);
        }
        [HttpGet]
        [Route("api/v1/identity/organization-users")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_ORGANIZATION_USERS")]
        [SwaggerOperation("GetOrganizationUsers")]
        [SwaggerResponse(200, type: typeof(List<UserListDto>), description: "Success")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetOrganizationUsers(
      [FromQuery] Guid? organizationId)
        {
            _logger.LogDebug("Fetching organization users.");
            string? role = User.FindFirst(ClaimTypes.Role)?.Value;

            
            if (!organizationId.HasValue)
            {
                organizationId = GetOrganizationId();
            }

            var result = await _mediator.Send(
                new GetOrganizationUserQuery
                {
                    OrganizationId = organizationId,
                    LoggedInRole = role
                });
        _logger.LogDebug("Organization users fetched successfully.");
            return Ok(result);
        }
        [HttpGet]
        [Route("api/v1/identity/person-detail")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_PERSON_DETAIL")]
        [SwaggerOperation("GetPersonDetail")]
        [SwaggerResponse(200, type: typeof(PersonDetailDto), description: "Success")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetPersonDetail([FromQuery] Guid personId)
        {
            _logger.LogDebug($"Fetching person details for PersonId: {personId}");
            var result = await _mediator.Send(
                new GetPersonDetailQuery
                {
                    PersonId = personId
                });
            _logger.LogDebug($"Person details fetched successfully for PersonId: {personId}");
            return Ok(result);
        }
        [HttpDelete]
        [Route("api/v1/identity/delete-person")]
        [ValidateModelState]
        [ApiAuthorization(Name = "DELETE_PERSON")]
        [SwaggerOperation("DeletePerson")]
        [SwaggerResponse(200, type: typeof(bool), description: "Person deleted successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> DeletePerson([FromQuery] Guid personId)
        {
            _logger.LogDebug($"Deleting person. PersonId: {personId}");

            var result = await _mediator.Send(
                new DeletePersonCommand
                {
                    PersonId = personId
                });

            _logger.LogDebug($"Person deleted successfully. PersonId: {personId}");

            return Ok(new SuccessResponseDto
            {
                Id = result.ToString(),
                Message = "Person deleted successfully",
                Description = "Person deleted successfully",
                StatusCode = 200
            });
        }
    }
}