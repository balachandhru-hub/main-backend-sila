using Operations.Application.Features.Commands.RetryDocumentTransfer;
using Operations.Application.Features.Commands.RunDocumentAdvancedExtraction;
using Operations.Application.Features.Commands.UploadInvoice;
using Operations.Application.Features.Queries.GetDocument;
using Operations.Application.Features.Queries.GetDocumentContent;
using Operations.Application.Features.Queries.GetDocumentStatus;
using Operations.Application.Features.Queries.GetDocumentTransfers;
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
    /// Invoice documents: upload, stored file, extraction and the transfers to external storage.
    /// </summary>
    [ApiController]
    public class DocumentController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public DocumentController(IMediator mediator, ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpPost]
        [Route("api/v1/operations/documents/invoices")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_DOCUMENT")]
        [SwaggerOperation("UploadInvoice")]
        [SwaggerResponse(200, type: typeof(DocumentResponseDto))]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public async Task<IActionResult> UploadInvoice([FromForm] UploadInvoiceRequestDto request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
        {
            _logger.LogDebug($"Uploading invoice document. SourceChannel: {request.SourceChannel}, OperatingUnitId: {request.OperatingUnitId}");
            DocumentResponseDto result = await _mediator.Send(new UploadInvoiceCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                Request = request,
                IdempotencyKey = idempotencyKey
            });
            _logger.LogDebug($"Invoice document uploaded. DocumentId: {result.Id}, InvoiceId: {result.InvoiceId}");
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/operations/documents/{documentId:guid}")]
        [ApiAuthorization(Name = "OPERATIONS_VIEW_DOCUMENT")]
        [SwaggerOperation("GetDocument")]
        [SwaggerResponse(200, type: typeof(DocumentResponseDto))]
        public async Task<IActionResult> GetDocument([FromRoute] Guid documentId)
        {
            _logger.LogDebug($"Fetching document. DocumentId: {documentId}");
            DocumentResponseDto result = await _mediator.Send(new GetDocumentQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                DocumentId = documentId
            });
            _logger.LogDebug($"Document fetched. DocumentId: {result.Id}");
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/operations/documents/{documentId:guid}/content")]
        [ApiAuthorization(Name = "OPERATIONS_VIEW_DOCUMENT")]
        [SwaggerOperation("GetDocumentContent")]
        [SwaggerResponse(200, type: typeof(FileContentResult))]
        public async Task<IActionResult> GetDocumentContent([FromRoute] Guid documentId)
        {
            _logger.LogDebug($"Fetching document content. DocumentId: {documentId}");
            FileDownloadDto result = await _mediator.Send(new GetDocumentContentQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                DocumentId = documentId
            });
            _logger.LogDebug($"Document content fetched. DocumentId: {documentId}, FileName: {result.FileName}");
            return File(result.FileBytes, result.ContentType, result.FileName);
        }

        [HttpGet]
        [Route("api/v1/operations/documents/{documentId:guid}/status")]
        [ApiAuthorization(Name = "OPERATIONS_VIEW_DOCUMENT")]
        [SwaggerOperation("GetDocumentStatus")]
        [SwaggerResponse(200, type: typeof(DocumentStatusResponseDto))]
        public async Task<IActionResult> GetDocumentStatus([FromRoute] Guid documentId)
        {
            _logger.LogDebug($"Fetching document status. DocumentId: {documentId}");
            DocumentStatusResponseDto result = await _mediator.Send(new GetDocumentStatusQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                DocumentId = documentId
            });
            _logger.LogDebug($"Document status fetched. DocumentId: {documentId}, Status: {result.DocumentStatus}");
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/operations/documents/{documentId:guid}/advanced-extract")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_DOCUMENT")]
        [SwaggerOperation("RunDocumentAdvancedExtraction")]
        [SwaggerResponse(200, type: typeof(AdvancedInvoiceExtractionResponseDto))]
        public async Task<IActionResult> RunDocumentAdvancedExtraction([FromRoute] Guid documentId, [FromQuery] ExtractionTrigger trigger = ExtractionTrigger.MANUAL_REREAD)
        {
            _logger.LogDebug($"Running advanced extraction. DocumentId: {documentId}, Trigger: {trigger}");
            AdvancedInvoiceExtractionResponseDto result = await _mediator.Send(new RunDocumentAdvancedExtractionCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                DocumentId = documentId,
                Trigger = trigger
            });
            _logger.LogDebug($"Advanced extraction completed. DocumentId: {documentId}, Status: {result.Status}");
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/operations/documents/{documentId:guid}/transfers")]
        [ApiAuthorization(Name = "OPERATIONS_VIEW_DOCUMENT")]
        [SwaggerOperation("GetDocumentTransfers")]
        [SwaggerResponse(200, type: typeof(DocumentTransfersResponseDto))]
        public async Task<IActionResult> GetDocumentTransfers([FromRoute] Guid documentId)
        {
            _logger.LogDebug($"Fetching document transfers. DocumentId: {documentId}");
            DocumentTransfersResponseDto result = await _mediator.Send(new GetDocumentTransfersQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                DocumentId = documentId
            });
            _logger.LogDebug($"Document transfers fetched. DocumentId: {documentId}, Count: {result.Transfers.Count}");
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/operations/document-transfers/{transferId:guid}/retry")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_DOCUMENT")]
        [SwaggerOperation("RetryDocumentTransfer")]
        [SwaggerResponse(200, type: typeof(DocumentTransferRetryResponseDto))]
        public async Task<IActionResult> RetryDocumentTransfer([FromRoute] Guid transferId)
        {
            _logger.LogDebug($"Retrying document transfer. TransferId: {transferId}");
            DocumentTransferRetryResponseDto result = await _mediator.Send(new RetryDocumentTransferCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                TransferId = transferId
            });
            _logger.LogDebug($"Document transfer queued again. TransferId: {result.TransferId}");
            return Ok(result);
        }
    }
}
