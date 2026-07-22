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
            var query = _repository.BuyerSupplierMapping.FindByCondition(x => true);

            // Optional search by SupplierId (if SearchTerm contains Guid)
            if (!string.IsNullOrWhiteSpace(request.VerifiedSupplierRequestDto.SearchTerm))
            {
                if (Guid.TryParse(request.VerifiedSupplierRequestDto.SearchTerm, out Guid supplierId))
                {
                    query = query.Where(x => x.SupplierId == supplierId);
                }
            }

            var supplierIds = await query
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