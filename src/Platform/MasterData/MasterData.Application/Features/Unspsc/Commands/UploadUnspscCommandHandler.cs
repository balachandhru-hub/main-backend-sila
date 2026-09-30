using ClosedXML.Excel;
using MediatR;
using Microsoft.AspNetCore.Http;
using MasterData.Infrastructure.Contracts.IRepository;
using MasterData.Domain.Dto;
using MasterData.Domain.Entities;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace MasterData.Application.Features.Unspsc.Commands;

public class UploadUnspscCommandHandler
    : IRequestHandler<UploadUnspscCommand, ExcelUploadResultDto>
{
    private readonly IRepositoryWrapper _repository;
    private readonly ILoggerManager _logger;
    
private readonly IBulkInsertHelper _bulkInsertHelper;

    public UploadUnspscCommandHandler(
        IRepositoryWrapper repository,
        IBulkInsertHelper bulkInsertHelper,
        ILoggerManager logger)
    {
        _repository = repository;
        _bulkInsertHelper = bulkInsertHelper;
        _logger = logger;
    }

    public async Task<ExcelUploadResultDto> Handle(
        UploadUnspscCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInfo("Starting UNSPSC upload process.");
        ValidateFile(request.File);

        using var stream = request.File.OpenReadStream();
        using var workbook = new XLWorkbook(stream);

        var worksheet = workbook.Worksheet(1);

        ValidateHeaders(worksheet);

        var rows = ExtractRows(worksheet);

        const int batchSize = 1000;

        return await ProcessInBatches(rows, batchSize);
    }

   private static void ValidateFile(IFormFile file)
{
    
    if (file == null)
    {
        throw new BadRequestCustomException(
            "File is required.",
            "File is required.");
    }

    if (file.Length == 0)
    {
        throw new NoContentCustomException(
            "Uploaded file is empty.",
            "Uploaded file is empty.");
    }

    if (!Path.GetExtension(file.FileName)
            .Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
    {
        throw new BadRequestCustomException(
            "Only .xlsx files are supported.",
            "Only .xlsx files are supported.");
    }
}

    private static void ValidateHeaders(IXLWorksheet worksheet)
{
    
    string[] expectedHeaders =
    {
        
        "Version",
        "Key",
        "Segment",
        "Segment Title",
        "Segment Definition",
        "Family",
        "Family Title",
        "Family Definition",
        "Class",
        "Class Title",
        "Class Definition",
        "Commodity",
        "Commodity Title",
        "Commodity Definition",
        "Synonym",
        "Acronym"
    };

    var headerRow = worksheet.Row(1);

    if (headerRow == null)
    {
        throw new NoContentCustomException(
            "Excel file is empty.",
            "Excel file is empty.");
    }

    for (int i = 0; i < expectedHeaders.Length; i++)
    {
        var value = headerRow.Cell(i + 1).GetString().Trim();

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new NoContentCustomException(
                $"Header missing at column {i + 1}.",
                $"Header missing at column {i + 1}.");
        }

        if (!value.Equals(expectedHeaders[i], StringComparison.OrdinalIgnoreCase))
        {
            throw new NotFoundCustomException(
                $"Expected header '{expectedHeaders[i]}' but found '{value}'.",
                $"Expected header '{expectedHeaders[i]}' but found '{value}'.");
        }
    }
}

    private List<UnspscCategory> ExtractRows(IXLWorksheet worksheet)
    {
        var categories = new List<UnspscCategory>();

        foreach (var row in worksheet.RowsUsed().Skip(1))
        {
            var entity = new UnspscCategory
            {
                Version = row.Cell(1).GetString(),

                Key = int.TryParse(row.Cell(2).GetString(), out var key)
                    ? key
                    : 0,

                Segment = long.TryParse(row.Cell(3).GetString(), out var segment)
                    ? segment
                    : 0,

                SegmentTitle = row.Cell(4).GetString(),
                SegmentDefinition = row.Cell(5).GetString(),

                Family = long.TryParse(row.Cell(6).GetString(), out var family)
                    ? family
                    : null,

                FamilyTitle = row.Cell(7).GetString(),
                FamilyDefinition = row.Cell(8).GetString(),

                Class = long.TryParse(row.Cell(9).GetString(), out var classCode)
                    ? classCode
                    : null,

                ClassTitle = row.Cell(10).GetString(),
                ClassDefinition = row.Cell(11).GetString(),

                Commodity = long.TryParse(row.Cell(12).GetString(), out var commodity)
                    ? commodity
                    : null,

                CommodityTitle = row.Cell(13).GetString(),
                CommodityDefinition = row.Cell(14).GetString(),

                Synonym = row.Cell(15).GetString(),
                Acronym = row.Cell(16).GetString()
            };

            categories.Add(entity);
        }

        return categories;
    }

   private async Task<ExcelUploadResultDto> ProcessInBatches(
    List<UnspscCategory> data,
    int batchSize)
{
    var result = new ExcelUploadResultDto
    {
        TotalRows = data.Count
    };

    for (int i = 0; i < data.Count; i += batchSize)
    {
        var batch = data
            .Skip(i)
            .Take(batchSize)
            .ToList();

        var validRows = new List<UnspscCategory>();

        for (int j = 0; j < batch.Count; j++)
        {
            var item = batch[j];

            if (!IsValid(item))
            {
                result.FailedUploads++;

                result.Errors.Add(
                    $"Invalid record at Excel row {i + j + 2}");

                continue;
            }

            validRows.Add(item);
        }

        try
        {
            if (validRows.Any())
            {
                await _bulkInsertHelper.BulkInsertOrUpdateAsync(validRows);

                result.SuccessfulUploads += validRows.Count;
            }
        }
        catch (Exception ex)
        {
            result.FailedUploads += validRows.Count;

            result.Errors.Add(
                $"Batch starting at row {i + 2}: {ex.Message}");
        }
    }

    return result;
}
    private static bool IsValid(UnspscCategory item)
    {
        return
            !string.IsNullOrWhiteSpace(item.Version) &&
            item.Key > 0 &&
            item.Segment > 0 &&
            !string.IsNullOrWhiteSpace(item.SegmentTitle) &&
            !string.IsNullOrWhiteSpace(item.SegmentDefinition);
    }
}