using Operations.Application.Features.Commands.MatchInvoiceLines;
using Operations.Application.Features.Commands.MatchInvoicePurchaseOrder;
using Operations.Application.Features.Commands.MatchInvoiceSupplier;
using Operations.Application.Features.Commands.ProcessInvoice;
using Operations.Application.Features.Commands.ReprocessInvoice;
using Operations.Application.Features.Commands.UpdateInvoice;
using Operations.Application.Features.Queries.GetBasicExtraction;
using Operations.Application.Features.Queries.GetExtractionHistory;
using Operations.Application.Features.Queries.GetInvoice;
using Operations.Application.Features.Queries.GetInvoiceExtraction;
using Operations.Application.Features.Queries.GetInvoices;
using Operations.Application.Features.Queries.GetSupplierCandidates;
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
    /// Invoices: list, review, re-read and matching to supplier, purchase order and lines.
    /// </summary>
    [ApiController]
    public class InvoiceController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public InvoiceController(IMediator mediator, ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet]
        [Route("api/v1/operations/invoices")]
        [ApiAuthorization(Name = "OPERATIONS_VIEW_INVOICE")]
        [SwaggerOperation("GetInvoices")]
        [SwaggerResponse(200, type: typeof(List<InvoiceResponseDto>))]
        public async Task<IActionResult> GetInvoices()
        {
            _logger.LogDebug($"Fetching invoices.");
            List<InvoiceResponseDto> result = await _mediator.Send(new GetInvoicesQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId()
            });
            _logger.LogDebug($"Invoices fetched. Count: {result.Count}");
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/operations/invoices/{invoiceId:guid}")]
        [ApiAuthorization(Name = "OPERATIONS_VIEW_INVOICE")]
        [SwaggerOperation("GetInvoice")]
        [SwaggerResponse(200, type: typeof(InvoiceResponseDto))]
        public async Task<IActionResult> GetInvoice([FromRoute] Guid invoiceId)
        {
            _logger.LogDebug($"Fetching invoice. InvoiceId: {invoiceId}");
            InvoiceResponseDto result = await _mediator.Send(new GetInvoiceQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                InvoiceId = invoiceId
            });
            _logger.LogDebug($"Invoice fetched. InvoiceId: {result.Id}");
            return Ok(result);
        }

        [HttpPut]
        [Route("api/v1/operations/invoices/{invoiceId:guid}")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_INVOICE")]
        [SwaggerOperation("UpdateInvoice")]
        [SwaggerResponse(200, type: typeof(InvoiceResponseDto))]
        public async Task<IActionResult> UpdateInvoice([FromRoute] Guid invoiceId, [FromBody] UpdateInvoiceRequestDto request)
        {
            _logger.LogDebug($"Updating invoice. InvoiceId: {invoiceId}");
            InvoiceResponseDto result = await _mediator.Send(new UpdateInvoiceCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                InvoiceId = invoiceId,
                Request = request
            });
            _logger.LogDebug($"Invoice updated. InvoiceId: {result.Id}");
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/operations/invoices/{invoiceId:guid}/process")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_INVOICE")]
        [SwaggerOperation("ProcessInvoice")]
        [SwaggerResponse(200, type: typeof(InvoiceResponseDto))]
        public async Task<IActionResult> ProcessInvoice([FromRoute] Guid invoiceId)
        {
            _logger.LogDebug($"Processing invoice. InvoiceId: {invoiceId}");
            InvoiceResponseDto result = await _mediator.Send(new ProcessInvoiceCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                InvoiceId = invoiceId
            });
            _logger.LogDebug($"Invoice processed. InvoiceId: {result.Id}, Status: {result.Status}");
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/operations/invoices/{invoiceId:guid}/reprocess")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_INVOICE")]
        [SwaggerOperation("ReprocessInvoice")]
        [SwaggerResponse(200, type: typeof(AdvancedInvoiceExtractionResponseDto))]
        public async Task<IActionResult> ReprocessInvoice([FromRoute] Guid invoiceId)
        {
            _logger.LogDebug($"Re-reading invoice. InvoiceId: {invoiceId}");
            AdvancedInvoiceExtractionResponseDto result = await _mediator.Send(new ReprocessInvoiceCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                InvoiceId = invoiceId
            });
            _logger.LogDebug($"Invoice re-read completed. InvoiceId: {invoiceId}, Status: {result.Status}");
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/operations/invoices/{invoiceId:guid}/advanced-reread")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_INVOICE")]
        [SwaggerOperation("AdvancedRereadInvoice")]
        [SwaggerResponse(200, type: typeof(AdvancedInvoiceExtractionResponseDto))]
        public async Task<IActionResult> AdvancedRereadInvoice([FromRoute] Guid invoiceId)
        {
            _logger.LogDebug($"Re-reading invoice with the advanced extraction. InvoiceId: {invoiceId}");
            AdvancedInvoiceExtractionResponseDto result = await _mediator.Send(new ReprocessInvoiceCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                InvoiceId = invoiceId
            });
            _logger.LogDebug($"Invoice advanced re-read completed. InvoiceId: {invoiceId}, Status: {result.Status}");
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/operations/invoices/{invoiceId:guid}/basic-extraction")]
        [ApiAuthorization(Name = "OPERATIONS_VIEW_INVOICE")]
        [SwaggerOperation("GetBasicExtraction")]
        [SwaggerResponse(200, type: typeof(BasicInvoiceExtractionResponseDto))]
        public async Task<IActionResult> GetBasicExtraction([FromRoute] Guid invoiceId)
        {
            _logger.LogDebug($"Fetching basic extraction. InvoiceId: {invoiceId}");
            BasicInvoiceExtractionResponseDto result = await _mediator.Send(new GetBasicExtractionQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                InvoiceId = invoiceId
            });
            _logger.LogDebug($"Basic extraction fetched. InvoiceId: {invoiceId}");
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/operations/invoices/{invoiceId:guid}/extraction")]
        [ApiAuthorization(Name = "OPERATIONS_VIEW_INVOICE")]
        [SwaggerOperation("GetInvoiceExtraction")]
        [SwaggerResponse(200, type: typeof(InvoiceExtractionResponseDto))]
        public async Task<IActionResult> GetInvoiceExtraction([FromRoute] Guid invoiceId)
        {
            _logger.LogDebug($"Fetching invoice extraction. InvoiceId: {invoiceId}");
            InvoiceExtractionResponseDto result = await _mediator.Send(new GetInvoiceExtractionQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                InvoiceId = invoiceId
            });
            _logger.LogDebug($"Invoice extraction fetched. InvoiceId: {invoiceId}, Lines: {result.Lines.Count}");
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/operations/invoices/{invoiceId:guid}/extraction-history")]
        [ApiAuthorization(Name = "OPERATIONS_VIEW_INVOICE")]
        [SwaggerOperation("GetExtractionHistory")]
        [SwaggerResponse(200, type: typeof(List<ExtractionHistoryItemDto>))]
        public async Task<IActionResult> GetExtractionHistory([FromRoute] Guid invoiceId)
        {
            _logger.LogDebug($"Fetching extraction history. InvoiceId: {invoiceId}");
            List<ExtractionHistoryItemDto> result = await _mediator.Send(new GetExtractionHistoryQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                InvoiceId = invoiceId
            });
            _logger.LogDebug($"Extraction history fetched. InvoiceId: {invoiceId}, Count: {result.Count}");
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/operations/invoices/{invoiceId:guid}/supplier-candidates")]
        [ApiAuthorization(Name = "OPERATIONS_VIEW_INVOICE")]
        [SwaggerOperation("GetSupplierCandidates")]
        [SwaggerResponse(200, type: typeof(SupplierMatchResponseDto))]
        public async Task<IActionResult> GetSupplierCandidates([FromRoute] Guid invoiceId)
        {
            _logger.LogDebug($"Fetching supplier candidates. InvoiceId: {invoiceId}");
            SupplierMatchResponseDto result = await _mediator.Send(new GetSupplierCandidatesQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                InvoiceId = invoiceId
            });
            _logger.LogDebug($"Supplier candidates fetched. InvoiceId: {invoiceId}, Count: {result.Candidates.Count}");
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/operations/invoices/{invoiceId:guid}/match-supplier")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_INVOICE")]
        [SwaggerOperation("MatchInvoiceSupplier")]
        [SwaggerResponse(200, type: typeof(InvoiceResponseDto))]
        public async Task<IActionResult> MatchInvoiceSupplier([FromRoute] Guid invoiceId, [FromBody] MatchSupplierRequestDto request)
        {
            _logger.LogDebug($"Matching invoice supplier. InvoiceId: {invoiceId}, SupplierId: {request.SupplierId}");
            InvoiceResponseDto result = await _mediator.Send(new MatchInvoiceSupplierCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                InvoiceId = invoiceId,
                Request = request
            });
            _logger.LogDebug($"Invoice supplier matched. InvoiceId: {result.Id}, SupplierId: {result.SupplierId}");
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/operations/invoices/{invoiceId:guid}/match-po")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_INVOICE")]
        [SwaggerOperation("MatchInvoicePurchaseOrder")]
        [SwaggerResponse(200, type: typeof(InvoiceResponseDto))]
        public async Task<IActionResult> MatchInvoicePurchaseOrder([FromRoute] Guid invoiceId, [FromBody] MatchPurchaseOrderRequestDto request)
        {
            _logger.LogDebug($"Matching invoice purchase order. InvoiceId: {invoiceId}, PurchaseOrderId: {request.PurchaseOrderId}, PoNumber: {request.PoNumber}");
            InvoiceResponseDto result = await _mediator.Send(new MatchInvoicePurchaseOrderCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                InvoiceId = invoiceId,
                Request = request
            });
            _logger.LogDebug($"Invoice purchase order matched. InvoiceId: {result.Id}, PurchaseOrderId: {result.PurchaseOrderId}");
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/operations/invoices/{invoiceId:guid}/match-lines")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_INVOICE")]
        [SwaggerOperation("MatchInvoiceLines")]
        [SwaggerResponse(200, type: typeof(InvoiceResponseDto))]
        public async Task<IActionResult> MatchInvoiceLines([FromRoute] Guid invoiceId, [FromBody] MatchInvoiceLinesRequestDto request)
        {
            _logger.LogDebug($"Matching invoice lines. InvoiceId: {invoiceId}, Lines: {request.Lines.Count}");
            InvoiceResponseDto result = await _mediator.Send(new MatchInvoiceLinesCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                InvoiceId = invoiceId,
                Request = request
            });
            _logger.LogDebug($"Invoice lines matched. InvoiceId: {result.Id}, Status: {result.Status}");
            return Ok(result);
        }
    }
}
