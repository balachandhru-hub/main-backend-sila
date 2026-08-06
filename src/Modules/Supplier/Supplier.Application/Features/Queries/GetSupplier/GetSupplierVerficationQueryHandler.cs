using Supplier.Application.Contracts;
using Supplier.Application.Features.Queries.GetSupplier;
using Supplier.Domain.Common;
using Supplier.Domain.Dto;
using Supplier.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedKernel.LoggerServices;

namespace Supplier.Application.Features.Queries.GetSupplier
{
    public class GetSupplierListQueryHandler
        : IRequestHandler<GetSupplierListQuery, List<SupplierListDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IBuyerApiClient _buyerApiClient;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILoggerManager _logger;

        public GetSupplierListQueryHandler(
            IRepositoryWrapper repository,
            IBuyerApiClient buyerApiClient,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILoggerManager loggerManager)
        {
            _repository = repository;
            _buyerApiClient = buyerApiClient;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _logger = loggerManager;
        }

        public async Task<List<SupplierListDto>> Handle(
            GetSupplierListQuery request,
            CancellationToken cancellationToken)
        {
            var filter = request.SupplierListDto;

            var categoryQuery = _repository.SupplierCategory
            .FindByCondition(x =>
                x.IsActive &&
                (!filter.Segment.HasValue || x.Segment == filter.Segment) &&
                (!filter.Family.HasValue || x.Family == filter.Family));

            var supplierIds = await categoryQuery
                .Select(x => x.SupplierId)
                .Distinct()
                .ToListAsync(cancellationToken);



            // Step 3 : Get Verified SupplierIds for the Buyer
            var verifiedSupplierIds = await _buyerApiClient.GetVerifiedSuppliers(
                new GetVerifiedSupplierRequestDto
                {
                    BuyerId = filter.BuyerId,
                    Index = filter.Index,
                    Limit = filter.Limit
                },

                cancellationToken);

            verifiedSupplierIds = verifiedSupplierIds
                .Where(id => supplierIds.Contains(id))
                .ToList();

            // Step 4 : Type Filter
            if (!string.IsNullOrWhiteSpace(filter.Type))
            {
                if (filter.Type.Equals(Common.VERIFIED_STATUS, StringComparison.OrdinalIgnoreCase))
                {
                    supplierIds = supplierIds
                        .Where(id => verifiedSupplierIds.Contains(id))
                        .ToList();
                }
                else if (filter.Type.Equals(Common.UNVERIFIED_STATUS, StringComparison.OrdinalIgnoreCase))
                {
                    supplierIds = supplierIds
                        .Where(id => !verifiedSupplierIds.Contains(id))
                        .ToList();
                }
            }

            // Step 5 : Final Result
            var result = await (
     from category in _repository.SupplierCategory.FindByCondition(x =>
         x.IsActive &&
         supplierIds.Contains(x.SupplierId) &&
         (!filter.Segment.HasValue || x.Segment == filter.Segment) &&
         (!filter.Family.HasValue || x.Family == filter.Family))

     join supplier in _repository.SupplierBusinessProfile.FindByCondition(x => x.IsActive)
         on category.SupplierId equals supplier.Id

     where string.IsNullOrEmpty(filter.SearchTerm)
           || supplier.OrganizationName.Contains(filter.SearchTerm)||supplier.SNID.Contains(filter.SearchTerm)

     select new SupplierListDto
     {
         SupplierId = supplier.Id,
         SupplierName = supplier.OrganizationName,
         Email = supplier.Email,
         IsVerified = verifiedSupplierIds.Contains(supplier.Id),
         SNID = supplier.SNID
     })
     .Distinct()
     .OrderBy(x => x.SupplierName)
     .Skip(filter.Index)
     .Take(filter.Limit)
     .ToListAsync(cancellationToken);

            return result;
        }
    }
}