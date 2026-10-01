using Operations.Application.Features.Commands.ActivateIntegration;
using Operations.Application.Features.Commands.CommitIntegrationImport;
using Operations.Application.Features.Commands.CreateIntegration;
using Operations.Application.Features.Commands.DeactivateIntegration;
using Operations.Application.Features.Commands.DiscoverIntegrationSchema;
using Operations.Application.Features.Commands.RunIntegration;
using Operations.Application.Features.Commands.SaveIntegrationMappings;
using Operations.Application.Features.Commands.TestIntegration;
using Operations.Application.Features.Commands.UpdateIntegration;
using Operations.Application.Features.Queries.BuildImportCorrectionReport;
using Operations.Application.Features.Queries.ExportIntegrationData;
using Operations.Application.Features.Queries.GetImportTemplate;
using Operations.Application.Features.Queries.GetIntegration;
using Operations.Application.Features.Queries.GetIntegrationDataUpdate;
using Operations.Application.Features.Queries.GetIntegrationExecutions;
using Operations.Application.Features.Queries.GetIntegrationMappings;
using Operations.Application.Features.Queries.GetIntegrationSchema;
using Operations.Application.Features.Queries.GetIntegrationTargetFields;
using Operations.Application.Features.Queries.GetIntegrations;
using Operations.Application.Features.Queries.PreviewIntegrationImport;
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
    /// ERP integration framework: configurations, schema, mappings, pulls and the Excel data update.
    /// </summary>
    [ApiController]
    public class IntegrationController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public IntegrationController(IMediator mediator, ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet]
        [Route("api/v1/operations/integrations/target-fields")]
        [ApiAuthorization(Name = "OPERATIONS_VIEW_INTEGRATION")]
        [SwaggerOperation("GetIntegrationTargetFields")]
        [SwaggerResponse(200, type: typeof(List<IntegrationTargetFieldResponseDto>))]
        public async Task<IActionResult> GetIntegrationTargetFields()
        {
            _logger.LogDebug($"Fetching integration target fields.");
            List<IntegrationTargetFieldResponseDto> result = await _mediator.Send(new GetIntegrationTargetFieldsQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId()
            });
            _logger.LogDebug($"Integration target fields fetched. Count: {result.Count}");
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/operations/integrations/executions")]
        [ApiAuthorization(Name = "OPERATIONS_VIEW_INTEGRATION")]
        [SwaggerOperation("GetIntegrationExecutions")]
        [SwaggerResponse(200, type: typeof(List<IntegrationExecutionResponseDto>))]
        public async Task<IActionResult> GetIntegrationExecutions([FromQuery] Guid? configurationId)
        {
            _logger.LogDebug($"Fetching integration executions. ConfigurationId: {configurationId}");
            List<IntegrationExecutionResponseDto> result = await _mediator.Send(new GetIntegrationExecutionsQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                ConfigurationId = configurationId
            });
            _logger.LogDebug($"Integration executions fetched. Count: {result.Count}");
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/operations/integrations/data-update/template")]
        [ApiAuthorization(Name = "OPERATIONS_VIEW_INTEGRATION")]
        [SwaggerOperation("GetImportTemplate")]
        [SwaggerResponse(200, type: typeof(FileContentResult))]
        public async Task<IActionResult> GetImportTemplate([FromQuery] IntegrationImportKind kind = IntegrationImportKind.PURCHASE_ORDERS)
        {
            _logger.LogDebug($"Building import template. Kind: {kind}");
            FileDownloadDto result = await _mediator.Send(new GetImportTemplateQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                Kind = kind
            });
            _logger.LogDebug($"Import template built. FileName: {result.FileName}");
            return File(result.FileBytes, result.ContentType, result.FileName);
        }

        [HttpGet]
        [Route("api/v1/operations/integrations")]
        [ApiAuthorization(Name = "OPERATIONS_VIEW_INTEGRATION")]
        [SwaggerOperation("GetIntegrations")]
        [SwaggerResponse(200, type: typeof(List<IntegrationConfigurationResponseDto>))]
        public async Task<IActionResult> GetIntegrations()
        {
            _logger.LogDebug($"Fetching integrations.");
            List<IntegrationConfigurationResponseDto> result = await _mediator.Send(new GetIntegrationsQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId()
            });
            _logger.LogDebug($"Integrations fetched. Count: {result.Count}");
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/operations/integrations")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_INTEGRATION")]
        [SwaggerOperation("CreateIntegration")]
        [SwaggerResponse(200, type: typeof(IntegrationConfigurationResponseDto))]
        public async Task<IActionResult> CreateIntegration([FromBody] IntegrationConfigurationInputDto request)
        {
            _logger.LogDebug($"Creating integration. Name: {request.Name}, ProcessType: {request.ProcessType}, EntityCode: {request.EntityCode}");
            IntegrationConfigurationResponseDto result = await _mediator.Send(new CreateIntegrationCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                Request = request
            });
            _logger.LogDebug($"Integration created. ConfigurationId: {result.Id}");
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/operations/integrations/{configurationId:guid}")]
        [ApiAuthorization(Name = "OPERATIONS_VIEW_INTEGRATION")]
        [SwaggerOperation("GetIntegration")]
        [SwaggerResponse(200, type: typeof(IntegrationConfigurationResponseDto))]
        public async Task<IActionResult> GetIntegration([FromRoute] Guid configurationId)
        {
            _logger.LogDebug($"Fetching integration. ConfigurationId: {configurationId}");
            IntegrationConfigurationResponseDto result = await _mediator.Send(new GetIntegrationQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                ConfigurationId = configurationId
            });
            _logger.LogDebug($"Integration fetched. ConfigurationId: {result.Id}");
            return Ok(result);
        }

        [HttpPut]
        [Route("api/v1/operations/integrations/{configurationId:guid}")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_INTEGRATION")]
        [SwaggerOperation("UpdateIntegration")]
        [SwaggerResponse(200, type: typeof(IntegrationConfigurationResponseDto))]
        public async Task<IActionResult> UpdateIntegration([FromRoute] Guid configurationId, [FromBody] IntegrationConfigurationInputDto request)
        {
            _logger.LogDebug($"Updating integration. ConfigurationId: {configurationId}");
            IntegrationConfigurationResponseDto result = await _mediator.Send(new UpdateIntegrationCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                ConfigurationId = configurationId,
                Request = request
            });
            _logger.LogDebug($"Integration updated. ConfigurationId: {result.Id}");
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/operations/integrations/{configurationId:guid}/test")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_INTEGRATION")]
        [SwaggerOperation("TestIntegration")]
        [SwaggerResponse(200, type: typeof(IntegrationTestResponseDto))]
        public async Task<IActionResult> TestIntegration([FromRoute] Guid configurationId)
        {
            _logger.LogDebug($"Testing integration. ConfigurationId: {configurationId}");
            IntegrationTestResponseDto result = await _mediator.Send(new TestIntegrationCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                ConfigurationId = configurationId
            });
            _logger.LogDebug($"Integration tested. ConfigurationId: {configurationId}, Success: {result.Success}");
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/operations/integrations/{configurationId:guid}/schema")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_INTEGRATION")]
        [SwaggerOperation("DiscoverIntegrationSchema")]
        [SwaggerResponse(200, type: typeof(IntegrationSchemaResponseDto))]
        public async Task<IActionResult> DiscoverIntegrationSchema([FromRoute] Guid configurationId)
        {
            _logger.LogDebug($"Discovering integration schema. ConfigurationId: {configurationId}");
            IntegrationSchemaResponseDto result = await _mediator.Send(new DiscoverIntegrationSchemaCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                ConfigurationId = configurationId
            });
            _logger.LogDebug($"Integration schema discovered. ConfigurationId: {configurationId}, Entities: {result.Entities.Count}");
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/operations/integrations/{configurationId:guid}/schema")]
        [ApiAuthorization(Name = "OPERATIONS_VIEW_INTEGRATION")]
        [SwaggerOperation("GetIntegrationSchema")]
        [SwaggerResponse(200, type: typeof(IntegrationSchemaResponseDto))]
        public async Task<IActionResult> GetIntegrationSchema([FromRoute] Guid configurationId)
        {
            _logger.LogDebug($"Fetching integration schema. ConfigurationId: {configurationId}");
            IntegrationSchemaResponseDto result = await _mediator.Send(new GetIntegrationSchemaQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                ConfigurationId = configurationId
            });
            _logger.LogDebug($"Integration schema fetched. ConfigurationId: {configurationId}");
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/operations/integrations/{configurationId:guid}/mappings")]
        [ApiAuthorization(Name = "OPERATIONS_VIEW_INTEGRATION")]
        [SwaggerOperation("GetIntegrationMappings")]
        [SwaggerResponse(200, type: typeof(List<IntegrationMappingResponseDto>))]
        public async Task<IActionResult> GetIntegrationMappings([FromRoute] Guid configurationId)
        {
            _logger.LogDebug($"Fetching integration mappings. ConfigurationId: {configurationId}");
            List<IntegrationMappingResponseDto> result = await _mediator.Send(new GetIntegrationMappingsQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                ConfigurationId = configurationId
            });
            _logger.LogDebug($"Integration mappings fetched. ConfigurationId: {configurationId}, Count: {result.Count}");
            return Ok(result);
        }

        [HttpPut]
        [Route("api/v1/operations/integrations/{configurationId:guid}/mappings")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_INTEGRATION")]
        [SwaggerOperation("SaveIntegrationMappings")]
        [SwaggerResponse(200, type: typeof(List<IntegrationMappingResponseDto>))]
        public async Task<IActionResult> SaveIntegrationMappings([FromRoute] Guid configurationId, [FromBody] List<FieldMappingInputDto> request)
        {
            _logger.LogDebug($"Saving integration mappings. ConfigurationId: {configurationId}, Count: {request.Count}");
            List<IntegrationMappingResponseDto> result = await _mediator.Send(new SaveIntegrationMappingsCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                ConfigurationId = configurationId,
                Mappings = request
            });
            _logger.LogDebug($"Integration mappings saved. ConfigurationId: {configurationId}, Count: {result.Count}");
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/operations/integrations/{configurationId:guid}/activate")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_INTEGRATION")]
        [SwaggerOperation("ActivateIntegration")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> ActivateIntegration([FromRoute] Guid configurationId)
        {
            _logger.LogDebug($"Activating integration. ConfigurationId: {configurationId}");
            bool result = await _mediator.Send(new ActivateIntegrationCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                ConfigurationId = configurationId
            });
            _logger.LogDebug($"Integration activated. ConfigurationId: {configurationId}, Active: {result}");
            return Ok(new SuccessResponseDto
            {
                Id = configurationId.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "Integration activated."
            });
        }

        [HttpPost]
        [Route("api/v1/operations/integrations/{configurationId:guid}/deactivate")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_INTEGRATION")]
        [SwaggerOperation("DeactivateIntegration")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> DeactivateIntegration([FromRoute] Guid configurationId)
        {
            _logger.LogDebug($"Deactivating integration. ConfigurationId: {configurationId}");
            bool result = await _mediator.Send(new DeactivateIntegrationCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                ConfigurationId = configurationId
            });
            _logger.LogDebug($"Integration deactivated. ConfigurationId: {configurationId}, Active: {result}");
            return Ok(new SuccessResponseDto
            {
                Id = configurationId.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "Integration deactivated."
            });
        }

        [HttpPost]
        [Route("api/v1/operations/integrations/{configurationId:guid}/pull")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_INTEGRATION")]
        [SwaggerOperation("RunIntegration")]
        [SwaggerResponse(200, type: typeof(IntegrationExecutionResponseDto))]
        public async Task<IActionResult> RunIntegration([FromRoute] Guid configurationId, [FromBody] IntegrationExecutionRequestDto? request)
        {
            _logger.LogDebug($"Running integration. ConfigurationId: {configurationId}, FullSync: {request?.FullSync}");
            IntegrationExecutionResponseDto result = await _mediator.Send(new RunIntegrationCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                ConfigurationId = configurationId,
                Trigger = IntegrationExecutionTrigger.MANUAL,
                FullSync = request?.FullSync ?? false
            });
            _logger.LogDebug($"Integration run completed. ConfigurationId: {configurationId}, Status: {result.Status}");
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/operations/integrations/{configurationId:guid}/data-update")]
        [ApiAuthorization(Name = "OPERATIONS_VIEW_INTEGRATION")]
        [SwaggerOperation("GetIntegrationDataUpdate")]
        [SwaggerResponse(200, type: typeof(IntegrationDataUpdateResponseDto))]
        public async Task<IActionResult> GetIntegrationDataUpdate([FromRoute] Guid configurationId, [FromQuery] IntegrationDataUpdateRequestDto request)
        {
            _logger.LogDebug($"Fetching integration data. ConfigurationId: {configurationId}, Kind: {request.Kind}, Page: {request.Page}");
            IntegrationDataUpdateResponseDto result = await _mediator.Send(new GetIntegrationDataUpdateQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                ConfigurationId = configurationId,
                Request = request
            });
            _logger.LogDebug($"Integration data fetched. ConfigurationId: {configurationId}, TotalRows: {result.TotalRows}");
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/operations/integrations/{configurationId:guid}/data-update/template")]
        [ApiAuthorization(Name = "OPERATIONS_VIEW_INTEGRATION")]
        [SwaggerOperation("GetIntegrationImportTemplate")]
        [SwaggerResponse(200, type: typeof(FileContentResult))]
        public async Task<IActionResult> GetIntegrationImportTemplate([FromRoute] Guid configurationId, [FromQuery] IntegrationImportKind kind = IntegrationImportKind.PURCHASE_ORDERS)
        {
            _logger.LogDebug($"Building import template. ConfigurationId: {configurationId}, Kind: {kind}");
            FileDownloadDto result = await _mediator.Send(new GetImportTemplateQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                Kind = kind
            });
            _logger.LogDebug($"Import template built. FileName: {result.FileName}");
            return File(result.FileBytes, result.ContentType, result.FileName);
        }

        [HttpGet]
        [Route("api/v1/operations/integrations/{configurationId:guid}/data-update/export")]
        [ApiAuthorization(Name = "OPERATIONS_VIEW_INTEGRATION")]
        [SwaggerOperation("ExportIntegrationData")]
        [SwaggerResponse(200, type: typeof(FileContentResult))]
        public async Task<IActionResult> ExportIntegrationData([FromRoute] Guid configurationId, [FromQuery] IntegrationDataUpdateRequestDto request)
        {
            _logger.LogDebug($"Exporting integration data. ConfigurationId: {configurationId}, Kind: {request.Kind}");
            FileDownloadDto result = await _mediator.Send(new ExportIntegrationDataQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                ConfigurationId = configurationId,
                Request = request
            });
            _logger.LogDebug($"Integration data exported. ConfigurationId: {configurationId}, FileName: {result.FileName}");
            return File(result.FileBytes, result.ContentType, result.FileName);
        }

        [HttpPost]
        [Route("api/v1/operations/integrations/{configurationId:guid}/data-update/import/preview")]
        [ApiAuthorization(Name = "OPERATIONS_VIEW_INTEGRATION")]
        [SwaggerOperation("PreviewIntegrationImport")]
        [SwaggerResponse(200, type: typeof(IntegrationImportPreviewResponseDto))]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> PreviewIntegrationImport([FromRoute] Guid configurationId, [FromQuery] IntegrationImportKind kind, IFormFile? file)
        {
            _logger.LogDebug($"Previewing integration import. ConfigurationId: {configurationId}, Kind: {kind}");
            IntegrationImportPreviewResponseDto result = await _mediator.Send(new PreviewIntegrationImportQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                ConfigurationId = configurationId,
                Kind = kind,
                File = file
            });
            _logger.LogDebug($"Integration import previewed. ConfigurationId: {configurationId}, Rows: {result.TotalRows}, Invalid: {result.InvalidRows}");
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/operations/integrations/{configurationId:guid}/data-update/import/correction-report")]
        [ApiAuthorization(Name = "OPERATIONS_VIEW_INTEGRATION")]
        [SwaggerOperation("BuildImportCorrectionReport")]
        [SwaggerResponse(200, type: typeof(FileContentResult))]
        public async Task<IActionResult> BuildImportCorrectionReport([FromRoute] Guid configurationId, [FromBody] IntegrationImportCorrectionReportInputDto request)
        {
            _logger.LogDebug($"Building import correction report. ConfigurationId: {configurationId}, Kind: {request.Kind}");
            FileDownloadDto result = await _mediator.Send(new BuildImportCorrectionReportQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                ConfigurationId = configurationId,
                Request = request
            });
            _logger.LogDebug($"Import correction report built. ConfigurationId: {configurationId}, FileName: {result.FileName}");
            return File(result.FileBytes, result.ContentType, result.FileName);
        }

        [HttpPost]
        [Route("api/v1/operations/integrations/{configurationId:guid}/data-update/import")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_INTEGRATION")]
        [SwaggerOperation("CommitIntegrationImport")]
        [SwaggerResponse(200, type: typeof(IntegrationImportCommitResponseDto))]
        public async Task<IActionResult> CommitIntegrationImport([FromRoute] Guid configurationId, [FromBody] IntegrationImportCommitInputDto request)
        {
            _logger.LogDebug($"Committing integration import. ConfigurationId: {configurationId}, Kind: {request.Kind}, Rows: {request.Rows.Count}");
            IntegrationImportCommitResponseDto result = await _mediator.Send(new CommitIntegrationImportCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                ConfigurationId = configurationId,
                Request = request
            });
            _logger.LogDebug($"Integration import committed. ConfigurationId: {configurationId}, Records: {result.RecordsCommitted}");
            return Ok(result);
        }
    }
}
