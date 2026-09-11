using MediatR;
using Supplier.Domain.Dto;
using Supplier.Infrastructure.Contracts.IRepository;
using SharedKernel.LoggerServices;
using SharedKernel.ExceptionHandler;

namespace Supplier.Application.Features.Queries.GetSupplierNamesByIds
{
    public class GetSupplierNamesByIdsQueryHandler
        : IRequestHandler<GetSupplierNamesByIdsQuery, List<SupplierNameDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetSupplierNamesByIdsQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public Task<List<SupplierNameDto>> Handle(
            GetSupplierNamesByIdsQuery request,
            CancellationToken cancellationToken)
        {
            if (request.SupplierIds == null || !request.SupplierIds.Any())
            {
                _logger.LogInfo("No Supplier IDs provided. Returning empty list.");
                throw new NotFoundCustomException(
                    "No Supplier IDs provided.",
                    "The list of Supplier IDs is empty or null.");
            }

            var supplierIds = request.SupplierIds.Distinct().ToList();

            _logger.LogInfo($"Fetching {supplierIds.Count} supplier name(s) by id.");

            var result = _repository.SupplierBusinessProfile
                .FindByCondition(x => supplierIds.Contains(x.Id) && x.IsActive)
                .Select(x => new SupplierNameDto
                {
                    SupplierId = x.Id,
                    SupplierName = x.OrganizationName
                })
                .ToList();

            _logger.LogInfo($"Found {result.Count} supplier name(s) for the requested ids.");

            return Task.FromResult(result);
        }
    }
}
