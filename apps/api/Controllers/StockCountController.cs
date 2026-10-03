using Microsoft.AspNetCore.Mvc;
using SilaMe.Api.Auth;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;
using SilaMe.Api.Services;

namespace SilaMe.Api.Controllers;

[ApiController]
[Route("api/v1/inventory")]
public sealed class StockCountController(IAccessService access, StockCountService counts) : ControllerBase
{
    [HttpGet("stock-counts")]
    public Task<IActionResult> List([FromQuery] Guid organizationId, [FromQuery] string? status, [FromQuery] string? countType, [FromQuery] Guid? propertyId, [FromQuery] Guid? locationId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken) =>
        Read(organizationId, () => counts.ListAsync(organizationId, status, countType, propertyId, locationId, from, to, cancellationToken), cancellationToken);

    [HttpPost("stock-counts")]
    public Task<IActionResult> Create([FromQuery] Guid organizationId, [FromBody] StockCountCreateRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, gate => counts.CreateAsync(gate, organizationId, request, cancellationToken), cancellationToken);

    [HttpGet("stock-counts/{id:guid}")]
    public Task<IActionResult> Get(Guid id, [FromQuery] Guid organizationId, [FromQuery] string? line, [FromQuery] int? take, CancellationToken cancellationToken) =>
        Write(organizationId, gate => counts.GetAsync(gate, organizationId, id, line, take, cancellationToken), cancellationToken);

    [HttpPost("stock-counts/{id:guid}/cancel")]
    public Task<IActionResult> Cancel(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Write(organizationId, gate => counts.CancelAsync(gate, organizationId, id, cancellationToken), cancellationToken);

    [HttpPost("stock-counts/{id:guid}/submit")]
    public Task<IActionResult> Submit(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Write(organizationId, gate => counts.SubmitAsync(gate, organizationId, id, cancellationToken), cancellationToken);

    [HttpGet("stock-counts/materials")]
    public Task<IActionResult> Materials([FromQuery] Guid organizationId, [FromQuery] Guid? locationId, [FromQuery] Guid? sessionId, [FromQuery] string? query, CancellationToken cancellationToken) =>
        Read(organizationId, () => counts.SearchAsync(organizationId, locationId, sessionId, query, cancellationToken), cancellationToken);

    [HttpPost("stock-counts/{id:guid}/identify-barcode")]
    public Task<IActionResult> Barcode(Guid id, [FromQuery] Guid organizationId, [FromBody] BarcodeBody body, CancellationToken cancellationToken) =>
        Write(organizationId, gate => counts.IdentifyBarcodeAsync(gate, organizationId, id, body.Barcode, cancellationToken), cancellationToken);

    [HttpPost("stock-counts/{id:guid}/identify-photo")]
    public Task<IActionResult> Photo(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Write(organizationId, gate => counts.IdentifyPhotoAsync(gate, organizationId, id, cancellationToken), cancellationToken);

    [HttpPost("stock-counts/{id:guid}/lines/{lineId:guid}")]
    public Task<IActionResult> Count(Guid id, Guid lineId, [FromQuery] Guid organizationId, [FromBody] StockCountEntry request, CancellationToken cancellationToken) =>
        Write(organizationId, gate => counts.SaveCountAsync(gate, organizationId, id, lineId, request, cancellationToken), cancellationToken);

    [HttpPost("stock-counts/barcodes")]
    public Task<IActionResult> BarcodeMap([FromQuery] Guid organizationId, [FromBody] MaterialBarcodeRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, gate => counts.UpsertBarcodeAsync(gate, organizationId, request, cancellationToken), cancellationToken);

    [HttpGet("stock-counts/tasks")]
    public Task<IActionResult> Tasks([FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Write(organizationId, gate => counts.TasksAsync(gate, organizationId, cancellationToken), cancellationToken);

    [HttpGet("stock-counts/enquiries/{id:guid}")]
    public Task<IActionResult> Enquiry(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Write(organizationId, gate => counts.EnquiryAsync(gate, organizationId, id, cancellationToken), cancellationToken);

    [HttpPost("stock-counts/enquiries/{id:guid}/respond")]
    public Task<IActionResult> Respond(Guid id, [FromQuery] Guid organizationId, [FromBody] StockJustificationRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, gate => counts.RespondAsync(gate, organizationId, id, request, cancellationToken), cancellationToken);

    [HttpPost("stock-counts/lines/{lineId:guid}/review")]
    public Task<IActionResult> Review(Guid lineId, [FromQuery] Guid organizationId, [FromBody] StockReviewRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, gate => counts.ReviewAsync(gate, organizationId, lineId, request, cancellationToken), cancellationToken);

    [HttpPost("stock-counts/lines/{lineId:guid}/reprocess")]
    public Task<IActionResult> Reprocess(Guid lineId, [FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Write(organizationId, gate => counts.ReprocessAsync(gate, organizationId, lineId, cancellationToken), cancellationToken);

    [HttpGet("stock-counts/shortages")]
    public Task<IActionResult> Shortages([FromQuery] Guid organizationId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] Guid? propertyId, [FromQuery] Guid? locationId, [FromQuery] string? materialGroup, [FromQuery] Guid? manager, [FromQuery] string? category, [FromQuery] string? enquiryStatus, [FromQuery] string? reviewStatus, [FromQuery] string? sapStatus, CancellationToken cancellationToken) =>
        Read(organizationId, () => counts.ShortagesAsync(organizationId, new StockReportFilter(from, to, propertyId, locationId, materialGroup, manager, category, enquiryStatus, reviewStatus, sapStatus), cancellationToken), cancellationToken);

    [HttpGet("stock-counts/report.xlsx")]
    public async Task<IActionResult> Excel([FromQuery] Guid organizationId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] Guid? propertyId, [FromQuery] Guid? locationId, [FromQuery] string? materialGroup, [FromQuery] Guid? manager, [FromQuery] string? category, [FromQuery] string? enquiryStatus, [FromQuery] string? reviewStatus, [FromQuery] string? sapStatus, CancellationToken cancellationToken)
    {
        if (await Guard(organizationId, cancellationToken) is { } denied) return denied;
        var report = await counts.ShortagesAsync(organizationId, new StockReportFilter(from, to, propertyId, locationId, materialGroup, manager, category, enquiryStatus, reviewStatus, sapStatus), cancellationToken);
        return File(StockCountReportFiles.Excel(report), "application/vnd.ms-excel", "sila-shortage-report.xls");
    }

    [HttpGet("stock-counts/report.pdf")]
    public async Task<IActionResult> Pdf([FromQuery] Guid organizationId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] Guid? propertyId, [FromQuery] Guid? locationId, [FromQuery] string? materialGroup, [FromQuery] Guid? manager, [FromQuery] string? category, [FromQuery] string? enquiryStatus, [FromQuery] string? reviewStatus, [FromQuery] string? sapStatus, CancellationToken cancellationToken)
    {
        if (await Guard(organizationId, cancellationToken) is { } denied) return denied;
        var report = await counts.ShortagesAsync(organizationId, new StockReportFilter(from, to, propertyId, locationId, materialGroup, manager, category, enquiryStatus, reviewStatus, sapStatus), cancellationToken);
        return File(StockCountReportFiles.Pdf(report), "application/pdf", "sila-shortage-report.pdf");
    }

    [HttpPost("stock-counts/report/send")]
    public async Task<IActionResult> Send([FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        if (await Guard(organizationId, cancellationToken) is { } denied) return denied;
        return StatusCode(StatusCodes.Status409Conflict, new ApiError("EMAIL_NOT_CONFIGURED", "EMAIL NOT CONFIGURED. Download the Excel or PDF report."));
    }

    [HttpGet("transactions")]
    public Task<IActionResult> Transactions([FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Read(organizationId, () => counts.TransactionsAsync(organizationId, cancellationToken), cancellationToken);

    private Task<IActionResult> Read<T>(Guid organizationId, Func<Task<T>> work, CancellationToken cancellationToken) =>
        Run(organizationId, _ => work(), cancellationToken);

    private Task<IActionResult> Write<T>(Guid organizationId, Func<StockCountAccess, Task<T>> work, CancellationToken cancellationToken) =>
        Run(organizationId, work, cancellationToken);

    private async Task<IActionResult> Run<T>(Guid organizationId, Func<StockCountAccess, Task<T>> work, CancellationToken cancellationToken)
    {
        if (await Guard(organizationId, cancellationToken) is { } denied) return denied;
        try { return Ok(await work(await Gate(organizationId, cancellationToken))); }
        catch (RecipeManagementException exception) { return StatusCode(exception.Status, new ApiError(exception.Code, exception.Message)); }
    }

    private async Task<IActionResult?> Guard(Guid organizationId, CancellationToken cancellationToken)
    {
        var session = Current();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        if (await access.HasPermissionAsync(session, "VIEW_INVENTORY", organizationId, null, cancellationToken)) return null;
        return StatusCode(403, new ApiError("FORBIDDEN", "You do not have permission for Inventory."));
    }

    private async Task<StockCountAccess> Gate(Guid organizationId, CancellationToken cancellationToken)
    {
        var session = Current()!;
        async Task<bool> Has(string key) => await access.HasPermissionAsync(session, key, organizationId, null, cancellationToken);
        return new StockCountAccess(session,
            await Has("CREATE_INVENTORY_COUNT"),
            await Has("ENTER_INVENTORY_COUNT") || await Has("CREATE_INVENTORY_COUNT"),
            await Has("APPROVE_INVENTORY_COUNT") || await Has("APPROVE_STOCK_ADJUSTMENT"),
            await Has("POST_INVENTORY_COUNT") || await Has("POST_STOCK_ADJUSTMENT"));
    }

    private Session? Current() => HttpContext.Items[SessionContext.ItemKey] as Session;

    public sealed record BarcodeBody(string Barcode);
}
