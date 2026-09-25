using Buyer.Infrastructure.Contracts.IRepository;
using ClosedXML.Excel;
using MediatR;
using BuyerEntity = Buyer.Domain.Entities.ItemBuyerMaster;
using SharedKernel.ExceptionHandler;
using Buyer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Buyer.Domain.Dtos;
using Buyer.Domain.Common;
using Buyer.Application.Features.Assets.Commands;

namespace Buyer.Application.Features.Commands.PredefinedMaterialMaster
{
    /// <summary>
    /// Excel bulk upload for Item Master. Unlike the manual create flow
    /// (one PredefinedMaterial + one approval per material), a whole
    /// uploaded file goes through exactly ONE approval workflow no matter
    /// how many rows it contains: the file is stored as an Asset and
    /// staged in a single ExcelMaterialMaster row; ItemBuyerMaster rows
    /// are only created once that ONE approval is fully COMPLETE (see
    /// ApproveRejectMasterCommandHandler). The manual flow
    /// (CreateItemBuyerMasterCommandHandler) is untouched by this file.
    /// </summary>
    public class UploadPredefinedMaterialCommandHandler
        : IRequestHandler<UploadPredefinedMaterialCommand, ExcelUploadResultDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IMediator _mediator;

        public UploadPredefinedMaterialCommandHandler(
            IRepositoryWrapper repository,
            IMediator mediator)
        {
            _repository = repository;
            _mediator = mediator;
        }

        public async Task<ExcelUploadResultDto> Handle(
    UploadPredefinedMaterialCommand request,
    CancellationToken cancellationToken)
        {
            ValidateFile(request.UploadDto.Document?.FileBytes, request.UploadDto.Document?.FileName);

            var fileBytes = request.UploadDto.Document.FileBytes;

            using var workbook = new XLWorkbook(new MemoryStream(fileBytes));

            var worksheet = workbook.Worksheet(1);

            ValidateHeaders(worksheet);

            var buyerId = await GetBuyerId(request);

            var rows = ExtractRows(worksheet, buyerId);

            var result = ValidateRows(rows);

            // Nothing valid to approve - don't start an approval workflow
            // for an empty/entirely-invalid file.
            if (result.SuccessfulUploads == 0)
            {
                return result;
            }

            // =========================================================
            // Excel bulk flow: no PredefinedMaterial rows are created.
            // Store the file as an Asset, stage ONE ExcelMaterialMaster
            // record, and put it through ONE approval workflow.
            // =========================================================

            var approvalFlow = await _repository.MasterApprovalFlow
                .FindFirstByConditionAsync(x =>
                    x.Id == request.UploadDto.ApprovalFlowId &&
                    x.IsActive);

            if (approvalFlow == null)
            {
                throw new NotFoundCustomException(
                    "Approval flow not found.",
                    "The selected approval flow does not exist.");
            }

            // The Document is the same AssetUploadDto every other upload in
            // this app sends - EntityId/EntityType/AssetType/FileName/
            // ContentType/IsSingletonAsset all come straight from the
            // client's body, unchanged, exactly like UploadRFQESign etc.
            var assetId = await _mediator.Send(
                new UploadAssetCommand(request.UploadDto.Document),
                cancellationToken);

            var excelMaterialMaster = new ExcelMaterialMaster
            {
                Id = Guid.NewGuid(),
                AssetId = assetId,
                BuyerId = buyerId,
                Status = Common.PROCESSING,
                Title = request.UploadDto.Title
            };

            await _repository.ExcelMaterialMaster
                .CreateAsync(excelMaterialMaster);

            var materialApprovalFlowMapping = new ApprovalFlowPredefinedMaterialMapping
            {
                Id = Guid.NewGuid(),
                ApprovalFlowId = request.UploadDto.ApprovalFlowId,
                PredefinedMaterialId = excelMaterialMaster.Id,
                UploadType = Common.UPLOAD_TYPE_EXCEL
            };

            await _repository.ApprovalFlowPredefinedMaterialMapping
                .CreateAsync(materialApprovalFlowMapping);

            var approvalFlowUsers = await _repository.ApprovalFlowUserMapping
                .FindByCondition(x =>
                    x.ApprovalFlowId == request.UploadDto.ApprovalFlowId &&
                    x.IsActive)
                .ToListAsync(cancellationToken);

            if (!approvalFlowUsers.Any())
            {
                throw new PreConditionFailedCustomException(
                    "Approval flow users not found.",
                    "The selected approval flow has no users configured.");
            }

            var materialApprovalFlowUserMappings = approvalFlowUsers
                .Select(user => new PredefinedMaterialApprovalFlowUserMapping
                {
                    Id = Guid.NewGuid(),
                    ApprovalFlowPredefinedMaterialId = materialApprovalFlowMapping.Id,
                    ApprovalFlowId = request.UploadDto.ApprovalFlowId,
                    UserId = user.UserId,
                    Order = user.Order,
                    Comment = request.UploadDto.Comment,
                    Status = Common.PENDING
                })
                .ToList();

            await _repository.PredefinedMaterialApprovalFlowUserMapping
                .CreateRangeAsync(materialApprovalFlowUserMappings);

            await _repository.SaveAsync();

            result.ExcelMaterialMasterId = excelMaterialMaster.Id;

            return result;
        }

        private static void ValidateFile(byte[]? fileBytes, string? fileName)
        {
            if (fileBytes == null || fileBytes.Length == 0)
            {
                throw new NoContentCustomException(
                    "Uploaded file is empty.",
                    "Uploaded file is empty.");
            }

            if (string.IsNullOrWhiteSpace(fileName) ||
                !Path.GetExtension(fileName)
                    .Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestCustomException(
                    "Only .xlsx files are supported.",
                    "Only .xlsx files are supported.");
            }
        }

        // Same 11 columns/order as manual creation's CreateItemBuyerMasterDto
        // (see ItemBuyerMasterController.Create), so the Excel flow ends up
        // creating ItemBuyerMaster rows with the exact same required fields
        // populated - SubUnit/MicroUnit are the only nullable ones.
        private static readonly string[] ExpectedHeaders =
        {
            "Description",
            "MaterialCode",
            "MaterialGroup",
            "ProductType",
            "BaseUnitOfMeasure",
            "OrderUnitOfMeasure",
            "AlternateUnitOfMeasure",
            "ValuationClass",
            "UnitOfMeasureMapping",
            "SubUnit",
            "MicroUnit"
        };

        private static void ValidateHeaders(IXLWorksheet worksheet)
        {
            var expectedHeaders = ExpectedHeaders;

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

                if (!value.Equals(expectedHeaders[i],
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw new NotFoundCustomException(
                        $"Expected header '{expectedHeaders[i]}' but found '{value}'.",
                        $"Expected header '{expectedHeaders[i]}' but found '{value}'.");
                }
            }
        }
        private async Task<Guid> GetBuyerId(
            UploadPredefinedMaterialCommand request)
        {
            BuyerBusinessProfile? buyer;

            if (request.UploadDto.BuyerId.HasValue)
            {
                buyer = await _repository.BuyerBusinessProfile
                    .FindFirstByConditionAsync(x =>
                        x.Id == request.UploadDto.BuyerId.Value &&
                        x.IsActive);
            }
            else
            {
                buyer = await _repository.BuyerBusinessProfile
                    .FindFirstByConditionAsync(x =>
                        x.OrganizationId == request.UploadDto.OrganizationId &&
                        x.IsActive);
            }

            if (buyer == null)
            {
                throw new NotFoundCustomException(
                    "Buyer profile not found.",
                    "Buyer profile not found.");
            }

            return buyer.Id;
        }
        private List<BuyerEntity> ExtractRows(
    IXLWorksheet worksheet,
    Guid buyerId)
        {
            var items = new List<BuyerEntity>();

            foreach (var row in worksheet.RowsUsed().Skip(1))
            {
                var entity = new BuyerEntity
                {
                    BuyerId = buyerId,
                    Description = row.Cell(1).GetString().Trim(),
                    MaterialCode = row.Cell(2).GetString().Trim(),
                    MaterialGroup = row.Cell(3).GetString().Trim(),
                    ProductType = row.Cell(4).GetString().Trim(),
                    BaseUnitOfMeasure = row.Cell(5).GetString().Trim(),
                    OrderUnitOfMeasure = row.Cell(6).GetString().Trim(),
                    AlternateUnitOfMeasure = row.Cell(7).GetString().Trim(),
                    ValuationClass = row.Cell(8).GetString().Trim(),
                    UnitOfMeasureMapping = row.Cell(9).GetString().Trim(),
                    SubUnit = NullIfEmpty(row.Cell(10).GetString().Trim()),
                    MicroUnit = NullIfEmpty(row.Cell(11).GetString().Trim())
                };

                items.Add(entity);
            }

            return items;
        }

        /// <summary>
        /// Validates the extracted rows exactly like the previous direct
        /// insert flow did (same row-level rules preserved), but does NOT
        /// write anything to ItemBuyerMaster - that only happens once the
        /// batch's single approval workflow is COMPLETE.
        /// </summary>
        private static ExcelUploadResultDto ValidateRows(List<BuyerEntity> data)
        {
            var result = new ExcelUploadResultDto
            {
                TotalRows = data.Count
            };

            for (int i = 0; i < data.Count; i++)
            {
                var item = data[i];

                if (!IsValid(item))
                {
                    result.FailedUploads++;

                    result.Errors.Add(
                        $"Invalid record at Excel row {i + 2}");

                    continue;
                }

                result.SuccessfulUploads++;
            }

            return result;
        }
        private static bool IsValid(BuyerEntity item)
        {
            return
                !string.IsNullOrWhiteSpace(item.MaterialCode);
        }

        /// <summary>SubUnit/MicroUnit are nullable columns - store null rather than "" for a blank cell.</summary>
        private static string? NullIfEmpty(string value) =>
            string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
