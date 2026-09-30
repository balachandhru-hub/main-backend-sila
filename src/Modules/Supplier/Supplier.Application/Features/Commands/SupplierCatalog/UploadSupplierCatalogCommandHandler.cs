using ClosedXML.Excel;
using MediatR;
using Microsoft.AspNetCore.Http;
using SharedKernel.ExceptionHandler;
using Supplier.Domain.Common;
using Supplier.Domain.Dto;
using Supplier.Infrastructure.Contracts.IRepository;
using SupplierCatalogEntity = Supplier.Domain.Entities.SupplierCatalog;
using SharedKernel.LoggerServices;

namespace Supplier.Application.Features.Commands.SupplierCatalog
{
    public class UploadSupplierCatalogCommandHandler
        : IRequestHandler<UploadSupplierCatalogCommand, ExcelUploadResultDto>
    {
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly ILoggerManager _logger;

        public UploadSupplierCatalogCommandHandler(
            IRepositoryWrapper repositoryWrapper,
            ILoggerManager logger)
        {
            _repositoryWrapper = repositoryWrapper;
            _logger = logger;
        }

        public async Task<ExcelUploadResultDto> Handle(
            UploadSupplierCatalogCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo("Starting supplier catalog upload process.");

            ValidateFile(request.File);

            var supplier = _repositoryWrapper.SupplierBusinessProfile
                .FindFirstByCondition(x =>
                    x.OrganizationId == request.OrganizationId &&
                    x.IsActive);

            if (supplier == null)
            {
                _logger.LogError($"Supplier profile not found for OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException(
                    "Supplier profile not found.",
                    "Supplier profile not found.");
            }

            if (!string.Equals(
                    supplier.Status,
                    Common.VERIFIED_STATUS,
                    StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError($"Supplier with OrganizationId: {request.OrganizationId} is not verified.");
                throw new PreConditionFailedCustomException(
                    "Supplier must be verified before uploading catalog.",
                    "Supplier is not verified.");
            }

            _logger.LogInfo($"Validating supplier catalog Excel file for OrganizationId: {request.OrganizationId}. FileName: {request.File.FileName}, Size: {request.File.Length}");

            using var stream = request.File.OpenReadStream();
            using var workbook = new XLWorkbook(stream);

            var worksheet = workbook.Worksheet(1);
            ValidateHeaders(worksheet);

            var rows = ExtractRows(worksheet, supplier.Id);
            _logger.LogInfo($"Extracted {rows.Count} rows from the catalog upload file.");

            var result = await ProcessRows(rows);
            _logger.LogInfo($"Supplier catalog upload completed. TotalRows: {result.TotalRows}, Success: {result.SuccessfulUploads}, Failed: {result.FailedUploads}");

            return result;
        }

        private void ValidateFile(IFormFile file)
        {
            if (file == null)
            {
                _logger.LogError("Supplier catalog upload failed. File is required.");
                throw new BadRequestCustomException(
                    "File is required.",
                    "File is required.");
            }

            if (file.Length == 0)
            {
                _logger.LogError("Supplier catalog upload failed. Uploaded file is empty.");
                throw new NoContentCustomException(
                    "Uploaded file is empty.",
                    "Uploaded file is empty.");
            }

            if (!Path.GetExtension(file.FileName)
                    .Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError($"Supplier catalog upload failed. Unsupported file type: {file.FileName}");
                throw new BadRequestCustomException(
                    "Only .xlsx files are supported.",
                    "Only .xlsx files are supported.");
            }
        }

        private static void ValidateHeaders(IXLWorksheet worksheet)
        {
            string[] expectedHeaders =
            {
                "CatalogName",
                "Description",
                "Price",
                "Currency",
                "UnitOfMeasure",
                "CatalogType",
                "IsPunchOut",
                "PunchOutUrl",
                "Segment",
                "SegmentTitle",
                "Family",
                "FamilyTitle",
                "Class",
                "ClassTitle",
                "Commodity",
                "CommodityTitle"
            };

            var headerRow = worksheet.Row(1);

            if (headerRow == null)
                throw new NoContentCustomException(
                    "Excel file is empty.",
                    "Excel file is empty.");

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

        private static List<(SupplierCatalogEntity Entity, int RowNumber, string? Error)> ExtractRows(
            IXLWorksheet worksheet,
            Guid supplierId)
        {
            var rows = new List<(SupplierCatalogEntity, int, string?)>();

            foreach (var row in worksheet.RowsUsed().Skip(1))
            {
                int rowNum = row.RowNumber();
                string? error = null;

                var catalogName = row.Cell(1).GetString().Trim();
                var description = row.Cell(2).GetString().Trim();
                var priceRaw = row.Cell(3).GetString().Trim();
                var currency = row.Cell(4).GetString().Trim();
                var unitOfMeasure = row.Cell(5).GetString().Trim();
                var catalogTypeRaw = row.Cell(6).GetString().Trim().ToUpper();
                var isPunchOutRaw = row.Cell(7).GetString().Trim();
                var punchOutUrl = row.Cell(8).GetString().Trim();
                var segmentRaw = row.Cell(9).GetString().Trim();
                var segmentTitle = row.Cell(10).GetString().Trim();
                var familyRaw = row.Cell(11).GetString().Trim();
                var familyTitle = row.Cell(12).GetString().Trim();
                var classRaw = row.Cell(13).GetString().Trim();
                var classTitle = row.Cell(14).GetString().Trim();
                var commodityRaw = row.Cell(15).GetString().Trim();
                var commodityTitle = row.Cell(16).GetString().Trim();

                if (string.IsNullOrWhiteSpace(description))
                {
                    error = $"Row {rowNum}: Description is required.";
                    rows.Add((null!, rowNum, error));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(unitOfMeasure))
                {
                    error = $"Row {rowNum}: UnitOfMeasure is required.";
                    rows.Add((null!, rowNum, error));
                    continue;
                }

                if (catalogTypeRaw != Common.CATALOG && catalogTypeRaw != Common.NON_CATALOG)
                {
                    error = $"Row {rowNum}: CatalogType must be CATALOG or NON_CATALOG.";
                    rows.Add((null!, rowNum, error));
                    continue;
                }

                decimal? price = null;
                if (!string.IsNullOrWhiteSpace(priceRaw))
                {
                    if (!decimal.TryParse(priceRaw, out decimal parsedPrice))
                    {
                        error = $"Row {rowNum}: Price must be a valid decimal.";
                        rows.Add((null!, rowNum, error));
                        continue;
                    }
                    price = parsedPrice;
                }

                if (catalogTypeRaw == Common.CATALOG && price == null)
                {
                    error = $"Row {rowNum}: Price is required for CATALOG type.";
                    rows.Add((null!, rowNum, error));
                    continue;
                }

                bool isPunchOut = isPunchOutRaw.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                                  isPunchOutRaw == "1";

                if (catalogTypeRaw == Common.CATALOG)
                    isPunchOut = false;

                if (catalogTypeRaw == Common.NON_CATALOG && isPunchOut &&
                    string.IsNullOrWhiteSpace(punchOutUrl))
                {
                    error = $"Row {rowNum}: PunchOutUrl is required when IsPunchOut is true.";
                    rows.Add((null!, rowNum, error));
                    continue;
                }

                long? segment = long.TryParse(segmentRaw, out long seg) ? seg : null;
                long? family = long.TryParse(familyRaw, out long fam) ? fam : null;
                long? cls = long.TryParse(classRaw, out long cl) ? cl : null;
                long? commodity = long.TryParse(commodityRaw, out long com) ? com : null;

                var entity = new SupplierCatalogEntity
                {
                    Id = Guid.NewGuid(),
                    SupplierId = supplierId,
                    CatalogName = string.IsNullOrWhiteSpace(catalogName) ? null! : catalogName,
                    Description = description,
                    Price = price,
                    Currency = string.IsNullOrWhiteSpace(currency) ? null : currency,
                    UnitOfMeasure = unitOfMeasure,
                    CatalogType = catalogTypeRaw,
                    IsPunchOut = isPunchOut,
                    PunchOutUrl = string.IsNullOrWhiteSpace(punchOutUrl) ? null : punchOutUrl,
                    Segment = segment,
                    SegmentTitle = string.IsNullOrWhiteSpace(segmentTitle) ? null : segmentTitle,
                    Family = family,
                    FamilyTitle = string.IsNullOrWhiteSpace(familyTitle) ? null : familyTitle,
                    Class = cls,
                    ClassTitle = string.IsNullOrWhiteSpace(classTitle) ? null : classTitle,
                    Commodity = commodity,
                    CommodityTitle = string.IsNullOrWhiteSpace(commodityTitle) ? null : commodityTitle
                };

                rows.Add((entity, rowNum, null));
            }

            return rows;
        }

        private async Task<ExcelUploadResultDto> ProcessRows(
            List<(SupplierCatalogEntity Entity, int RowNumber, string? Error)> rows)
        {
            var result = new ExcelUploadResultDto
            {
                TotalRows = rows.Count
            };

            var validEntities = new List<SupplierCatalogEntity>();

            foreach (var (entity, rowNumber, error) in rows)
            {
                if (error != null)
                {
                    _logger.LogError($"Skipping invalid supplier catalog row {rowNumber}: {error}");
                    result.FailedUploads++;
                    result.Errors.Add(error);
                    continue;
                }

                validEntities.Add(entity);
            }

            foreach (var entity in validEntities)
            {
                try
                {
                    var catalogIdentifier = string.IsNullOrWhiteSpace(entity.CatalogName) ? entity.Id.ToString() : entity.CatalogName;
                    _logger.LogInfo($"Persisting supplier catalog: {catalogIdentifier}");
                    _repositoryWrapper.SupplierCatalog.Create(entity);
                    result.SuccessfulUploads++;
                }
                catch (Exception ex)
                {
                    var catalogIdentifier = string.IsNullOrWhiteSpace(entity.CatalogName) ? entity.Id.ToString() : entity.CatalogName;
                    _logger.LogError($"Failed to insert catalog '{catalogIdentifier}': {ex.Message}");
                    result.FailedUploads++;
                    result.Errors.Add($"Failed to insert catalog '{catalogIdentifier}': {ex.Message}");
                }
            }

            _repositoryWrapper.Save();
            _logger.LogInfo($"Saved catalog upload transaction. Successful: {result.SuccessfulUploads}, Failed: {result.FailedUploads}");

            await Task.CompletedTask;
            return result;
        }
    }
}
