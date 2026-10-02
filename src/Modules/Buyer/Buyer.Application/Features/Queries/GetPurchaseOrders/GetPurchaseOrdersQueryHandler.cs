using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetPurchaseOrders
{
    public class GetPurchaseOrdersQueryHandler : IRequestHandler<GetPurchaseOrdersQuery, List<PurchaseOrderListItemDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetPurchaseOrdersQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<PurchaseOrderListItemDto>> Handle(GetPurchaseOrdersQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching purchase orders. OrganizationId: {request.OrganizationId}, Index: {request.Index}, Limit: {request.Limit}");

            BuyerBusinessProfile? buyer = _repository.BuyerBusinessProfile.FindFirstByCondition(
                x => x.OrganizationId == request.OrganizationId && x.IsActive);
            if (buyer == null)
            {
                _logger.LogError($"Buyer not found. OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Buyer not found.", "The signed-in organization does not have a buyer profile.");
            }

            Guid buyerId = buyer.Id;
            string buyerName = buyer.OrganizationName;
            int index = request.Index < 0 ? 0 : request.Index;
            int limit = request.Limit <= 0 ? 20 : Math.Min(request.Limit, 100);
            List<PurchaseOrderListItemDto> result = await _repository.PurchaseOrder
                .FindByCondition(x => x.BuyerId == buyerId && x.IsActive)
                .OrderByDescending(x => x.OrderDate)
                .Skip(index)
                .Take(limit)
                .Select(x => new PurchaseOrderListItemDto
                {
                    Id = x.Id,
                    PoNumber = x.PoNumber,
                    SupplierId = x.SupplierId,
                    SupplierName = x.SupplierName,
                    BuyerName = buyerName,
                    BucketCode = x.BucketCode,
                    PlantCode = x.PlantCode,
                    Status = x.Status,
                    Currency = x.Currency,
                    TotalAmount = x.TotalAmount,
                    OrderDate = x.OrderDate,
                    ItemCount = x.Items.Count(item => item.IsActive)
                })
                .ToListAsync(cancellationToken);

            _logger.LogInfo($"Purchase orders fetched. Count: {result.Count}, BuyerId: {buyerId}");
            return result;
        }
    }
}
