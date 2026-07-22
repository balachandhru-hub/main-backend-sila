using MediatR;
using Supplier.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Supplier.Application.Features.Profile.Queries.GetSupplierId
{
    public class GetSupplierIdQueryHandler
        : IRequestHandler<GetSupplierIdQuery, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetSupplierIdQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public Task<Guid> Handle(
            GetSupplierIdQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching Supplier Id for Organization: {request.OrganizationId}");

            var supplier = _repository.SupplierBusinessProfile
                .FindFirstByCondition(x =>
                    x.OrganizationId == request.OrganizationId &&
                    x.IsActive);

            if (supplier == null)
            {
                throw new NotFoundCustomException(
                    "Supplier not found.",
                    "Supplier profile does not exist.");
            }

            return Task.FromResult(supplier.Id);
        }
    }
}