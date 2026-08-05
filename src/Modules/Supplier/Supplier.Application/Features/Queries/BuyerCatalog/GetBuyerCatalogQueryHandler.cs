using MediatR;
using Supplier.Domain.Dto;
using Supplier.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Supplier.Application.Features.Queries.BuyerCatalog
{
    public class GetBuyerCatalogQueryHandler
        : IRequestHandler<GetBuyerCatalogQuery, List<BuyerCatalogDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetBuyerCatalogQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<BuyerCatalogDto>> Handle(
            GetBuyerCatalogQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo("Fetching Buyer Catalog.");



            var catalogQuery =
                from catalog in _repository.SupplierCatalog.FindByCondition(x => x.IsActive)

                join supplier in _repository.SupplierBusinessProfile.FindByCondition(x => x.IsActive)
                    on catalog.SupplierId equals supplier.Id

                select new BuyerCatalogDto
                {
                    SupplierId = supplier.Id,
                    CatalogId = catalog.Id,

                    SupplierName = supplier.OrganizationName,

                    CatalogName = catalog.CatalogName,
                    Description = catalog.Description,
                    Price = catalog.Price,
                    UnitOfMeasure = catalog.UnitOfMeasure,

                    Segment = catalog.Segment,
                    SegmentTitle = catalog.SegmentTitle,

                    Family = catalog.Family,
                    FamilyTitle = catalog.FamilyTitle,

                    Class = catalog.Class,
                    ClassTitle = catalog.ClassTitle,

                    Commodity = catalog.Commodity,
                    CommodityTitle = catalog.CommodityTitle,

                    CatalogType = catalog.CatalogType,
                    IsPunchOut = catalog.IsPunchOut,
                    PunchOutUrl = catalog.PunchOutUrl,


                };

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                _logger.LogInfo($"Applying search filter: {request.Search}");
                string search = request.Search.Trim();

                catalogQuery = catalogQuery.Where(x =>
                    x.SupplierName.Contains(search) ||
                    x.CatalogName.Contains(search) ||
                    x.Description.Contains(search));
            }

            if (request.Segment.HasValue){
                _logger.LogInfo($"Applying segment filter: {request.Segment}");
            catalogQuery = catalogQuery.Where(x => x.Segment == request.Segment);
            }
            if (request.Family.HasValue){
                _logger.LogInfo($"Applying family filter: {request.Family}");
            catalogQuery = catalogQuery.Where(x => x.Family == request.Family);
            }
            if (request.Class.HasValue){
                _logger.LogInfo($"Applying class filter: {request.Class}");
            catalogQuery = catalogQuery.Where(x => x.Class == request.Class);
            }
            if (request.Commodity.HasValue){
                _logger.LogInfo($"Applying commodity filter: {request.Commodity}");
            catalogQuery = catalogQuery.Where(x => x.Commodity == request.Commodity);
            }


            catalogQuery = catalogQuery.OrderBy(x => x.CatalogName);
            _logger.LogInfo("Applying pagination.");
            int catalogCount = catalogQuery.Count();

            var result = catalogQuery
                .Skip(request.Index)
                .Take(request.Limit)
                .ToList();

            _logger.LogInfo($"Fetched {result.Count} records out of {catalogCount} total records.");
            int remaining = request.Limit - result.Count;

            if (remaining > 0)
            {
                _logger.LogInfo($"Fetching additional {remaining} records from SupplierCategory.");

                var existingCatalogIds = result
                    .Where(x => x.CatalogId.HasValue)
                    .Select(x => x.CatalogId.Value)
                    .ToList();
                _logger.LogInfo($"Existing catalog IDs: {string.Join(", ", existingCatalogIds)}");

                var existingSupplierIds = result
                    .Select(x => x.SupplierId)
                    .Distinct()
                    .ToList();
                _logger.LogInfo($"Existing supplier IDs: {string.Join(", ", existingSupplierIds)}");


                var supplierCategoryQuery =
                    _repository.SupplierCategory
                    .FindByCondition(x => x.IsActive);

                if (request.Segment.HasValue){
                    _logger.LogInfo($"Filtering SupplierCategory by segment: {request.Segment}");
                supplierCategoryQuery =
                    supplierCategoryQuery.Where(x => x.Segment == request.Segment);
                }
                if (request.Family.HasValue){
                    _logger.LogInfo($"Filtering SupplierCategory by family: {request.Family}");
                supplierCategoryQuery =
                    supplierCategoryQuery.Where(x => x.Family == request.Family);
                }
                if (request.Class.HasValue){
                    _logger.LogInfo($"Filtering SupplierCategory by class: {request.Class}");
                supplierCategoryQuery =
                    supplierCategoryQuery.Where(x => x.Class == request.Class);
                }
                if (request.Commodity.HasValue){
                    _logger.LogInfo($"Filtering SupplierCategory by commodity: {request.Commodity}");
                supplierCategoryQuery =
                    supplierCategoryQuery.Where(x => x.Commodity == request.Commodity);
                }
                var supplierIds = supplierCategoryQuery
                    .Select(x => x.SupplierId)
                    .Distinct()
                    .ToList();


                _logger.LogInfo($"Fetching catalogs for supplier IDs: {string.Join(", ", supplierIds)}");
                var additionalCatalogQuery =
                    from catalog in _repository.SupplierCatalog.FindByCondition(x => x.IsActive)

                    join supplier in _repository.SupplierBusinessProfile.FindByCondition(x => x.IsActive)
                        on catalog.SupplierId equals supplier.Id

                    where supplierIds.Contains(catalog.SupplierId)

                        && !existingCatalogIds.Contains(catalog.Id)

                    select new BuyerCatalogDto
                    {
                        SupplierId = supplier.Id,
                        CatalogId = catalog.Id,

                        SupplierName = supplier.OrganizationName,

                        CatalogName = catalog.CatalogName,
                        Description = catalog.Description,
                        Price = catalog.Price,
                        UnitOfMeasure = catalog.UnitOfMeasure,

                        Segment = catalog.Segment,
                        SegmentTitle = catalog.SegmentTitle,

                        Family = catalog.Family,
                        FamilyTitle = catalog.FamilyTitle,

                        Class = catalog.Class,
                        ClassTitle = catalog.ClassTitle,

                        Commodity = catalog.Commodity,
                        CommodityTitle = catalog.CommodityTitle,

                        CatalogType = catalog.CatalogType,

                        IsPunchOut = catalog.IsPunchOut,

                        PunchOutUrl = catalog.PunchOutUrl,

                       
                    };


                if (!string.IsNullOrWhiteSpace(request.Search))
                {
                    _logger.LogInfo($"Applying search filter to additional catalogs: {request.Search}");
                    string search = request.Search.Trim();

                    additionalCatalogQuery =
                        additionalCatalogQuery.Where(x =>
                            x.SupplierName.Contains(search) ||
                            x.CatalogName.Contains(search) ||
                            x.Description.Contains(search));
                }



                if (request.Segment.HasValue)
                {
                    _logger.LogInfo($"Applying segment filter to additional catalogs: {request.Segment}");
                    additionalCatalogQuery =
                        additionalCatalogQuery.Where(x => x.Segment == null);
                }

                if (request.Family.HasValue)
                {
                    _logger.LogInfo($"Applying family filter to additional catalogs: {request.Family}");
                    additionalCatalogQuery =
                        additionalCatalogQuery.Where(x => x.Family == null);
                }

                if (request.Class.HasValue)
                {
                    _logger.LogInfo($"Applying class filter to additional catalogs: {request.Class}");
                    additionalCatalogQuery =
                        additionalCatalogQuery.Where(x => x.Class == null);
                }

                if (request.Commodity.HasValue)
                {
                    _logger.LogInfo($"Applying commodity filter to additional catalogs: {request.Commodity}");
                    additionalCatalogQuery =
                        additionalCatalogQuery.Where(x => x.Commodity == null);
                }
                _logger.LogInfo($"Ordering additional catalogs by CatalogName and taking {remaining} records.");
                var additionalCatalogs = additionalCatalogQuery
                    .OrderBy(x => x.CatalogName)
                    .Take(remaining)
                    .ToList();

                result.AddRange(additionalCatalogs);
            }



            if (!result.Any())
            {
                _logger.LogError("No suppliers found for the given filters.");
                throw new NotFoundCustomException(
                    "No suppliers found.",
                    "No catalog or supplier category found.");
            }

            _logger.LogInfo($"Returned {result.Count} records.");

            return await Task.FromResult(result);
        }
    }
}