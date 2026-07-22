using MediatR;
using SharedKernel.ExceptionHandler;
using Supplier.Infrastructure.Contracts.IRepository;
using Supplier.Application.Features.Commands.Supplier.UpdateSupplierStatusOrganization;

namespace Supplier.Application.Features.Commands.Supplier.UpdateSupplierStatusOrganization
{
    public class UpdateSupplierStatusOrganizationCommandHandler
        : IRequestHandler<UpdateSupplierStatusOrganizationCommand, bool>
    {
        private readonly IRepositoryWrapper _repository;

        public UpdateSupplierStatusOrganizationCommandHandler(
            IRepositoryWrapper repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(
            UpdateSupplierStatusOrganizationCommand request,
            CancellationToken cancellationToken)
        {
            var supplier = _repository.SupplierBusinessProfile
                .FindFirstByCondition(x =>
                    x.OrganizationId == request.Supplier.OrganizationId);

            if (supplier == null)
            {
                throw new NotFoundCustomException(
                    "Supplier not found.",
                    "Supplier does not exist.");
            }

            supplier.IsActive = request.Supplier.IsActive;

            _repository.SupplierBusinessProfile.Update(supplier);

            await _repository.SaveAsync();

            return true;
        }
    }
}