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
            var supplierRfq = await _repository.SupplierRFQ
                .FindByCondition(x =>
                    x.BuyerRFQId == request.Request.RFQId)
                .FirstOrDefaultAsync(cancellationToken);

            if (supplierRfq == null)
            {


                throw new NotFoundCustomException(
                    "RFQ not found.",
                    $"No RFQ was found with RFQId: {request.Request.RFQId}");

            }

            supplierRfq.Status = request.Request.Status;

            _repository.SupplierRFQ.Update(supplierRfq);

            await _repository.SaveAsync();

            return true;
        }
    }
}