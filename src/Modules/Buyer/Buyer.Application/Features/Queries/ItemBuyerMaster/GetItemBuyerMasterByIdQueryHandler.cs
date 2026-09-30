using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.ItemBuyerMaster
{
    public class GetItemBuyerMasterByIdQueryHandler
        : IRequestHandler<GetItemBuyerMasterByIdQuery, ItemBuyerMasterDetailDto>
    {
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly ILoggerManager _logger;

        public GetItemBuyerMasterByIdQueryHandler(
            IRepositoryWrapper repositoryWrapper,
            ILoggerManager logger)
        {
            _repositoryWrapper = repositoryWrapper;
            _logger = logger;
        }

        public async Task<ItemBuyerMasterDetailDto> Handle(
            GetItemBuyerMasterByIdQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching Item Buyer Master details for Id: {request.Id}");

            var item = await _repositoryWrapper.ItemBuyerMaster
                .FindFirstByConditionAsync(x =>
                    x.Id == request.Id &&
                    x.IsActive);

            if (item == null)
            {
                _logger.LogError($"Item Buyer Master not found for Id: {request.Id}");
                throw new NotFoundCustomException(
                    "Item Buyer Master not found.",
                    $"No Item Buyer Master exists with Id: {request.Id}.");
            }

            return new ItemBuyerMasterDetailDto
            {
                Id = item.Id,
                BuyerId = item.BuyerId,        
                ProductType = item.ProductType,
                BaseUnitOfMeasure = item.BaseUnitOfMeasure,
                OrderUnitOfMeasure = item.OrderUnitOfMeasure,
                AlternateUnitOfMeasure = item.AlternateUnitOfMeasure,
                ValuationClass = item.ValuationClass,
                UnitOfMeasureMapping = item.UnitOfMeasureMapping,
                SubUnit = item.SubUnit,
                MicroUnit = item.MicroUnit,

            };
        }
    }
}
