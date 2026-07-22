using Buyer.Domain.Dto;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;

namespace Buyer.Application.Features.Queries.CostCenter
{
    public class GetBuyerDepartmentQueryHandler
     : IRequestHandler<GetBuyerCostCenterQuery, List<BuyerCostCenterDto>>
    {
        private readonly IRepositoryWrapper _repository;

        public GetBuyerDepartmentQueryHandler(IRepositoryWrapper repository)
        {
            _repository = repository;
        }
        public async Task<List<BuyerCostCenterDto>> Handle(GetBuyerCostCenterQuery request, CancellationToken cancellationToken)
        {
            return await _repository.BuyerCostCenter
                .FindByCondition(x =>
                    x.DepartmentId == request.DepartmentId &&
                    (string.IsNullOrEmpty(request.SearchTerm) ||
                     x.CostCenter.Contains(request.SearchTerm)))
                .OrderBy(x => x.CostCenter)
                .Skip(request.Index)
                .Take(request.Limit)
                .Select(x => new BuyerCostCenterDto
                {
                    Id = x.Id,
                    DepartmentId = x.DepartmentId,
                    CostCenter = x.CostCenter
                })
                .ToListAsync(cancellationToken);
        }
    }
}