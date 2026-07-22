using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;

namespace Buyer.Application.Features.Queries.ItemBuyerMaster
{
    public class GetItemBuyerMasterQueryHandler
        : IRequestHandler<GetItemBuyerMasterQuery, List<ItemBuyerMasterDto>>
    {
        private readonly IRepositoryWrapper _repositoryWrapper;

        public GetItemBuyerMasterQueryHandler(
            IRepositoryWrapper repositoryWrapper)
        {
            _repositoryWrapper = repositoryWrapper;
        }

        public async Task<List<ItemBuyerMasterDto>> Handle(
            GetItemBuyerMasterQuery request,
            CancellationToken cancellationToken)
        {
            var query = _repositoryWrapper.ItemBuyerMaster
                .FindByCondition(x => x.IsActive);

            if (request.BuyerId.HasValue)
            {
                query = query.Where(x => x.BuyerId == request.BuyerId.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
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

            return await Task.FromResult(result);
        }
    }
}