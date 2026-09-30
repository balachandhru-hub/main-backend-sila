using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.ItemBuyerMaster
{
    public class GetItemBuyerMasterQueryHandler
        : IRequestHandler<GetItemBuyerMasterQuery, List<ItemBuyerMasterDto>>
    {
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly ILoggerManager _logger;

        public GetItemBuyerMasterQueryHandler(
            IRepositoryWrapper repositoryWrapper,
            ILoggerManager logger)
        {
            _repositoryWrapper = repositoryWrapper;
            _logger = logger;
        }

        public async Task<List<ItemBuyerMasterDto>> Handle(
            GetItemBuyerMasterQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Fetching Item Buyer Masters. " +
                $"Index: {request.Index}, Limit: {request.Limit}, " +
                $"BuyerId: {request.BuyerId}, SearchTerm: {request.SearchTerm}");
            var query = _repositoryWrapper.ItemBuyerMaster
                .FindByCondition(x => x.IsActive);

            if (request.BuyerId.HasValue)
            {
                _logger.LogInfo(
                    $"Filtering by BuyerId: {request.BuyerId.Value}");
                query = query.Where(x => x.BuyerId == request.BuyerId.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                _logger.LogInfo(
                    $"Filtering by SearchTerm: {request.SearchTerm}");
                query = query.Where(x =>
                    (x.Description ?? "").Contains(request.SearchTerm) ||
                    x.MaterialCode.Contains(request.SearchTerm) ||
                    x.MaterialGroup.Contains(request.SearchTerm));
            }

            var items = query
                .OrderByDescending(x => x.DateUpdated)
                .Skip(request.Index)
                .Take(request.Limit)
                .ToList();

            var result = items.Select(x => new ItemBuyerMasterDto
            {
                Id = x.Id,
                BuyerId = x.BuyerId,
                Description = x.Description,
                MaterialCode = x.MaterialCode,
                MaterialGroup = x.MaterialGroup
            }).ToList();

            if (result == null)
            {
                _logger.LogError("No Item Buyer Masters found.");
                throw new NotFoundCustomException(
                    "Item Buyer Master not found.",
                    "No Item Buyer Master records exist for the given criteria.");
            }

            _logger.LogInfo(
                $"Returning Item Buyer Masters. Count: {result.Count}");
            return await Task.FromResult(result);
        }
    }
}