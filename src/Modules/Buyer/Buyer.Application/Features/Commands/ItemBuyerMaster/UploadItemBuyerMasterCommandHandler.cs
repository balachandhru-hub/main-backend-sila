using Buyer.Infrastructure.Contracts.IRepository;
using ClosedXML.Excel;
using MediatR;
using BuyerEntity = Buyer.Domain.Entities.ItemBuyerMaster;

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
            if (request.File == null || request.File.Length == 0)
                throw new Exception("Please upload a valid Excel file.");

            List<BuyerEntity> items = new();

            using var stream = request.File.OpenReadStream();
            using var workbook = new XLWorkbook(stream);

            var worksheet = workbook.Worksheet(1);

            foreach (var row in worksheet.RowsUsed().Skip(1))
            {
                Guid buyerId;

                var buyerIdText = row.Cell(1).GetString().Trim();

                if (!string.IsNullOrWhiteSpace(buyerIdText))
                {
                    // Platform User
                    if (!Guid.TryParse(buyerIdText, out buyerId))
                        throw new Exception($"Invalid BuyerId at row {row.RowNumber()}.");

                    var buyer = await _repository.BuyerBusinessProfile
                        .FindFirstByConditionAsync(x =>
                            x.Id == buyerId &&
                            x.IsActive);

                    if (buyer == null)
                        throw new Exception($"Buyer not found at row {row.RowNumber()}.");
                }
                else
                {
                    // Buyer User
                    var buyer = await _repository.BuyerBusinessProfile
                        .FindFirstByConditionAsync(x =>
                            x.OrganizationId == request.OrganizationId &&
                            x.IsActive);

                    if (buyer == null)
                        throw new Exception("Buyer profile not found.");

                    buyerId = buyer.Id;
                }

                var description = row.Cell(2).GetString().Trim();
                var materialCode = row.Cell(3).GetString().Trim();
                var materialGroup = row.Cell(4).GetString().Trim();

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

            await _repository.ItemBuyerMasterBulk
                .BulkInsertOrUpdateAsync(items);

            return items.Count;
        }
    }
}