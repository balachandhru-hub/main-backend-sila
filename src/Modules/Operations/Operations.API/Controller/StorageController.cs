using Operations.Application.Features.Commands.CreateMicrosoftAuthorization;
using Operations.Application.Features.Commands.CreateStorageConnection;
using Operations.Application.Features.Commands.DisconnectMicrosoftConnection;
using Operations.Application.Features.Commands.HandleMicrosoftCallback;
using Operations.Application.Features.Commands.ValidateMicrosoftConnection;
using Operations.Application.Features.Queries.GetMicrosoftConnection;
using Operations.Application.Features.Queries.GetMicrosoftFolders;
using Operations.Application.Features.Queries.GetMicrosoftLibraries;
using Operations.Application.Features.Queries.GetMicrosoftReadiness;
using Operations.Application.Features.Queries.GetStorageConnections;
using Operations.Application.Features.Queries.ResolveMicrosoftSite;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Operations.Domain.Dtos;
using Operations.Domain.Enums;
using SharedKernel.Attributes;
using SharedKernel.Controllers;
using SharedKernel.Dto;
using SharedKernel.LoggerServices;
using Swashbuckle.AspNetCore.Annotations;

namespace Operations.API.Controllers
{
    /// <summary>
    /// External document storage (Microsoft SharePoint): connections, Microsoft sign-in and the destination folder.
    /// </summary>
    [ApiController]
    public class StorageController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public StorageController(IMediator mediator, ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet]
        [Route("api/v1/operations/storage-connections")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_STORAGE")]
        [SwaggerOperation("GetStorageConnections")]
        [SwaggerResponse(200, type: typeof(List<StorageConnectionResponseDto>))]
        public async Task<IActionResult> GetStorageConnections()
        {
            _logger.LogDebug($"Fetching storage connections.");
            List<StorageConnectionResponseDto> result = await _mediator.Send(new GetStorageConnectionsQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId()
            });
            _logger.LogDebug($"Storage connections fetched. Count: {result.Count}");
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/operations/storage-connections")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_STORAGE")]
        [SwaggerOperation("CreateStorageConnection")]
        [SwaggerResponse(200, type: typeof(StorageConnectionResponseDto))]
        public async Task<IActionResult> CreateStorageConnection([FromBody] CreateStorageConnectionRequestDto request)
        {
            _logger.LogDebug($"Creating storage connection. Provider: {request.Provider}, Name: {request.Name}");
            StorageConnectionResponseDto result = await _mediator.Send(new CreateStorageConnectionCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                Request = request
            });
            _logger.LogDebug($"Storage connection created. ConnectionId: {result.Id}");
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/operations/integrations/microsoft/readiness")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_STORAGE")]
        [SwaggerOperation("GetMicrosoftReadiness")]
        [SwaggerResponse(200, type: typeof(MicrosoftReadinessResponseDto))]
        public async Task<IActionResult> GetMicrosoftReadiness()
        {
            _logger.LogDebug($"Fetching Microsoft readiness.");
            MicrosoftReadinessResponseDto result = await _mediator.Send(new GetMicrosoftReadinessQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId()
            });
            _logger.LogDebug($"Microsoft readiness fetched. Ready: {result.GraphIntegrationReady}");
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/operations/integrations/microsoft/connect")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_STORAGE")]
        [SwaggerOperation("ConnectMicrosoft")]
        [SwaggerResponse(200, type: typeof(MicrosoftConnectResponseDto))]
        public async Task<IActionResult> ConnectMicrosoft([FromBody] MicrosoftConnectRequestDto request)
        {
            _logger.LogDebug($"Starting Microsoft authorization. ReturnUrl: {request.ReturnUrl}");
            MicrosoftConnectResponseDto result = await _mediator.Send(new CreateMicrosoftAuthorizationCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                Request = request
            });
            _logger.LogDebug($"Microsoft authorization started. DraftId: {result.DraftId}");
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/operations/integrations/microsoft/callback")]
        [SwaggerOperation("MicrosoftCallback")]
        [SwaggerResponse(302)]
        public async Task<IActionResult> MicrosoftCallback([FromQuery] MicrosoftCallbackRequestDto request)
        {
            _logger.LogDebug($"Microsoft authorization callback received. HasCode: {!string.IsNullOrWhiteSpace(request.Code)}, Error: {request.Error}");
            string result = await _mediator.Send(new HandleMicrosoftCallbackCommand
            {
                Request = request
            });
            _logger.LogDebug($"Microsoft authorization callback handled.");
            return Redirect(result);
        }

        [HttpGet]
        [Route("api/v1/operations/integrations/microsoft/connections/{connectionId:guid}")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_STORAGE")]
        [SwaggerOperation("GetMicrosoftConnection")]
        [SwaggerResponse(200, type: typeof(MicrosoftConnectionValidationResponseDto))]
        public async Task<IActionResult> GetMicrosoftConnection([FromRoute] Guid connectionId)
        {
            _logger.LogDebug($"Fetching Microsoft connection. ConnectionId: {connectionId}");
            MicrosoftConnectionValidationResponseDto result = await _mediator.Send(new GetMicrosoftConnectionQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                ConnectionId = connectionId
            });
            _logger.LogDebug($"Microsoft connection fetched. ConnectionId: {connectionId}, Status: {result.Status}");
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/operations/integrations/microsoft/connections/{connectionId:guid}/site")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_STORAGE")]
        [SwaggerOperation("ResolveMicrosoftSite")]
        [SwaggerResponse(200, type: typeof(MicrosoftSiteResponseDto))]
        public async Task<IActionResult> ResolveMicrosoftSite([FromRoute] Guid connectionId, [FromBody] MicrosoftSiteRequestDto request)
        {
            _logger.LogDebug($"Resolving SharePoint site. ConnectionId: {connectionId}");
            MicrosoftSiteResponseDto result = await _mediator.Send(new ResolveMicrosoftSiteQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                ConnectionId = connectionId,
                Request = request
            });
            _logger.LogDebug($"SharePoint site resolved. ConnectionId: {connectionId}, SiteId: {result.Id}");
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/operations/integrations/microsoft/connections/{connectionId:guid}/libraries")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_STORAGE")]
        [SwaggerOperation("GetMicrosoftLibraries")]
        [SwaggerResponse(200, type: typeof(List<MicrosoftLibraryResponseDto>))]
        public async Task<IActionResult> GetMicrosoftLibraries([FromRoute] Guid connectionId, [FromQuery] string siteId)
        {
            _logger.LogDebug($"Fetching SharePoint libraries. ConnectionId: {connectionId}");
            List<MicrosoftLibraryResponseDto> result = await _mediator.Send(new GetMicrosoftLibrariesQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                ConnectionId = connectionId,
                SiteId = siteId
            });
            _logger.LogDebug($"SharePoint libraries fetched. ConnectionId: {connectionId}, Count: {result.Count}");
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/operations/integrations/microsoft/connections/{connectionId:guid}/libraries")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_STORAGE")]
        [SwaggerOperation("ListMicrosoftLibraries")]
        [SwaggerResponse(200, type: typeof(List<MicrosoftLibraryResponseDto>))]
        public async Task<IActionResult> ListMicrosoftLibraries([FromRoute] Guid connectionId, [FromQuery] string siteId)
        {
            _logger.LogDebug($"Fetching SharePoint libraries. ConnectionId: {connectionId}");
            List<MicrosoftLibraryResponseDto> result = await _mediator.Send(new GetMicrosoftLibrariesQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                ConnectionId = connectionId,
                SiteId = siteId
            });
            _logger.LogDebug($"SharePoint libraries fetched. ConnectionId: {connectionId}, Count: {result.Count}");
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/operations/integrations/microsoft/connections/{connectionId:guid}/folders")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_STORAGE")]
        [SwaggerOperation("GetMicrosoftFolders")]
        [SwaggerResponse(200, type: typeof(List<MicrosoftFolderResponseDto>))]
        public async Task<IActionResult> GetMicrosoftFolders([FromRoute] Guid connectionId, [FromQuery] MicrosoftFolderListRequestDto request)
        {
            _logger.LogDebug($"Fetching SharePoint folders. ConnectionId: {connectionId}, FolderPath: {request.FolderPath}");
            List<MicrosoftFolderResponseDto> result = await _mediator.Send(new GetMicrosoftFoldersQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                ConnectionId = connectionId,
                Request = request
            });
            _logger.LogDebug($"SharePoint folders fetched. ConnectionId: {connectionId}, Count: {result.Count}");
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/operations/integrations/microsoft/connections/{connectionId:guid}/folders")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_STORAGE")]
        [SwaggerOperation("ListMicrosoftFolders")]
        [SwaggerResponse(200, type: typeof(List<MicrosoftFolderResponseDto>))]
        public async Task<IActionResult> ListMicrosoftFolders([FromRoute] Guid connectionId, [FromBody] MicrosoftFolderListRequestDto request)
        {
            _logger.LogDebug($"Fetching SharePoint folders. ConnectionId: {connectionId}, FolderPath: {request.FolderPath}");
            List<MicrosoftFolderResponseDto> result = await _mediator.Send(new GetMicrosoftFoldersQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                ConnectionId = connectionId,
                Request = request
            });
            _logger.LogDebug($"SharePoint folders fetched. ConnectionId: {connectionId}, Count: {result.Count}");
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/operations/integrations/microsoft/connections/{connectionId:guid}/validate")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_STORAGE")]
        [SwaggerOperation("ValidateMicrosoftConnection")]
        [SwaggerResponse(200, type: typeof(MicrosoftConnectionValidationResponseDto))]
        public async Task<IActionResult> ValidateMicrosoftConnection([FromRoute] Guid connectionId, [FromBody] MicrosoftValidateConnectionRequestDto request)
        {
            _logger.LogDebug($"Validating Microsoft connection. ConnectionId: {connectionId}");
            MicrosoftConnectionValidationResponseDto result = await _mediator.Send(new ValidateMicrosoftConnectionCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                ConnectionId = connectionId,
                Request = request
            });
            _logger.LogDebug($"Microsoft connection validated. ConnectionId: {connectionId}, Status: {result.Status}");
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/operations/integrations/microsoft/connections/{connectionId:guid}/disconnect")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_STORAGE")]
        [SwaggerOperation("DisconnectMicrosoftConnection")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> DisconnectMicrosoftConnection([FromRoute] Guid connectionId)
        {
            _logger.LogDebug($"Disconnecting Microsoft connection. ConnectionId: {connectionId}");
            Guid result = await _mediator.Send(new DisconnectMicrosoftConnectionCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                ConnectionId = connectionId
            });
            _logger.LogDebug($"Microsoft connection disconnected. ConnectionId: {result}");
            return Ok(new SuccessResponseDto
            {
                Id = result.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "Microsoft connection disconnected."
            });
        }
    }
}
