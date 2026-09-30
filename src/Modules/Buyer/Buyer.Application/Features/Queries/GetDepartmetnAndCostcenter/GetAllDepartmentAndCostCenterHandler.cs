using Buyer.Domain.Dto;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;

namespace Buyer.Application.Features.Queries.Department
{
    public class GetBuyerDepartmentQueryHandler
        : IRequestHandler<GetBuyerDepartmentQuery, List<BuyerDepartmentDto>>
    {
        private readonly IRepositoryWrapper _repository;

        public GetBuyerDepartmentQueryHandler(IRepositoryWrapper repository)
        {
            _repository = repository;
        }

        public async Task<List<BuyerDepartmentDto>> Handle(GetBuyerDepartmentQuery request, CancellationToken cancellationToken)
        {
            Guid buyerId;

            if (request.BuyerId.HasValue && request.BuyerId.Value != Guid.Empty)
            {
                buyerId = request.BuyerId.Value;
            }
            else
            {
                var buyer = await _repository.BuyerBusinessProfile
                    .FindByCondition(x => x.OrganizationId == request.OrganizationId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (buyer == null)
                    throw new NotFoundCustomException("Buyer not found.", "");

                buyerId = buyer.Id;
            }

            return await _repository.BuyerDepartment
                .FindByCondition(x =>
                    x.BuyerId == buyerId &&
                    (string.IsNullOrEmpty(request.SearchTerm) ||
                     x.Department.Contains(request.SearchTerm)))
                .OrderBy(x => x.Department)
                .Skip(request.Index)
                .Take(request.Limit)
                .Select(x => new BuyerDepartmentDto
                {
                    Id = x.Id,
                    Department = x.Department
                })
                .ToListAsync(cancellationToken);
        }
    }
}