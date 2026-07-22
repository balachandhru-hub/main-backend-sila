using Supplier.Application.Features.Queries.GetSupplier;
using Supplier.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Supplier.Domain.Dto;
using Supplier.Application.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SharedKernel.LoggerServices;
using Supplier.Domain.Common;

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

        public GetSupplierListQueryHandler(IRepositoryWrapper repository, IBuyerApiClient buyerApiClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor, ILoggerManager loggerManager)
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

            // Step 1 : Supplier Catalog Filter
            var catalogQuery = _repository.SupplierCatalog
                .FindByCondition(x =>
                    (string.IsNullOrEmpty(filter.SegmentCode) ||
                        x.SegmentCode == filter.SegmentCode) &&

                    (string.IsNullOrEmpty(filter.FamilyCode) ||
                        x.FamilyCode == filter.FamilyCode));

            // Step 2 : Get Distinct SupplierIds
            var supplierIds = await catalogQuery
                .Select(x => x.SupplierId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var token = _httpContextAccessor.HttpContext?
                .Request.Cookies[Common.ACCESS_TOKEN];

            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogError("Access token is missing in the request cookies.");
                throw new UnauthorizedAccessException("Access token is missing.");
            }

            // Step 3 : Verified SupplierIds
            var verifiedSupplierIds = await _buyerApiClient.GetVerifiedSuppliers(
     new GetVerifiedSupplierRequestDto
     {
         Index = 0,
         Limit = int.MaxValue,
         SearchTerm = null
     },
     token,
     cancellationToken);

            verifiedSupplierIds = verifiedSupplierIds
                .Where(x => supplierIds.Contains(x))
                .ToList();

            // Step 4 : Type Filter
            if (!string.IsNullOrWhiteSpace(filter.Type))
            {
                if (filter.Type.Equals(Common.VERIFIED_STATUS, StringComparison.OrdinalIgnoreCase))
                {
                    supplierIds = supplierIds
                        .Where(x => verifiedSupplierIds.Contains(x))
                        .ToList();
                }
                else if (filter.Type.Equals(Common.UNVERIFIED_STATUS, StringComparison.OrdinalIgnoreCase))
                {
                    supplierIds = supplierIds
                        .Where(x => !verifiedSupplierIds.Contains(x))
                        .ToList();
                }
            }

            // Step 5 : Final Data
            var result = await _repository.SupplierCatalog
                .FindByCondition(x =>
                    supplierIds.Contains(x.SupplierId) &&
                    (string.IsNullOrEmpty(filter.SearchTerm) ||
                     x.Supplier.OrganizationName.Contains(filter.SearchTerm)))
                .OrderBy(x => x.Price)
                .Skip(filter.Index)
                .Take(filter.Limit)
                .Select(x => new SupplierListDto
                {
                    SupplierId = x.SupplierId,
                    SupplierName = x.Supplier.OrganizationName,
                    SupplierCode = null,
                    Price = x.Price,
                    IsVerified = verifiedSupplierIds.Contains(x.SupplierId)
                })
                .ToListAsync(cancellationToken);

            return result;
        }
    }
}