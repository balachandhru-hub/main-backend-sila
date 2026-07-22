using Buyer.Infrastructure.Contracts.IRepository;
using ClosedXML.Excel;
using MediatR;
using BuyerEntity = Buyer.Domain.Entities.ItemBuyerMaster;
using SharedKernel.ExceptionHandler;

namespace Buyer.Application.Features.Commands.ItemBuyerMaster
{
    public class UploadItemBuyerMasterCommandHandler
        : IRequestHandler<UploadItemBuyerMasterCommand, int>
    {
        private readonly IRepositoryWrapper _repository;

        public UploadItemBuyerMasterCommandHandler(
            IRepositoryWrapper repository)
        {
            _repository = repository;
        }

        public async Task<int> Handle(
    UploadItemBuyerMasterCommand request,
    CancellationToken cancellationToken)
        {
            if (request.UploadDto.File == null)
            {
                throw new BadRequestCustomException(
                    "File is required.",
                    "File is required.");
            }
            if (!Path.GetExtension(request.UploadDto.File.FileName)
                    .Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestCustomException(
                    "Only .xlsx files are supported.",
                    "Only .xlsx files are supported.");
            }
            if (request.UploadDto.File.Length == 0)
            {
                throw new NoContentCustomException(
                    "Uploaded file is empty.",
                    "Uploaded file is empty.");
            }


            List<BuyerEntity> items = new();

            using var stream = request.UploadDto.File.OpenReadStream();
            using var workbook = new XLWorkbook(stream);

            var worksheet = workbook.Worksheet(1);

            Guid buyerId;

            if (request.UploadDto.BuyerId.HasValue)
            {
                var buyer = await _repository.BuyerBusinessProfile
                    .FindFirstByConditionAsync(x =>
                        x.Id == request.UploadDto.BuyerId.Value &&
                        x.IsActive);

                if (buyer == null)
                    throw new NotFoundCustomException(
                        "Buyer profile not found.",
                        "Buyer profile not found.");
                buyerId = buyer.Id;
            }
            else
            {
                var buyer = await _repository.BuyerBusinessProfile
                    .FindFirstByConditionAsync(x =>
                        x.OrganizationId == request.UploadDto.OrganizationId &&
                        x.IsActive);

                if (buyer == null)
                    throw new NotFoundCustomException(
                        "Buyer profile not found.",
                        "Buyer profile not found.");
                buyerId = buyer.Id;
            }

            foreach (var row in worksheet.RowsUsed().Skip(1))
            {
                var description = row.Cell(1).GetString().Trim();
                var materialCode = row.Cell(2).GetString().Trim();
                var materialGroup = row.Cell(3).GetString().Trim();

                if (string.IsNullOrWhiteSpace(materialCode))
                    continue;

                items.Add(new BuyerEntity
                {
                    BuyerId = buyerId,
                    Description = description,
                    MaterialCode = materialCode,
                    MaterialGroup = materialGroup
                });
            }

            await _repository.BulkInsertHelper.BulkInsertOrUpdateAsync(items);

            return items.Count;
        }
    }
}