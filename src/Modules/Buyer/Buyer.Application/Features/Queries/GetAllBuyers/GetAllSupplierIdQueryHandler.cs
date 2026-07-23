using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Domain.Dto;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buyer.Application.Features.Queries.GetAllSupplier
{
    public class GetVerifiedSupplierQueryHandler
        : IRequestHandler<GetVerifiedSupplierQuery, List<Guid>>
    {
        private readonly IRepositoryWrapper _repository;

        public GetVerifiedSupplierQueryHandler(IRepositoryWrapper repository)
        {
            _repository = repository;
        }

        public async Task<List<Guid>> Handle(
            GetVerifiedSupplierQuery request,
            CancellationToken cancellationToken)
        {
              var supplierIds = await _repository.BuyerSupplierMapping
                .FindByCondition(x => x.BuyerId == request.VerifiedSupplierRequestDto.BuyerId)
                .OrderBy(x => x.SupplierId)
                .Skip(request.VerifiedSupplierRequestDto.Index)
                .Take(request.VerifiedSupplierRequestDto.Limit)
                .Select(x => x.SupplierId)
                .Distinct()
                .ToListAsync(cancellationToken);

            return supplierIds;
        }
    }
}