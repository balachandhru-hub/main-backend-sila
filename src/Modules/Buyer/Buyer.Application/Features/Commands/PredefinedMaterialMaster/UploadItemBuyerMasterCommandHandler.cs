using Buyer.Infrastructure.Contracts.IRepository;
using ClosedXML.Excel;
using MediatR;
using BuyerEntity = Buyer.Domain.Entities.ItemBuyerMaster;
using SharedKernel.ExceptionHandler;
using Buyer.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Buyer.Domain.Dtos;

namespace Buyer.Application.Features.Commands.PredefinedMaterialMaster
{
    public class UploadPredefinedMaterialCommandHandler
        : IRequestHandler<UploadPredefinedMaterialCommand, ExcelUploadResultDto>
    {
        private readonly IRepositoryWrapper _repository;

        public UploadPredefinedMaterialCommandHandler(
            IRepositoryWrapper repository)
        {
            _repository = repository;
        }

        public async Task<ExcelUploadResultDto> Handle(
    UploadPredefinedMaterialCommand request,
    CancellationToken cancellationToken)
        {
            ValidateFile(request.UploadDto.File);

            using var stream = request.UploadDto.File.OpenReadStream();
            using var workbook = new XLWorkbook(stream);

            var worksheet = workbook.Worksheet(1);

            ValidateHeaders(worksheet);

            var buyerId = await GetBuyerId(request);

            var rows = ExtractRows(worksheet, buyerId);

            return await ProcessInBatches(rows, 1000);
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
        "Description",
        "MaterialCode",
        "MaterialGroup"
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
                    MaterialGroup = row.Cell(3).GetString().Trim()
                };

                items.Add(entity);
            }

            return items;
        }
        private async Task<ExcelUploadResultDto> ProcessInBatches(List<BuyerEntity> data, int batchSize)
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

                var validRows = new List<BuyerEntity>();

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
                        await _repository.BulkInsertHelper
                            .BulkInsertOrUpdateAsync(validRows);

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
        private static bool IsValid(BuyerEntity item)
        {
            return
                !string.IsNullOrWhiteSpace(item.MaterialCode);
        }
    }
}