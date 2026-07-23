using MediatR;
using SharedKernel.ExceptionHandler;
using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.Application.Features.Commands.SupplierCatalog
{
    public class DeleteSupplierCatalogCommandHandler
        : IRequestHandler<DeleteSupplierCatalogCommand, bool>
    {
        private readonly IRepositoryWrapper _repository;

        public DeleteSupplierCatalogCommandHandler(
            IRepositoryWrapper repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(
            DeleteSupplierCatalogCommand request,
            CancellationToken cancellationToken)
        {
            var supplier = await _repository.SupplierBusinessProfile
                .FindFirstByConditionAsync(x =>
                    x.OrganizationId == request.OrganizationId &&
                    x.IsActive);

            if (supplier == null)
            {
                throw new NotFoundCustomException(
                    "Supplier profile not found.",
                    "Supplier profile not found.");
            }

            var catalog = await _repository.SupplierCatalog
                .FindFirstByConditionAsync(x =>
                    x.Id == request.Id &&
                    x.SupplierId == supplier.Id &&
                    x.IsActive);

            if (catalog == null)
            {
                throw new NotFoundCustomException(
                    "Supplier catalog not found.",
                    "Supplier catalog not found.");
            }

            _repository.SupplierCatalog.Delete(catalog);

            await _repository.SaveAsync();

            return true;
        }
    }
}