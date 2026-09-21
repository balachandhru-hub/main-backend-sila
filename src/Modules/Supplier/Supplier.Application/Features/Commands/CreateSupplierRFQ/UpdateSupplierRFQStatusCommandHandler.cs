using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using Supplier.Application.Features.Commands.RFQ;
using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.Application.Features.Handlers.RFQ
{
    public class UpdateSupplierRFQStatusCommandHandler
        : IRequestHandler<UpdateSupplierRFQStatusCommand, bool>
    {
        private readonly IRepositoryWrapper _repository;

        public UpdateSupplierRFQStatusCommandHandler(
            IRepositoryWrapper repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(
            UpdateSupplierRFQStatusCommand request,
            CancellationToken cancellationToken)
        {
            // One SupplierRFQ row exists per invited supplier (registered and external)
            // for the same BuyerRFQId, so the status must be applied to all of them -
            // otherwise only one supplier moves from Open to Freezing.
            var supplierRfqs = await _repository.SupplierRFQ
                .FindByCondition(x =>
                    x.BuyerRFQId == request.Request.RFQId)
                .ToListAsync(cancellationToken);

            if (supplierRfqs.Count == 0)
            {


                throw new NotFoundCustomException(
                    "RFQ not found.",
                    $"No RFQ was found with RFQId: {request.Request.RFQId}");

            }

            foreach (var supplierRfq in supplierRfqs)
            {
                supplierRfq.Status = request.Request.Status;

                _repository.SupplierRFQ.Update(supplierRfq);
            }

            await _repository.SaveAsync();

            return true;
        }
    }
}